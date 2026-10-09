//! Host-owned Component MRP preparation, operations, preferences and saved-artifact grants.
use crate::component_mrp_native as native;
use serde::{Deserialize, Serialize};
use std::{
    collections::HashMap,
    io::Read,
    path::PathBuf,
    sync::{Arc, Mutex},
    time::{Duration, Instant},
};
use tauri::{Emitter, Manager};

const MAX_BYTES: usize = 8 * 1024 * 1024;
const LIFETIME: Duration = Duration::from_secs(1800);
#[derive(Default, Deserialize, Serialize)]
#[serde(deny_unknown_fields, rename_all = "camelCase")]
struct Preference {
    schema_version: u32,
    open_after_save: bool,
    #[serde(default)]
    last_parent: Option<PathBuf>,
}
struct Preparation {
    id: String,
    bytes: Arc<Vec<u8>>,
    name: String,
    created: Instant,
    backend: String,
}
struct Artifact {
    path: PathBuf,
    id: (u32, u64),
    bytes: Arc<Vec<u8>>,
    attempts: u8,
    retries: HashMap<String, bool>,
}
#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Outcome {
    outcome: String,
    message: String,
    saved_artifact_id: Option<String>,
    open_failed: bool,
    operation_complete: bool,
}
impl Outcome {
    fn new(kind: &str, message: &str) -> Self {
        Self {
            outcome: kind.into(),
            message: message.into(),
            saved_artifact_id: None,
            open_failed: false,
            operation_complete: kind != "pending",
        }
    }
}
#[derive(Default)]
struct State {
    session: String,
    epoch: u64,
    prepared: Option<Preparation>,
    preference: Option<Preference>,
    operations: HashMap<String, Outcome>,
    artifacts: HashMap<String, Artifact>,
    busy: bool,
    close_pending: bool,
}
#[derive(Default)]
pub struct ReportPreferences(Mutex<State>, Mutex<bool>);
impl ReportPreferences {
    pub fn cancel_close(&self) {
        self.0
            .lock()
            .unwrap_or_else(|e| e.into_inner())
            .close_pending = false;
    }
    pub fn defer_close(&self) -> bool {
        let mut s = self.0.lock().unwrap_or_else(|e| e.into_inner());
        s.close_pending = true;
        s.busy || *self.1.lock().unwrap_or_else(|e| e.into_inner())
    }
    fn complete(&self, window: &tauri::WebviewWindow) {
        let close = {
            let mut s = self.0.lock().unwrap_or_else(|e| e.into_inner());
            s.release_work()
        };
        if close {
            let _ = window.close();
        }
    }
}
#[tauri::command]
pub fn component_mrp_cancel_close(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
) -> Result<(), String> {
    require_main(&window)?;
    state.cancel_close();
    Ok(())
}
impl State {
    fn retry(
        &mut self,
        artifact_id: &str,
        request_id: &str,
    ) -> Result<Option<(PathBuf, (u32, u64), Arc<Vec<u8>>)>, String> {
        if self.busy || self.close_pending {
            return Err("Another save/open is active.".into());
        }
        let a = self
            .artifacts
            .get_mut(artifact_id)
            .ok_or("Saved artifact grant unavailable.")?;
        if let Some(success) = a.retries.get(request_id) {
            return if *success {
                Ok(None)
            } else {
                Err("Previous Open request failed or is unresolved; no redispatch.".into())
            };
        }
        if a.attempts >= 3 {
            return Err("Open retry limit reached.".into());
        }
        a.attempts += 1;
        a.retries.insert(request_id.into(), false);
        let result = (a.path.clone(), a.id, a.bytes.clone());
        self.busy = true;
        Ok(Some(result))
    }
    fn record_operation(&mut self, id: String, outcome: Outcome) {
        self.operations.insert(id, outcome);
    }
    fn release_work(&mut self) -> bool {
        // A committed intermediate save is not terminal while its optional open is pending.
        assert!(self
            .operations
            .values()
            .all(|o| o.outcome != "saved" || o.operation_complete));
        self.busy = false;
        self.close_pending
    }
}
struct GenerationLease<'a>(&'a ReportPreferences, &'a tauri::WebviewWindow);
impl Drop for GenerationLease<'_> {
    fn drop(&mut self) {
        if let Ok(mut active) = self.0 .1.lock() {
            *active = false;
        }
        let close = self
            .0
             .0
            .lock()
            .map(|s| s.close_pending && !s.busy)
            .unwrap_or(false);
        if close {
            let _ = self.1.close();
        }
    }
}
fn require_main(window: &tauri::WebviewWindow) -> Result<(), String> {
    if window.label() != "main" {
        Err("Main window required.".into())
    } else {
        Ok(())
    }
}
fn open_artifact_with(
    target: &std::path::Path,
    identity: (u32, u64),
    bytes: &[u8],
    launch: impl FnOnce(&std::path::Path) -> Result<(), String>,
) -> Result<(), String> {
    native::verify_artifact(target, identity, bytes)?;
    launch(target)
}
fn path(app: &tauri::AppHandle) -> Result<PathBuf, String> {
    Ok(app
        .path()
        .local_data_dir()
        .map_err(|_| "Local preferences unavailable.")?
        .join("KST/config/component-mrp-open-preference.v1.json"))
}
fn load(path: &std::path::Path) -> Result<Preference, String> {
    match std::fs::File::open(path) {
        Ok(file) => {
            let mut bytes = Vec::new();
            file.take(4097)
                .read_to_end(&mut bytes)
                .map_err(|_| "Preference read failed.")?;
            if bytes.len() > 4096 {
                return Err("Preference exceeds capacity.".into());
            }
            let p: Preference =
                serde_json::from_slice(&bytes).map_err(|_| "Invalid save preferences.")?;
            if p.schema_version != 1 {
                return Err("Unknown preference version.".into());
            }
            Ok(p)
        }
        Err(e) if e.kind() == std::io::ErrorKind::NotFound => Ok(Preference {
            schema_version: 1,
            ..Default::default()
        }),
        Err(_) => Err("Preference unavailable.".into()),
    }
}
fn persist(path: &std::path::Path, p: &Preference) -> Result<(), String> {
    std::fs::create_dir_all(path.parent().ok_or("Preference parent unavailable.")?)
        .map_err(|_| "Preference directory unavailable.")?;
    std::fs::write(
        path,
        serde_json::to_vec(p).map_err(|_| "Preference encoding failed.")?,
    )
    .map_err(|_| "Save preferences could not be remembered.".into())
}
#[tauri::command]
pub fn component_mrp_open_preference(
    window: tauri::WebviewWindow,
    app: tauri::AppHandle,
    state: tauri::State<'_, ReportPreferences>,
) -> Result<bool, String> {
    require_main(&window)?;
    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
    if s.preference.is_none() {
        s.preference = Some(load(&path(&app)?)?)
    }
    Ok(s.preference.as_ref().unwrap().open_after_save)
}
#[tauri::command]
pub fn component_mrp_remember_open_preference(
    window: tauri::WebviewWindow,
    app: tauri::AppHandle,
    state: tauri::State<'_, ReportPreferences>,
    value: bool,
) -> Result<(), String> {
    require_main(&window)?;
    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
    let p = s.preference.get_or_insert_with(|| Preference {
        schema_version: 1,
        ..Default::default()
    });
    p.open_after_save = value;
    persist(&path(&app)?, p)
}

#[tauri::command]
pub fn component_mrp_begin(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
) -> Result<u64, String> {
    require_main(&window)?;
    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
    if s.busy || s.close_pending || s.operations.values().any(|o| o.outcome == "unknown") {
        return Err("A save operation is active or unresolved; reconcile it first.".into());
    }
    s.epoch += 1;
    s.prepared = None;
    if s.session.is_empty() {
        s.session = native::new_id()?
    }
    Ok(s.epoch)
}
#[derive(Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Prepared {
    preparation_id: String,
    file_name: String,
    as_of_date: String,
    acquired_at_utc: String,
    expires_at_utc: String,
    warning: Option<String>,
    period: String,
    include_unconfirmed: bool,
    forecast_end_exclusive: String,
}
#[tauri::command]
pub async fn component_mrp_generate(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
    backend: tauri::State<'_, crate::SharedBackendState>,
    epoch: u64,
    site: String,
    part: String,
    horizon_weeks: u32,
    include_unconfirmed: bool,
    period: String,
) -> Result<Prepared, String> {
    require_main(&window)?;
    {
        let mut active = state
            .1
            .lock()
            .map_err(|_| "Generation state unavailable.")?;
        if *active {
            return Err("Generation already active. Retry after it completes.".into());
        }
        *active = true;
    }
    let _lease = GenerationLease(&state, &window);
    if part.len() > 1024
        || !["SW", "NW", "AR", "MN", "MS", "KV"].contains(&site.as_str())
        || ![13, 26, 52, 72].contains(&horizon_weeks)
        || !["Day", "Week", "Month"].contains(&period.as_str())
    {
        return Err("Invalid report request.".into());
    }
    let session = {
        let s = state.0.lock().map_err(|_| "State unavailable.")?;
        if s.epoch != epoch || s.busy || s.close_pending {
            return Err("Report generation superseded.".into());
        }
        s.session.clone()
    };
    let url = {
        let b = backend.lock().await;
        b.active
            .as_ref()
            .filter(|p| p.ready)
            .and_then(|p| p.base_url.clone())
            .ok_or("Managed backend unavailable.")?
    };
    let client = reqwest::Client::builder()
        .redirect(reqwest::redirect::Policy::none())
        .timeout(Duration::from_secs(120))
        .build()
        .map_err(|_| "Transport unavailable.")?;
    let mut response=client.post(format!("{url}/api/v1/reports/component-mrp")).header("X-KST-Report-Session",&session)
        .json(&serde_json::json!({"site":site,"part":part,"horizonWeeks":horizon_weeks,"includeUnconfirmed":include_unconfirmed,"period":period})).send().await.map_err(|_|"Generation transport failed.")?;
    if !response.status().is_success() {
        return Err(if response.status().as_u16() == 404 {
            "Part not found in selected domain."
        } else {
            "Component MRP acquisition failed. Retry Generate."
        }
        .into());
    }
    let mut metadata = Vec::new();
    while let Some(chunk) = response
        .chunk()
        .await
        .map_err(|_| "Preparation metadata incomplete.")?
    {
        if metadata.len() + chunk.len() > 4096 {
            return Err("Preparation metadata exceeds capacity.".into());
        }
        metadata.extend_from_slice(&chunk)
    }
    let prepared: Prepared =
        serde_json::from_slice(&metadata).map_err(|_| "Invalid preparation metadata.")?;
    if prepared.preparation_id.len() != 36
        || !prepared
            .preparation_id
            .chars()
            .all(|c| c.is_ascii_hexdigit() || c == '-')
    {
        return Err("Invalid preparation ID.".into());
    }
    if prepared.file_name.len() > 220
        || prepared.file_name.contains(['/', '\\', ':'])
        || !prepared.file_name.ends_with(".xlsx")
    {
        return Err("Invalid suggested filename.".into());
    }
    let mut response = client
        .get(format!(
            "{url}/api/v1/reports/component-mrp/preparations/{}/bytes",
            prepared.preparation_id
        ))
        .header("X-KST-Report-Session", &session)
        .send()
        .await
        .map_err(|_| "Preparation transfer failed.")?;
    if !response.status().is_success() {
        return Err("Preparation expired. Generate again.".into());
    }
    if response
        .headers()
        .get(reqwest::header::CONTENT_TYPE)
        .and_then(|v| v.to_str().ok())
        != Some("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
    {
        return Err("Unexpected preparation content type.".into());
    }
    if response.content_length().unwrap_or(u64::MAX) > MAX_BYTES as u64 {
        return Err("Preparation exceeds capacity.".into());
    }
    let expected = response.content_length().unwrap_or(0) as usize;
    let mut bytes = Vec::new();
    while let Some(chunk) = response
        .chunk()
        .await
        .map_err(|_| "Preparation transfer incomplete.")?
    {
        if bytes.len() + chunk.len() > MAX_BYTES {
            return Err("Preparation exceeds capacity.".into());
        }
        bytes.extend_from_slice(&chunk)
    }
    if bytes.len() != expected || bytes.len() < 4 || !bytes.starts_with(b"PK") {
        return Err("Prepared workbook transfer is incomplete or invalid.".into());
    }
    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
    if s.epoch != epoch || s.busy {
        return Err("Generation superseded; result discarded.".into());
    }
    s.prepared = Some(Preparation {
        id: prepared.preparation_id.clone(),
        bytes: Arc::new(bytes),
        name: prepared.file_name.clone(),
        created: Instant::now(),
        backend: url,
    });
    Ok(prepared)
}

#[tauri::command]
pub fn component_mrp_status(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
    operation_id: String,
) -> Result<Outcome, String> {
    require_main(&window)?;
    let s = state.0.lock().map_err(|_| "State unavailable.")?;
    s.operations
        .get(&operation_id)
        .cloned()
        .ok_or("Operation not found; no replay is authorized.".into())
}

#[tauri::command]
pub async fn component_mrp_save(
    window: tauri::WebviewWindow,
    app: tauri::AppHandle,
    state: tauri::State<'_, ReportPreferences>,
    backend: tauri::State<'_, crate::SharedBackendState>,
    operation_id: String,
    preparation_id: String,
    open_after_save: bool,
) -> Result<Outcome, String> {
    require_main(&window)?;
    if operation_id.len() > 64 || operation_id.is_empty() {
        return Err("Invalid operation ID.".into());
    }
    let active_url = {
        let b = backend.lock().await;
        b.active
            .as_ref()
            .filter(|p| p.ready)
            .and_then(|p| p.base_url.clone())
    };
    let (bytes, name, folder) = {
        let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
        if let Some(result) = s.operations.get(&operation_id) {
            return Ok(result.clone());
        }
        if s.busy || s.close_pending || s.operations.values().any(|o| o.outcome == "unknown") {
            return Err(
                "Another save/open operation is active or unresolved; no replay is authorized."
                    .into(),
            );
        }
        if s.operations.len() >= 64 || s.artifacts.len() >= 8 {
            return Err(
                "Report operation capacity reached. Restart after reviewing saved outcomes.".into(),
            );
        }
        let valid = s.prepared.as_ref().is_some_and(|p| {
            p.id == preparation_id
                && p.created.elapsed() <= LIFETIME
                && active_url.as_ref() == Some(&p.backend)
        });
        if !valid {
            let result = Outcome::new(
                "failed",
                "Preparation expired, invalidated or sidecar changed. Generate again.",
            );
            s.operations.insert(operation_id, result.clone());
            return Ok(result);
        }
        let p = s.prepared.as_ref().unwrap();
        let bytes = p.bytes.clone();
        let mut name = p.name.clone();
        let suggestion_sequence = s
            .operations
            .values()
            .filter(|o| o.outcome == "saved")
            .count()
            + 1;
        if let Some(stem) = name.strip_suffix("_01.xlsx") {
            name = format!("{stem}_{suggestion_sequence:02}.xlsx")
        }
        if s.preference.is_none() {
            s.preference = Some(load(&path(&app)?).unwrap_or_else(|_| Preference {
                schema_version: 1,
                ..Default::default()
            }))
        }
        let folder = s
            .preference
            .as_ref()
            .and_then(|p| p.last_parent.clone())
            .filter(|p| p.is_dir())
            .or_else(native::documents);
        s.busy = true;
        s.operations.insert(
            operation_id.clone(),
            Outcome::new("pending", "Save As is active."),
        );
        (bytes, name, folder)
    };
    let owner = match window.hwnd() {
        Ok(hwnd) => hwnd.0 as isize,
        Err(_) => {
            let result = Outcome::new("failed", "Chooser owner unavailable; nothing saved.");
            let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
            s.record_operation(operation_id, result.clone());
            drop(s);
            state.complete(&window);
            return Ok(result);
        }
    };
    let selection = tauri::async_runtime::spawn_blocking(move || {
        crate::component_mrp_chooser::choose(owner, &name, folder.as_deref())
    })
    .await;
    let chosen = match selection {
        Ok(Ok(chosen)) => chosen,
        _ => {
            let result = Outcome::new(
                "failed",
                "Windows chooser could not establish an accepted destination; nothing saved.",
            );
            let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
            s.record_operation(operation_id, result.clone());
            drop(s);
            state.complete(&window);
            return Ok(result);
        }
    };
    let replace = chosen.as_ref().is_some_and(|s| s.replace);
    let chosen = chosen.map(|s| s.path);
    let result = if let Some(destination) = chosen {
        let data = bytes.clone();
        let target = destination.clone();
        match tauri::async_runtime::spawn_blocking(move || {
            native::publish_selected(&target, &data, replace, false)
        })
        .await
        {
            Ok(Ok(identity)) => {
                let artifact = native::new_id().unwrap_or_else(|_| operation_id.clone());
                let preference_warning = {
                    let mut s = state
                        .0
                        .lock()
                        .map_err(|_| "State unavailable after commitment; reconcile operation.")?;
                    s.artifacts.insert(
                        artifact.clone(),
                        Artifact {
                            path: destination.clone(),
                            id: identity,
                            bytes: bytes.clone(),
                            attempts: 0,
                            retries: HashMap::new(),
                        },
                    );
                    let p = s.preference.as_mut().unwrap();
                    p.last_parent = destination.parent().map(|p| p.to_path_buf());
                    path(&app)
                        .and_then(|location| persist(&location, p))
                        .is_err()
                };
                let mut result = Outcome::new(
                    "saved",
                    if preference_warning {
                        "Workbook saved, but save preferences could not be remembered."
                    } else {
                        "Workbook saved."
                    },
                );
                result.saved_artifact_id = Some(artifact.clone());
                result.operation_complete = false;
                if open_after_save {
                    result.message = "Workbook saved; opening is in progress.".into();
                }
                // Store commitment before launching; repeats/status never dispatch.
                {
                    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
                    s.record_operation(operation_id.clone(), result.clone());
                }
                let _ = window.emit(
                    "component-mrp-operation",
                    serde_json::json!({"operationId":operation_id,"result":result}),
                );
                if open_after_save {
                    let target = destination;
                    let data = bytes;
                    let opened = tauri::async_runtime::spawn_blocking(move || {
                        open_artifact_with(&target, identity, &data, native::dispatch)
                    })
                    .await;
                    if !matches!(opened, Ok(Ok(()))) {
                        result.open_failed = true;
                        result.message = "Workbook saved, but could not be opened.".into()
                    } else {
                        result.message = if preference_warning { "Workbook saved; opening request accepted, but save preferences could not be remembered." } else { "Workbook saved; opening request accepted." }.into();
                    }
                }
                result.operation_complete = true;
                result
            }
            Ok(Err(error)) => Outcome::new("failed", &error),
            Err(_) => Outcome::new(
                "unknown",
                "Publication worker completion is unknown. Reconcile status; do not replay.",
            ),
        }
    } else {
        Outcome::new("cancelled", "Save cancelled; prepared bytes retained.")
    };
    let mut s = state
        .0
        .lock()
        .map_err(|_| "State unavailable; reconcile operation.")?;
    s.record_operation(operation_id.clone(), result.clone());
    let close = s.release_work();
    drop(s);
    let _ = window.emit(
        "component-mrp-operation",
        serde_json::json!({"operationId":operation_id,"result":result}),
    );
    if close {
        let _ = window.close();
    }
    Ok(result)
}

#[tauri::command]
pub async fn component_mrp_open(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
    artifact_id: String,
    request_id: String,
) -> Result<(), String> {
    require_main(&window)?;
    if request_id.is_empty() || request_id.len() > 64 {
        return Err("Invalid Open request ID.".into());
    }
    let (target, id, bytes) = {
        let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
        match s.retry(&artifact_id, &request_id)? {
            Some(grant) => grant,
            None => return Ok(()),
        }
    };
    let result = tauri::async_runtime::spawn_blocking(move || {
        open_artifact_with(&target, id, &bytes, native::dispatch)
    })
    .await
    .map_err(|_| "Opening worker failed.".to_string())
    .and_then(|v| v);
    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
    if let Some(a) = s.artifacts.get_mut(&artifact_id) {
        a.retries.insert(request_id, result.is_ok());
    }
    drop(s);
    state.complete(&window);
    result
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn production_retry_grant_is_bounded_and_deduplicated_and_close_can_cancel() {
        let host = ReportPreferences::default();
        {
            let mut s = host.0.lock().unwrap();
            s.artifacts.insert(
                "artifact".into(),
                Artifact {
                    path: PathBuf::from("synthetic.xlsx"),
                    id: (1, 2),
                    bytes: Arc::new(vec![1]),
                    attempts: 0,
                    retries: HashMap::new(),
                },
            );
            assert!(s.retry("artifact", "request").unwrap().is_some());
            assert!(s.retry("artifact", "request").is_err());
        }
        assert!(host.defer_close());
        host.cancel_close();
        {
            let mut s = host.0.lock().unwrap();
            assert!(!s.close_pending);
            s.artifacts
                .get_mut("artifact")
                .unwrap()
                .retries
                .insert("request".into(), true);
            assert!(!s.release_work());
            for _ in 0..3 {
                assert!(s.retry("artifact", "request").unwrap().is_none());
            }
            assert_eq!(s.artifacts.get("artifact").unwrap().attempts, 1);
        }
    }
    #[test]
    fn controlled_artifact_launch_checks_content_and_delayed_success_failure() {
        for fail in [false, true] {
            let root = PathBuf::from(std::env::var("KST_MRP_TEST_ROOT").unwrap())
                .join(native::new_id().unwrap());
            std::fs::create_dir_all(&root).unwrap();
            let target = root.join("launch.xlsx");
            let id = native::publish(&target, b"synthetic B", false).unwrap();
            let host = Arc::new(ReportPreferences::default());
            host.0.lock().unwrap().busy = true;
            let worker = host.clone();
            let path = target.clone();
            let (started_tx, started_rx) = std::sync::mpsc::channel();
            let (resume_tx, resume_rx) = std::sync::mpsc::channel();
            let join = std::thread::spawn(move || {
                let mut saved = Outcome::new("saved", "opening");
                saved.operation_complete = false;
                worker
                    .0
                    .lock()
                    .unwrap()
                    .record_operation("op".into(), saved.clone());
                let result = open_artifact_with(&path, id, b"synthetic B", |_| {
                    started_tx.send(()).unwrap();
                    resume_rx.recv().unwrap();
                    if fail {
                        Err("controlled failure".into())
                    } else {
                        Ok(())
                    }
                });
                saved.operation_complete = true;
                saved.open_failed = result.is_err();
                let mut s = worker.0.lock().unwrap();
                s.record_operation("op".into(), saved);
                assert!(s.release_work());
            });
            started_rx.recv().unwrap();
            assert!(host.defer_close());
            for _ in 0..3 {
                assert!(
                    !host
                        .0
                        .lock()
                        .unwrap()
                        .operations
                        .get("op")
                        .unwrap()
                        .operation_complete
                );
            }
            resume_tx.send(()).unwrap();
            join.join().unwrap();
            assert_eq!(
                host.0
                    .lock()
                    .unwrap()
                    .operations
                    .get("op")
                    .unwrap()
                    .open_failed,
                fail
            );
            std::fs::write(&target, b"changed").unwrap();
            assert!(open_artifact_with(&target, id, b"synthetic B", |_| panic!(
                "changed artifact must not dispatch"
            ))
            .is_err());
        }
    }
    #[test]
    fn close_during_actual_staging_publication_and_commit_keeps_worker_owned() {
        for phase in [
            native::PublicationPhase::Staged,
            native::PublicationPhase::BeforeCommit,
            native::PublicationPhase::Committed,
        ] {
            let root = PathBuf::from(std::env::var("KST_MRP_TEST_ROOT").unwrap())
                .join(native::new_id().unwrap());
            std::fs::create_dir_all(&root).unwrap();
            let destination = root.join("close.xlsx");
            let target = destination.clone();
            let host = Arc::new(ReportPreferences::default());
            host.0.lock().unwrap().busy = true;
            let worker = host.clone();
            let (pause_tx, pause_rx) = std::sync::mpsc::channel();
            let (resume_tx, resume_rx) = std::sync::mpsc::channel();
            let join = std::thread::spawn(move || {
                let published =
                    native::publish_controlled(&target, b"synthetic B", false, false, |observed| {
                        if std::mem::discriminant(&observed) == std::mem::discriminant(&phase) {
                            pause_tx.send(()).unwrap();
                            resume_rx.recv().unwrap();
                        }
                    });
                let mut s = worker.0.lock().unwrap();
                s.record_operation(
                    "op".into(),
                    Outcome::new(
                        if published.is_ok() { "saved" } else { "failed" },
                        "terminal",
                    ),
                );
                assert!(s.release_work());
                published
            });
            pause_rx.recv().unwrap();
            assert!(host.defer_close());
            assert!(host.0.lock().unwrap().busy);
            resume_tx.send(()).unwrap();
            let id = join.join().unwrap().unwrap();
            native::verify_artifact(&destination, id, b"synthetic B").unwrap();
            assert!(
                host.0
                    .lock()
                    .unwrap()
                    .operations
                    .get("op")
                    .unwrap()
                    .operation_complete
            );
            assert!(!std::fs::read_dir(root).unwrap().any(|f| f
                .unwrap()
                .file_name()
                .to_string_lossy()
                .starts_with(".kst-mrp-")));
        }
    }
    #[test]
    fn close_barrier_waits_for_recorded_terminal_open_with_controlled_pause() {
        for phase in ["staging", "publication", "opening", "retry"] {
            let host = Arc::new(ReportPreferences::default());
            host.0.lock().unwrap().busy = true;
            assert!(host.defer_close(), "{phase}");
            let (committed_tx, committed_rx) = std::sync::mpsc::channel();
            let (finish_tx, finish_rx) = std::sync::mpsc::channel();
            let worker = host.clone();
            let join = std::thread::spawn(move || {
                let mut result = Outcome::new("saved", "Workbook saved; opening is in progress.");
                result.operation_complete = false;
                worker
                    .0
                    .lock()
                    .unwrap()
                    .record_operation("op".into(), result.clone());
                committed_tx.send(()).unwrap();
                finish_rx.recv().unwrap();
                result.operation_complete = true;
                result.open_failed = true;
                let mut s = worker.0.lock().unwrap();
                s.record_operation("op".into(), result);
                assert!(s.release_work()); // Same production terminal-record/release path.
            });
            committed_rx.recv().unwrap();
            {
                let s = host.0.lock().unwrap();
                assert!(s.busy);
                assert!(s.close_pending);
                assert!(!s.operations.get("op").unwrap().operation_complete);
            }
            assert!(host.defer_close());
            finish_tx.send(()).unwrap();
            join.join().unwrap();
            {
                let s = host.0.lock().unwrap();
                assert!(!s.busy);
                assert!(s.close_pending);
                assert!(s.operations.get("op").unwrap().open_failed);
            }
            assert!(!host.defer_close()); // Re-enters ordinary workspace discard/close handling.
        }
    }
    #[test]
    fn preferences_restart_and_failure() {
        let root = PathBuf::from(
            std::env::var("KST_MRP_TEST_ROOT").expect("Synthetic test root required"),
        )
        .join(native::new_id().unwrap());
        let file = root.join("prefs.json");
        let p = Preference {
            schema_version: 1,
            open_after_save: true,
            last_parent: Some(root.clone()),
        };
        persist(&file, &p).unwrap();
        let loaded = load(&file).unwrap();
        assert!(loaded.open_after_save);
        assert_eq!(loaded.last_parent, Some(root.clone()));
        assert!(persist(&root.join("prefs.json/invalid"), &p).is_err());
        assert!(p.open_after_save);
    }
    #[test]
    fn operation_status_is_a_retained_value_not_a_publication_or_dispatch() {
        let mut s = State::default();
        let mut saved = Outcome::new("saved", "Workbook saved.");
        saved.saved_artifact_id = Some("artifact".into());
        s.operations.insert("op".into(), saved.clone());
        for _ in 0..3 {
            let result = s.operations.get("op").unwrap();
            assert_eq!(result.outcome, "saved");
            assert_eq!(result.saved_artifact_id.as_deref(), Some("artifact"));
        }
        assert_eq!(s.operations.len(), 1);
        assert!(s.artifacts.is_empty());
        s.operations
            .insert("uncertain".into(), Outcome::new("unknown", "Do not replay"));
        assert_eq!(s.operations.get("uncertain").unwrap().outcome, "unknown");
    }
}
