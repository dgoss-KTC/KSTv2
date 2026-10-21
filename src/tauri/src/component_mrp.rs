//! Host-owned Component MRP preparation, operations, preferences and saved-artifact grants.
use crate::component_mrp_native as native;
use serde::{Deserialize, Serialize};
use std::{
    collections::HashMap,
    io::{Read, Seek, SeekFrom, Write},
    path::PathBuf,
    sync::{Arc, Mutex},
    time::{Duration, Instant},
};
use tauri::{Emitter, Manager};

const MAX_BYTES: usize = 8 * 1024 * 1024;
const HISTORICAL_MAX_PAGE_SIZE: u32 = 1000;
const HISTORICAL_MAX_REPLY_BYTES: usize = 20 * 1024 * 1024;
fn historical_page_limit(limit: Option<u32>) -> Result<u32, String> {
    let n = limit.unwrap_or(100);
    if n == 0 || n > HISTORICAL_MAX_PAGE_SIZE {
        return Err("Invalid page size.".into());
    }
    Ok(n)
}
const LIFETIME: Duration = Duration::from_secs(1800);
#[derive(Clone, Copy, Default, PartialEq, Eq, Deserialize, Serialize, Debug)]
pub enum ReportKind {
    #[default]
    ComponentMrp,
    ShipmentsToGo,
    Sop,
    OpenOrderReport,
    HistoricalShipments,
    Analytical,
    WorkspaceShortages,
}
impl ReportKind {
    fn route(self) -> &'static str {
        match self {
            Self::ComponentMrp => "component-mrp",
            Self::ShipmentsToGo => "shipments-to-go",
            Self::Sop => "sop",
            Self::OpenOrderReport => "open-orders",
            Self::HistoricalShipments => "historical-shipments",
            Self::Analytical => "analytical",
            Self::WorkspaceShortages => "workspace-shortages",
        }
    }
}
#[derive(Default, Deserialize, Serialize)]
#[serde(deny_unknown_fields, rename_all = "camelCase")]
struct Preference {
    schema_version: u32,
    open_after_save: bool,
    #[serde(default)]
    last_parent: Option<PathBuf>,
}
struct HeldFile {
    path: PathBuf,
    file: std::fs::File,
}
impl Drop for HeldFile {
    fn drop(&mut self) {
        let _ = std::fs::remove_file(&self.path);
    }
}
enum Payload {
    Memory(Vec<u8>),
    File(Mutex<HeldFile>),
}
impl Payload {
    fn publish(&self, target: &std::path::Path, replace: bool) -> Result<(u32, u64), String> {
        match self {
            Self::Memory(bytes) => native::publish_selected(target, bytes, replace, false),
            Self::File(held) => native::publish_stream_selected(
                target,
                &mut held.lock().map_err(|_| "Preparation unavailable.")?.file,
                replace,
            ),
        }
    }
    fn verify(&self, target: &std::path::Path, identity: (u32, u64)) -> Result<(), String> {
        match self {
            Self::Memory(bytes) => native::verify_artifact(target, identity, bytes),
            Self::File(held) => native::verify_artifact_stream(
                target,
                identity,
                &mut held.lock().map_err(|_| "Preparation unavailable.")?.file,
            ),
        }
    }
}
struct Preparation {
    kind: ReportKind,
    id: String,
    bytes: Arc<Payload>,
    name: String,
    created: Instant,
    backend: String,
}
struct Artifact {
    path: PathBuf,
    id: (u32, u64),
    bytes: Arc<Payload>,
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
    operation_kinds: HashMap<String, ReportKind>,
    artifact_kinds: HashMap<String, ReportKind>,
    departed: bool,
    sop_acquisition: Option<(String, String)>,
    historical_operation: Option<(String, String)>,
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
    fn discard(&mut self) {
        self.discard_sop();
        self.discard_historical();
        self.epoch += 1;
        self.prepared = None;
        self.departed = true;
        if !self.busy {
            self.discard_departed_artifacts();
        }
    }
    // The analytical pilot keeps saved grants (not preparations or destination authority) for
    // application-session Open recovery. Other Reports retain their accepted departure behavior.
    fn discard_departed_artifacts(&mut self) {
        self.artifacts
            .retain(|id, _| self.artifact_kinds.get(id) == Some(&ReportKind::Analytical));
        self.artifact_kinds
            .retain(|id, _| self.artifacts.contains_key(id));
    }
    fn can_open_artifact(&self, id: &str, kind: ReportKind) -> bool {
        self.artifact_kinds.get(id) == Some(&kind)
            && (!self.departed || kind == ReportKind::Analytical)
    }
    fn discard_sop(&mut self) {
        if let Some((id, url)) = self.sop_acquisition.take() {
            let session = self.session.clone();
            tauri::async_runtime::spawn(async move {
                if let Ok(client) = reqwest::Client::builder()
                    .redirect(reqwest::redirect::Policy::none())
                    .timeout(Duration::from_secs(10))
                    .build()
                {
                    let _ = client
                        .delete(format!("{url}/api/v1/reports/sop/acquisitions/{id}"))
                        .header("X-KST-Report-Session", session)
                        .send()
                        .await;
                }
            });
        }
    }
    fn discard_historical(&mut self) {
        if let Some((id, url)) = self.historical_operation.take() {
            let session = self.session.clone();
            tauri::async_runtime::spawn(async move {
                if let Ok(client) = reqwest::Client::builder()
                    .redirect(reqwest::redirect::Policy::none())
                    .timeout(Duration::from_secs(10))
                    .build()
                {
                    let _ = client
                        .delete(format!(
                            "{url}/api/v1/reports/historical-shipments/operations/{id}"
                        ))
                        .header("X-KST-Report-Session", session)
                        .send()
                        .await;
                }
            });
        }
    }
    fn retry(
        &mut self,
        artifact_id: &str,
        request_id: &str,
    ) -> Result<Option<(PathBuf, (u32, u64), Arc<Payload>)>, String> {
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
        if self.departed {
            self.discard_departed_artifacts();
        }
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
#[cfg(test)]
fn open_artifact_with(
    target: &std::path::Path,
    identity: (u32, u64),
    bytes: &[u8],
    launch: impl FnOnce(&std::path::Path) -> Result<(), String>,
) -> Result<(), String> {
    native::verify_artifact(target, identity, bytes)?;
    launch(target)
}
fn open_payload(
    target: &std::path::Path,
    identity: (u32, u64),
    payload: &Payload,
) -> Result<(), String> {
    payload.verify(target, identity)?;
    native::dispatch(target)
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
    s.discard_sop();
    s.discard_historical();
    s.prepared = None;
    s.departed = false;
    if s.session.is_empty() {
        s.session = native::new_id()?
    }
    Ok(s.epoch)
}
/// Departure invalidates acquisition immediately, even while a save worker owns bytes.
#[tauri::command]
pub fn report_discard(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
) -> Result<(), String> {
    require_main(&window)?;
    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
    s.discard();
    Ok(())
}

/// Fixed-route, managed-sidecar Historical Shipments transport. No caller URL/path authority.
#[tauri::command]
pub async fn historical_shipments_request(
    window: tauri::WebviewWindow,
    app: tauri::AppHandle,
    state: tauri::State<'_, ReportPreferences>,
    backend: tauri::State<'_, crate::SharedBackendState>,
    epoch: u64,
    action: String,
    operation_id: String,
    request: Option<serde_json::Value>,
    offset: Option<u64>,
    limit: Option<u32>,
) -> Result<serde_json::Value, String> {
    require_main(&window)?;
    if ![
        "register", "acquire", "status", "page", "prepare", "release",
    ]
    .contains(&action.as_str())
        || operation_id.len() != 36
        || !operation_id
            .chars()
            .all(|c| c.is_ascii_hexdigit() || c == '-')
    {
        return Err("Invalid Historical Shipments action/identity.".into());
    }
    {
        let mut active = state.1.lock().map_err(|_| "Transport state unavailable.")?;
        if *active {
            return Err("Report transport already active.".into());
        }
        *active = true;
    }
    let _lease = GenerationLease(&state, &window);
    let url = {
        let b = backend.lock().await;
        b.active
            .as_ref()
            .filter(|p| p.ready)
            .and_then(|p| p.base_url.clone())
            .ok_or("Managed backend unavailable.")?
    };
    let session = {
        let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
        if s.epoch != epoch || s.departed || s.busy || s.close_pending {
            return Err("Historical Shipments operation superseded or save is active.".into());
        }
        if action == "register" {
            s.discard_historical();
            s.prepared = None;
            s.historical_operation = Some((operation_id.clone(), url.clone()));
        } else if !s
            .historical_operation
            .as_ref()
            .is_some_and(|(id, held)| id == &operation_id && held == &url)
        {
            return Err("Historical Shipments operation is not owned by this visit.".into());
        }
        s.session.clone()
    };
    // Hold close for acquisition/transfer. Concurrent commands fail rather than competing for publication.
    let client = reqwest::Client::builder()
        .redirect(reqwest::redirect::Policy::none())
        .timeout(Duration::from_secs(120))
        .build()
        .map_err(|_| "Transport unavailable.")?;
    let base = format!("{url}/api/v1/reports/historical-shipments/operations/{operation_id}");
    let result = async {
        let builder = match action.as_str() {
            "register" => client.put(&base),
            "release" => client.delete(&base),
            "status" => client.get(&base),
            "page" => {
                let n = historical_page_limit(limit)?;
                client.get(format!(
                    "{base}/rows?offset={}&limit={n}",
                    offset.unwrap_or(0)
                ))
            }
            "acquire" => {
                let value = request.as_ref().ok_or("Missing report filters.")?;
                if serde_json::to_vec(value)
                    .map_err(|_| "Invalid filters.")?
                    .len()
                    > 65536
                {
                    return Err("Report inputs exceed capacity.".into());
                }
                client.post(format!("{base}/acquire")).json(value)
            }
            "prepare" => client.post(format!("{base}/prepare")),
            _ => unreachable!(),
        };
        let mut response = builder
            .header("X-KST-Report-Session", &session)
            .send()
            .await
            .map_err(|_| "Historical Shipments transport failed.")?;
        let success = response.status().is_success();
        let mut metadata = Vec::new();
        while let Some(chunk) = response
            .chunk()
            .await
            .map_err(|_| "Historical Shipments reply incomplete.")?
        {
            // Five times the former 200-row bound; still a hard transport ceiling, not full history.
            if metadata.len() + chunk.len() > HISTORICAL_MAX_REPLY_BYTES {
                return Err(
                    "Historical Shipments page/reply exceeds bounded transport capacity.".into(),
                );
            }
            metadata.extend_from_slice(&chunk);
        }
        let value = if metadata.is_empty() {
            serde_json::Value::Null
        } else {
            serde_json::from_slice::<serde_json::Value>(&metadata)
                .map_err(|_| "Invalid Historical Shipments reply.")?
        };
        if !success {
            return Err(value["detail"]
                .as_str()
                .filter(|s| s.len() <= 4096)
                .unwrap_or(
                    "Historical Shipments operation failed. No complete result was published.",
                )
                .to_string());
        }
        {
            let s = state.0.lock().map_err(|_| "State unavailable.")?;
            if s.epoch != epoch || s.departed {
                return Err("Historical Shipments reply superseded.".into());
            }
        }
        if action == "prepare" {
            if value["preparationId"].as_str() != Some(operation_id.as_str()) {
                return Err("Invalid preparation identity.".into());
            }
            let expected = value["byteLength"]
                .as_u64()
                .ok_or("Invalid workbook length.")?;
            let mut response = client
                .get(format!(
                    "{url}/api/v1/reports/historical-shipments/preparations/{operation_id}/bytes"
                ))
                .header("X-KST-Report-Session", &session)
                .send()
                .await
                .map_err(|_| "Workbook transfer failed.")?;
            if !response.status().is_success()
                || response
                    .headers()
                    .get(reqwest::header::CONTENT_TYPE)
                    .and_then(|v| v.to_str().ok())
                    != Some("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return Err("Workbook transfer unavailable.".into());
            }
            let directory = app
                .path()
                .app_cache_dir()
                .map_err(|_| "Report cache unavailable.")?
                .join("report-transport");
            std::fs::create_dir_all(&directory).map_err(|_| "Report cache unavailable.")?;
            let path = directory.join(format!("{}.xlsx", native::new_id()?));
            use std::os::windows::fs::OpenOptionsExt;
            let file = std::fs::OpenOptions::new()
                .read(true)
                .write(true)
                .create_new(true)
                .access_mode(0x80000000 | 0x40000000 | 0x10000)
                .share_mode(1)
                .custom_flags(0x04000000)
                .open(&path)
                .map_err(|_| "Private workbook staging failed.")?;
            let mut held = HeldFile { path, file };
            let mut received = 0u64;
            while let Some(chunk) = response
                .chunk()
                .await
                .map_err(|_| "Workbook transfer incomplete.")?
            {
                {
                    let s = state.0.lock().map_err(|_| "State unavailable.")?;
                    if s.epoch != epoch || s.departed {
                        return Err("Workbook transfer superseded.".into());
                    }
                }
                received = received
                    .checked_add(chunk.len() as u64)
                    .ok_or("Workbook length overflow.")?;
                if received > expected {
                    return Err("Workbook transfer length mismatch.".into());
                }
                held.file
                    .write_all(&chunk)
                    .map_err(|_| "Workbook cache write failed.")?;
            }
            if received != expected || received < 4 {
                return Err("Workbook transfer incomplete.".into());
            }
            held.file
                .seek(SeekFrom::Start(0))
                .map_err(|_| "Workbook seek failed.")?;
            let mut magic = [0u8; 2];
            held.file
                .read_exact(&mut magic)
                .map_err(|_| "Workbook invalid.")?;
            if &magic != b"PK" {
                return Err("Workbook invalid.".into());
            }
            held.file.sync_all().map_err(|_| "Workbook flush failed.")?;
            let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
            if s.epoch != epoch || s.departed || s.busy {
                return Err("Workbook preparation superseded.".into());
            }
            s.prepared = Some(Preparation {
                kind: ReportKind::HistoricalShipments,
                id: operation_id.clone(),
                bytes: Arc::new(Payload::File(Mutex::new(held))),
                name: "Historical-Shipments_01.xlsx".into(),
                created: Instant::now(),
                backend: url.clone(),
            });
            let expiry_payload = Arc::downgrade(&s.prepared.as_ref().unwrap().bytes);
            drop(s);
            let expiry_app = app.clone();
            let expiry_id = operation_id.clone();
            tauri::async_runtime::spawn(async move {
                tokio::time::sleep(LIFETIME).await;
                let state = expiry_app.state::<ReportPreferences>();
                if let Ok(mut s) = state.0.lock() {
                    if s.prepared.as_ref().is_some_and(|p| {
                        p.kind == ReportKind::HistoricalShipments && p.id == expiry_id
                    }) {
                        s.prepared = None;
                    }
                    if s.historical_operation
                        .as_ref()
                        .is_some_and(|(id, _)| id == &expiry_id)
                    {
                        s.discard_historical();
                    }
                    let expired_grants: Vec<_> = s
                        .artifacts
                        .iter()
                        .filter(|(_, artifact)| {
                            expiry_payload.ptr_eq(&Arc::downgrade(&artifact.bytes))
                        })
                        .map(|(id, _)| id.clone())
                        .collect();
                    for id in expired_grants {
                        s.artifacts.remove(&id);
                        s.artifact_kinds.remove(&id);
                    }
                };
            });
        }
        Ok(value)
    }
    .await;
    if result.is_err() || action == "release" {
        let _ = client
            .delete(&base)
            .header("X-KST-Report-Session", &session)
            .send()
            .await;
        let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
        if s.epoch == epoch
            && s.historical_operation
                .as_ref()
                .is_some_and(|(id, _)| id == &operation_id)
        {
            s.historical_operation = None;
            s.prepared = None;
        }
    }
    result
}
// A fixed Component MRP payload, not arbitrary webview JSON/URL authority.
fn mrp_request(
    site: &str,
    part: &str,
    weeks: u32,
    include: bool,
    period: &str,
    acquisition_id: Option<&str>,
    as_of_date: Option<&str>,
) -> Result<serde_json::Value, String> {
    if part.trim().is_empty()
        || part.chars().count() > 256
        || part.chars().any(char::is_control)
        || !["SW", "NW", "AR", "MN", "MS", "KV"].contains(&site)
        || ![13, 26, 52, 72].contains(&weeks)
        || !["Day", "Week", "Month"].contains(&period)
        || acquisition_id.is_some_and(|id| {
            id.len() != 36 || !id.chars().all(|c| c.is_ascii_hexdigit() || c == '-')
        })
        || as_of_date.is_some_and(|date| {
            date.len() != 10 || !date.chars().all(|c| c.is_ascii_digit() || c == '-')
        })
        || acquisition_id.is_some() != as_of_date.is_some()
    {
        return Err("Invalid Component MRP request.".into());
    }
    Ok(
        serde_json::json!({"site":site,"part":part,"horizonWeeks":weeks,"includeUnconfirmed":include,
        "period":period,"acquisitionId":acquisition_id,"asOfDate":as_of_date}),
    )
}

fn generation_route(
    kind: ReportKind,
    screen: bool,
    retained: bool,
    sop_prepare: bool,
) -> &'static str {
    if screen {
        "component-mrp/projection"
    } else if kind == ReportKind::ComponentMrp && retained {
        "component-mrp/screen-preparations"
    } else if kind == ReportKind::Sop {
        if sop_prepare {
            "sop/preparations"
        } else {
            "sop/acquisitions"
        }
    } else {
        kind.route()
    }
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
    acquisition_id: Option<String>,
    as_of_date: Option<String>,
    screen_read: Option<bool>,
    report_kind: Option<ReportKind>,
    sites: Option<Vec<String>>,
    include_next_week: Option<bool>,
    sop_request: Option<serde_json::Value>,
    sop_prepare: Option<serde_json::Value>,
    oor_request: Option<serde_json::Value>,
    analytical_request: Option<serde_json::Value>,
) -> Result<serde_json::Value, String> {
    require_main(&window)?;
    let kind = report_kind.unwrap_or_default();
    let screen = screen_read.unwrap_or(false);
    if (screen || acquisition_id.is_some() || as_of_date.is_some())
        && kind != ReportKind::ComponentMrp
    {
        return Err("Screen acquisition belongs to Component MRP only.".into());
    }
    if kind == ReportKind::HistoricalShipments {
        return Err("Use Historical Shipments operation transport.".into());
    }
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
    if kind == ReportKind::ComponentMrp
        && (part.len() > 1024
            || !["SW", "NW", "AR", "MN", "MS", "KV"].contains(&site.as_str())
            || ![13, 26, 52, 72].contains(&horizon_weeks)
            || !["Day", "Week", "Month"].contains(&period.as_str()))
    {
        return Err("Invalid report request.".into());
    }
    if kind == ReportKind::ShipmentsToGo
        && !sites.as_ref().is_some_and(|v| {
            !v.is_empty()
                && v.len() <= 6
                && v.iter()
                    .all(|s| ["SW", "NW", "AR", "MN", "MS", "KV"].contains(&s.as_str()))
                && v.iter().collect::<std::collections::HashSet<_>>().len() == v.len()
        })
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
    let route = kind.route();
    let request = if matches!(
        kind,
        ReportKind::Analytical | ReportKind::WorkspaceShortages
    ) {
        analytical_request.ok_or("Missing analytical export selections.")?
    } else if kind == ReportKind::OpenOrderReport {
        oor_request.ok_or("Missing Open Order Report inputs.")?
    } else if kind == ReportKind::Sop {
        if let Some(ref preparation) = sop_prepare {
            let id = preparation["acquisitionId"]
                .as_str()
                .ok_or("Invalid acquisition ID.")?;
            let s = state.0.lock().map_err(|_| "State unavailable.")?;
            if !s
                .sop_acquisition
                .as_ref()
                .is_some_and(|(held, _)| held == id)
            {
                return Err("Acquisition not owned by this report generation.".into());
            }
            preparation.clone()
        } else {
            sop_request.clone().ok_or("Missing S&OP inputs.")?
        }
    } else if kind == ReportKind::ComponentMrp {
        mrp_request(
            &site,
            &part,
            horizon_weeks,
            include_unconfirmed,
            &period,
            acquisition_id.as_deref(),
            as_of_date.as_deref(),
        )?
    } else {
        serde_json::json!({"sites":sites,"includeNextWeek":include_next_week.unwrap_or(false)})
    };
    if serde_json::to_vec(&request)
        .map_err(|_| "Invalid report inputs.")?
        .len()
        > if matches!(
            kind,
            ReportKind::Analytical | ReportKind::WorkspaceShortages
        ) {
            16 * 1024 * 1024
        } else {
            65536
        }
    {
        return Err("Report inputs exceed capacity.".into());
    }
    let action_route = generation_route(
        kind,
        screen,
        acquisition_id.is_some(),
        sop_prepare.is_some(),
    );
    let mut response = client
        .post(format!("{url}/api/v1/reports/{action_route}"))
        .header("X-KST-Report-Session", &session)
        .json(&request)
        .send()
        .await
        .map_err(|_| "Generation transport failed.")?;
    if !response.status().is_success() {
        if kind == ReportKind::ComponentMrp && response.status().as_u16() == 410 {
            return Err("MRP acquisition expired, changed or is not owned by this session. Refresh MRP before exporting.".into());
        }
        if kind == ReportKind::OpenOrderReport {
            // Only the backend's validation messages cross this fixed route; source details never do.
            return Err(if response.status().as_u16() == 400 {
                let mut raw = Vec::new();
                while let Some(chunk) = response
                    .chunk()
                    .await
                    .map_err(|_| "Validation response incomplete.")?
                {
                    if raw.len() + chunk.len() > 8192 {
                        return Err("Validation response exceeds capacity.".into());
                    }
                    raw.extend_from_slice(&chunk);
                }
                let body = serde_json::from_slice::<serde_json::Value>(&raw).unwrap_or_default();
                body["errors"]["inputs"]
                    .as_array()
                    .map(|v| {
                        v.iter()
                            .filter_map(|m| m.as_str())
                            .collect::<Vec<_>>()
                            .join(" ")
                    })
                    .filter(|s| !s.is_empty() && s.len() <= 4096)
                    .unwrap_or_else(|| {
                        "Invalid Open Order Report inputs. Review filters and Generate again."
                            .into()
                    })
            } else {
                "Open Order Report acquisition/preparation failed: source/schema failure or report capacity. No workbook was prepared.".into()
            });
        }
        if kind == ReportKind::Sop {
            return Err(if response.status().as_u16() == 400 {
                "S&OP inputs or fiscal presentation are invalid. Review settings and Generate again."
            } else {
                "S&OP acquisition/preparation failed: unsupported procedure output, source failure or report capacity. No workbook was prepared."
            }.into());
        }
        if kind == ReportKind::WorkspaceShortages {
            return Err("Loaded Workspace Shortages projection is unavailable, changed or exceeds capacity. Reload the view and prepare again. No source refresh was performed.".into());
        }
        return Err(if response.status().as_u16() == 404 {
            "Part not found in selected domain."
        } else {
            "Report acquisition failed. Retry Generate."
        }
        .into());
    }
    let mut metadata = Vec::new();
    while let Some(chunk) = response
        .chunk()
        .await
        .map_err(|_| "Preparation metadata incomplete.")?
    {
        if metadata.len() + chunk.len()
            > (if screen {
                512 * 1024
            } else if kind == ReportKind::Sop {
                65536
            } else {
                4096
            })
        {
            return Err("Preparation metadata exceeds capacity.".into());
        }
        metadata.extend_from_slice(&chunk)
    }
    let result: serde_json::Value =
        serde_json::from_slice(&metadata).map_err(|_| "Invalid preparation metadata.")?;
    if screen {
        let s = state.0.lock().map_err(|_| "State unavailable.")?;
        if s.epoch != epoch || s.departed || s.busy || s.close_pending {
            return Err("MRP screen request superseded; result discarded.".into());
        }
        if result["site"] != site
            || result["part"] != part
            || result["horizonWeeks"] != horizon_weeks
            || result["period"] != period
            || result["includeUnconfirmed"] != include_unconfirmed
            || !result["periods"]
                .as_array()
                .is_some_and(|p| !p.is_empty() && p.len() <= 506)
        {
            return Err("Invalid MRP screen identity/settings.".into());
        }
        return Ok(result);
    }
    if kind != ReportKind::ComponentMrp && result["outcome"] == "noData" {
        let s = state.0.lock().map_err(|_| "State unavailable.")?;
        if s.epoch != epoch || s.departed {
            return Err("Generation superseded; result discarded.".into());
        }
        return Ok(result);
    }
    if kind == ReportKind::Sop && sop_prepare.is_none() {
        let id = result["acquisitionId"]
            .as_str()
            .ok_or("Invalid acquisition ID.")?
            .to_string();
        if id.len() != 36 || !id.chars().all(|c| c.is_ascii_hexdigit() || c == '-') {
            return Err("Invalid acquisition ID.".into());
        }
        let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
        s.sop_acquisition = Some((id, url));
        if s.epoch != epoch || s.departed || s.busy {
            s.discard_sop();
            return Err("Generation superseded; acquisition discarded.".into());
        }
        return Ok(result);
    }
    let preparation_id = result["preparationId"]
        .as_str()
        .ok_or("Invalid preparation ID.")?
        .to_string();
    let file_name = result["fileName"]
        .as_str()
        .ok_or("Invalid suggested filename.")?
        .to_string();
    let prepared_id = preparation_id;
    if prepared_id.len() != 36
        || !prepared_id
            .chars()
            .all(|c| c.is_ascii_hexdigit() || c == '-')
    {
        return Err("Invalid preparation ID.".into());
    }
    if file_name.len() > 220
        || file_name.contains(['/', '\\', ':'])
        || !file_name.ends_with(".xlsx")
    {
        return Err("Invalid suggested filename.".into());
    }
    let mut response = client
        .get(format!(
            "{url}/api/v1/reports/{route}/preparations/{}/bytes",
            prepared_id
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
    if s.epoch != epoch || s.busy || s.departed {
        return Err("Generation superseded; result discarded.".into());
    }
    s.prepared = Some(Preparation {
        kind,
        id: prepared_id,
        bytes: Arc::new(Payload::Memory(bytes)),
        name: file_name,
        created: Instant::now(),
        backend: url,
    });
    Ok(result)
}

#[tauri::command]
pub fn sop_discard_generation(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
    epoch: u64,
) -> Result<(), String> {
    require_main(&window)?;
    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
    if s.epoch == epoch {
        s.discard();
    }
    Ok(())
}

#[tauri::command]
pub fn sop_discard_acquisition(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
    acquisition_id: String,
) -> Result<(), String> {
    require_main(&window)?;
    let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
    if s.sop_acquisition
        .as_ref()
        .is_some_and(|(id, _)| id == &acquisition_id)
    {
        s.discard_sop();
    }
    Ok(())
}

#[tauri::command]
pub fn component_mrp_status(
    window: tauri::WebviewWindow,
    state: tauri::State<'_, ReportPreferences>,
    operation_id: String,
    report_kind: Option<ReportKind>,
) -> Result<Outcome, String> {
    require_main(&window)?;
    let s = state.0.lock().map_err(|_| "State unavailable.")?;
    if s.operation_kinds
        .get(&operation_id)
        .copied()
        .unwrap_or_default()
        != report_kind.unwrap_or_default()
    {
        return Err("Operation belongs to another report.".into());
    }
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
    report_kind: Option<ReportKind>,
) -> Result<Outcome, String> {
    require_main(&window)?;
    let kind = report_kind.unwrap_or_default();
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
            if s.operation_kinds
                .get(&operation_id)
                .copied()
                .unwrap_or_default()
                != kind
            {
                return Err("Operation belongs to another report.".into());
            }
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
        s.operation_kinds.insert(operation_id.clone(), kind);
        let valid = s.prepared.as_ref().is_some_and(|p| {
            p.id == preparation_id
                && p.kind == kind
                && !s.departed
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
        match tauri::async_runtime::spawn_blocking(move || data.publish(&target, replace)).await {
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
                    s.artifact_kinds.insert(artifact.clone(), kind);
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
                        open_payload(&target, identity, &data)
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
    report_kind: Option<ReportKind>,
) -> Result<(), String> {
    require_main(&window)?;
    if request_id.is_empty() || request_id.len() > 64 {
        return Err("Invalid Open request ID.".into());
    }
    let (target, id, bytes) = {
        let mut s = state.0.lock().map_err(|_| "State unavailable.")?;
        if !s.can_open_artifact(&artifact_id, report_kind.unwrap_or_default()) {
            return Err("Saved artifact grant unavailable for this report.".into());
        }
        match s.retry(&artifact_id, &request_id)? {
            Some(grant) => grant,
            None => return Ok(()),
        }
    };
    let result = tauri::async_runtime::spawn_blocking(move || open_payload(&target, id, &bytes))
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
    #[test]
    fn mrp_screen_payload_forwards_retained_identity_and_bounds_webview_inputs() {
        assert_eq!(
            generation_route(ReportKind::ComponentMrp, true, false, false),
            "component-mrp/projection"
        );
        assert_eq!(
            generation_route(ReportKind::ComponentMrp, true, true, false),
            "component-mrp/projection"
        );
        assert_eq!(
            generation_route(ReportKind::ComponentMrp, false, true, false),
            "component-mrp/screen-preparations"
        );
        assert_eq!(
            generation_route(ReportKind::ComponentMrp, false, false, false),
            "component-mrp"
        );
        assert_eq!(
            generation_route(ReportKind::Sop, false, false, true),
            "sop/preparations"
        );
        assert_eq!(
            generation_route(ReportKind::ShipmentsToGo, false, false, false),
            "shipments-to-go"
        );
        let id = "11111111-1111-4111-8111-111111111111";
        let value = mrp_request(
            "SW",
            "EXACT",
            72,
            false,
            "Month",
            Some(id),
            Some("2026-09-29"),
        )
        .unwrap();
        assert_eq!(value["acquisitionId"], id);
        assert_eq!(value["asOfDate"], "2026-09-29");
        assert_eq!(value["includeUnconfirmed"], false);
        assert!(value.get("session").is_none());
        assert!(mrp_request("KS", "EXACT", 26, true, "Week", None, None).is_err());
        assert!(mrp_request("SW", "EXACT", 27, true, "Week", None, None).is_err());
        assert!(mrp_request("SW", "EXACT", 26, true, "Year", None, None).is_err());
        assert!(mrp_request(
            "SW",
            "EXACT",
            26,
            true,
            "Week",
            Some("../../x"),
            Some("2026-09-29")
        )
        .is_err());
        assert!(mrp_request("SW", "EXACT", 26, true, "Week", Some(id), None).is_err());
        assert!(mrp_request("SW", "bad\npart", 26, true, "Week", None, None).is_err());
        assert!(mrp_request("SW", &"x".repeat(257), 26, true, "Week", None, None).is_err());
        assert!(
            mrp_request("SW", "EXACT", 13, true, "Day", None, None).unwrap()["acquisitionId"]
                .is_null()
        );
    }
    use super::*;
    #[test]
    fn historical_pages_are_explicitly_bounded_for_all_three_report_sizes() {
        assert_eq!(historical_page_limit(None).unwrap(), 100);
        for n in [100, 500, 1000] {
            assert_eq!(historical_page_limit(Some(n)).unwrap(), n);
        }
        for n in [0, 1001, u32::MAX] {
            assert!(historical_page_limit(Some(n)).is_err());
        }
        assert_eq!(HISTORICAL_MAX_REPLY_BYTES, 20 * 1024 * 1024);
    }
    #[test]
    fn historical_file_payload_survives_departure_only_while_worker_owns_it() {
        use std::os::windows::fs::OpenOptionsExt;
        let root =
            PathBuf::from(std::env::var("KST_MRP_TEST_ROOT").expect("Synthetic root required"));
        let path = root.join(format!("{}.tmp", native::new_id().unwrap()));
        let file = std::fs::OpenOptions::new()
            .read(true)
            .write(true)
            .create_new(true)
            .access_mode(0x80000000 | 0x40000000 | 0x10000)
            .share_mode(1)
            .custom_flags(0x04000000)
            .open(&path)
            .unwrap();
        let payload = Arc::new(Payload::File(Mutex::new(HeldFile {
            path: path.clone(),
            file,
        })));
        let mut state = State::default();
        state.prepared = Some(Preparation {
            kind: ReportKind::HistoricalShipments,
            id: "p".into(),
            bytes: payload.clone(),
            name: "s.xlsx".into(),
            created: Instant::now(),
            backend: "managed".into(),
        });
        state.busy = true;
        state.discard();
        assert!(state.prepared.is_none());
        assert!(path.exists());
        state.record_operation("save".into(), Outcome::new("saved", "Workbook saved"));
        state.release_work();
        assert!(path.exists());
        drop(payload);
        assert!(!path.exists());
    }
    #[test]
    fn report_kind_routes_and_departure_retain_only_recovery_and_worker_ownership() {
        assert_eq!(ReportKind::ComponentMrp.route(), "component-mrp");
        assert_eq!(ReportKind::ShipmentsToGo.route(), "shipments-to-go");
        assert_eq!(ReportKind::Sop.route(), "sop");
        assert_eq!(ReportKind::OpenOrderReport.route(), "open-orders");
        assert_eq!(
            ReportKind::WorkspaceShortages.route(),
            "workspace-shortages"
        );
        assert_ne!(ReportKind::WorkspaceShortages, ReportKind::Analytical);
        assert_ne!(ReportKind::OpenOrderReport, ReportKind::ShipmentsToGo);
        assert_ne!(ReportKind::Sop, ReportKind::ShipmentsToGo);
        assert!(serde_json::from_str::<ReportKind>("\"https://other\"").is_err());
        let mut s = State::default();
        s.prepared = Some(Preparation {
            kind: ReportKind::ShipmentsToGo,
            id: "p".into(),
            bytes: Arc::new(Payload::Memory(vec![1])),
            name: "s.xlsx".into(),
            created: Instant::now(),
            backend: "managed".into(),
        });
        s.operation_kinds
            .insert("o".into(), ReportKind::ShipmentsToGo);
        s.record_operation("o".into(), Outcome::new("unknown", "Do not replay"));
        s.artifacts.insert(
            "a".into(),
            Artifact {
                path: PathBuf::from("s.xlsx"),
                id: (1, 2),
                bytes: Arc::new(Payload::Memory(vec![1])),
                attempts: 0,
                retries: HashMap::new(),
            },
        );
        s.artifact_kinds
            .insert("a".into(), ReportKind::ShipmentsToGo);
        s.busy = true;
        s.discard();
        assert!(s.prepared.is_none());
        assert_eq!(s.epoch, 1);
        assert!(s.busy);
        assert!(s.artifacts.contains_key("a"));
        s.release_work();
        assert!(s.artifacts.is_empty());
        assert!(s.artifact_kinds.is_empty());
        assert_eq!(s.operations["o"].outcome, "unknown");
        assert_eq!(s.operation_kinds["o"], ReportKind::ShipmentsToGo);
        assert_ne!(s.operation_kinds["o"], ReportKind::ComponentMrp);
    }
    #[test]
    fn open_order_preparation_and_recovery_are_kind_bound_on_departure() {
        let mut s = State::default();
        s.prepared = Some(Preparation {
            kind: ReportKind::OpenOrderReport,
            id: "oor".into(),
            bytes: Arc::new(Payload::Memory(vec![1])),
            name: "Open-Orders_2026-10-12_01.xlsx".into(),
            created: Instant::now(),
            backend: "managed".into(),
        });
        assert_ne!(s.prepared.as_ref().unwrap().kind, ReportKind::Sop);
        s.operation_kinds
            .insert("op".into(), ReportKind::OpenOrderReport);
        s.record_operation("op".into(), Outcome::new("unknown", "Do not replay"));
        s.discard();
        assert!(s.prepared.is_none());
        assert!(s.departed);
        assert_eq!(s.operation_kinds["op"], ReportKind::OpenOrderReport);
        assert_eq!(s.operations["op"].outcome, "unknown");
    }
    #[test]
    fn analytical_saved_grants_survive_departure_but_preparations_and_other_kinds_do_not() {
        for busy in [false, true] {
            let mut s = State::default();
            s.prepared = Some(Preparation {
                kind: ReportKind::Analytical,
                id: "prepared".into(),
                bytes: Arc::new(Payload::Memory(vec![1])),
                name: "MPS.xlsx".into(),
                created: Instant::now(),
                backend: "managed".into(),
            });
            for (id, kind) in [
                ("analytical", ReportKind::Analytical),
                ("mrp", ReportKind::ComponentMrp),
                ("stg", ReportKind::ShipmentsToGo),
                ("sop", ReportKind::Sop),
                ("oor", ReportKind::OpenOrderReport),
                ("historical", ReportKind::HistoricalShipments),
                ("shortages", ReportKind::WorkspaceShortages),
            ] {
                s.artifacts.insert(
                    id.into(),
                    Artifact {
                        path: PathBuf::from("host-owned.xlsx"),
                        id: (1, 2),
                        bytes: Arc::new(Payload::Memory(vec![1])),
                        attempts: 0,
                        retries: HashMap::new(),
                    },
                );
                s.artifact_kinds.insert(id.into(), kind);
            }
            s.busy = busy;
            s.discard();
            assert!(s.prepared.is_none());
            assert!(s.departed);
            if busy {
                s.release_work();
            }
            assert!(s.artifacts.contains_key("analytical"));
            assert!(!s.artifacts.contains_key("mrp"));
            for (id, kind) in [
                ("stg", ReportKind::ShipmentsToGo),
                ("sop", ReportKind::Sop),
                ("oor", ReportKind::OpenOrderReport),
                ("historical", ReportKind::HistoricalShipments),
                ("shortages", ReportKind::WorkspaceShortages),
            ] {
                assert!(!s.artifacts.contains_key(id));
                assert!(!s.can_open_artifact("analytical", kind));
                assert!(!s.can_open_artifact(id, kind));
            }
            assert!(s.can_open_artifact("analytical", ReportKind::Analytical));
            assert!(!s.can_open_artifact("analytical", ReportKind::ComponentMrp));
            assert!(!s.can_open_artifact("missing", ReportKind::Analytical));
            assert!(!s.can_open_artifact("mrp", ReportKind::ComponentMrp));
            assert!(s.retry("analytical", "retry-1").unwrap().is_some());
            s.busy = false;
            assert!(s.retry("analytical", "retry-1").is_err());
        }
    }
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
                    bytes: Arc::new(Payload::Memory(vec![1])),
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
