use std::sync::Arc;
use std::time::Duration;
use tauri::{AppHandle, Emitter, Manager};
use tauri_plugin_shell::ShellExt;
use tauri_plugin_shell::process::{CommandChild, CommandEvent, TerminatedPayload};
use serde::{Deserialize, Serialize};
use tokio::sync::Mutex;
use log::{error, info, warn};

const HANDSHAKE_TIMEOUT: Duration = Duration::from_secs(30);
const READINESS_ATTEMPTS: u32 = 30;
const READINESS_INTERVAL: Duration = Duration::from_secs(1);
const SHUTDOWN_TIMEOUT: Duration = Duration::from_secs(5);

/// Name of the KST backend sidecar, matching the sole entry declared in
/// `tauri.conf.json` (`bundle.externalBin`: `binaries/Kst.Api`). This is the only
/// external executable the KST Tauri host launches. See
/// `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`.
const BACKEND_SIDECAR_NAME: &str = "Kst.Api";

type SharedBackendState = Arc<Mutex<BackendRuntimeState>>;

/// Startup handshake written by the backend to stdout.
#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct BackendHandshake {
    port: u16,
    instance_id: String,
    #[allow(dead_code)]
    status: String,
}

/// Runtime state shared across the Tauri app.
#[derive(Default)]
pub struct BackendRuntimeState {
    active: Option<ActiveBackendProcess>,
    launch_in_progress: bool,
    shutdown_in_progress: bool,
}

struct ActiveBackendProcess {
    child: CommandChild,
    pid: u32,
    port: Option<u16>,
    instance_id: Option<String>,
    base_url: Option<String>,
    ready: bool,
    expected_shutdown: bool,
}

/// Emitted to the frontend when the backend URL is known.
#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct BackendReadyEvent {
    base_url: String,
    port: u16,
    instance_id: String,
}

/// Emitted to the frontend when the backend becomes unavailable.
#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct BackendUnavailableEvent {
    reason: String,
    pid: Option<u32>,
    expected: bool,
    code: Option<i32>,
    signal: Option<i32>,
}

/// Tauri command: returns the current backend base URL.
#[tauri::command]
async fn get_backend_url(
    state: tauri::State<'_, SharedBackendState>,
) -> Result<Option<String>, String> {
    let guard = state.lock().await;
    let base_url = guard.active.as_ref().and_then(|active| {
        if active.ready {
            active.base_url.clone()
        } else {
            None
        }
    });

    Ok(base_url)
}

pub fn run() {
    let backend_state: SharedBackendState = Arc::new(Mutex::new(BackendRuntimeState::default()));

    let app = tauri::Builder::default()
        .plugin(tauri_plugin_single_instance::init(|app, args, cwd| {
            info!(
                "KST Tauri: second instance launch intercepted; args={:?} cwd={}",
                args,
                cwd
            );

            if let Some(window) = app.get_webview_window("main") {
                let _ = window.unminimize();
                let _ = window.show();
                let _ = window.set_focus();
            }
        }))
        .plugin(tauri_plugin_shell::init())
        .manage(backend_state)
        .setup(|app| {
            let app_handle = app.handle().clone();
            let state = app.state::<SharedBackendState>().inner().clone();

            #[cfg(debug_assertions)]
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.set_title("Keytronic Scheduler's Toolbox [DEV]");
            }

            // Launch the backend sidecar in a background task.
            tauri::async_runtime::spawn(async move {
                launch_backend(app_handle, state).await;
            });

            Ok(())
        })
        .on_window_event(|window, event| {
            if let tauri::WindowEvent::CloseRequested { .. } = event {
                let app_handle = window.app_handle().clone();
                let state = window.state::<SharedBackendState>().inner().clone();

                tauri::async_runtime::spawn(async move {
                    let _ = shutdown_active_backend(
                        &app_handle,
                        state,
                        "window-close-requested",
                        false,
                        "",
                    )
                    .await;
                });
            }
        })
        .invoke_handler(tauri::generate_handler![get_backend_url])
        .build(tauri::generate_context!())
        .expect("error while building KST application");

    app.run(|app_handle, event| match event {
        tauri::RunEvent::ExitRequested { .. } => {
            let app_handle = app_handle.clone();
            let state = app_handle.state::<SharedBackendState>().inner().clone();

            tauri::async_runtime::spawn(async move {
                let _ = shutdown_active_backend(
                    &app_handle,
                    state,
                    "runtime-exit-requested",
                    false,
                    "",
                )
                .await;
            });
        }
        tauri::RunEvent::Exit => {
            let app_handle = app_handle.clone();
            let state = app_handle.state::<SharedBackendState>().inner().clone();

            tauri::async_runtime::spawn(async move {
                let _ = shutdown_active_backend(&app_handle, state, "runtime-exit", false, "")
                    .await;
            });
        }
        _ => {}
    });
}

/// Finds the backend executable, starts it, reads the startup handshake,
/// polls /ready, then emits the backend-ready event to the frontend.
async fn launch_backend(app: AppHandle, state: SharedBackendState) {
    {
        let mut runtime = state.lock().await;
        if runtime.active.is_some() || runtime.launch_in_progress {
            warn!("KST Tauri: backend launch requested while active or starting; skipping");
            return;
        }

        runtime.launch_in_progress = true;
    }

    info!("KST Tauri: launching backend sidecar");

    // Resolve the backend sidecar path.
    // In development: looks for Kst.Api.exe next to the Tauri binary.
    // In production: packaged as a Tauri external binary via tauri.conf.json.
    let sidecar_result = app.shell().sidecar(BACKEND_SIDECAR_NAME);
    let sidecar = match sidecar_result {
        Ok(s) => s,
        Err(e) => {
            error!("KST Tauri: could not resolve backend sidecar: {e}");
            clear_launch_flag(state).await;
            emit_backend_unavailable(
                &app,
                "Backend sidecar could not be resolved.",
                None,
                false,
                None,
                None,
            );
            return;
        }
    };

    // ASP.NET Core resolves appsettings*.json relative to its content root, which defaults to
    // the process's current working directory - not the directory the sidecar exe actually runs
    // from (Tauri stages/copies the sidecar into its own build output, e.g. target/debug in dev).
    // Point it explicitly at the stable "binaries" location where these config files are known to
    // live (copied there by scripts/build-sidecar.ps1 for dev, bundled as resources for packaged
    // builds), independent of wherever Tauri actually executes the sidecar binary from.
    let content_root = if cfg!(debug_assertions) {
        Some(std::path::PathBuf::from(env!("CARGO_MANIFEST_DIR")).join("binaries"))
    } else {
        app.path().resource_dir().ok().map(|dir| dir.join("binaries"))
    };

    let sidecar = match content_root {
        Some(root) => sidecar.env("ASPNETCORE_CONTENTROOT", root.to_string_lossy().to_string()),
        None => {
            warn!("KST Tauri: could not resolve sidecar content root; falling back to default");
            sidecar
        }
    };

    let (mut rx, child) = match sidecar.spawn() {
        Ok(pair) => pair,
        Err(e) => {
            error!("KST Tauri: failed to spawn backend: {e}");
            clear_launch_flag(state).await;
            emit_backend_unavailable(
                &app,
                "Backend sidecar failed to start.",
                None,
                false,
                None,
                None,
            );
            return;
        }
    };

    let pid = child.pid();

    {
        let mut runtime = state.lock().await;
        runtime.active = Some(ActiveBackendProcess {
            child,
            pid,
            port: None,
            instance_id: None,
            base_url: None,
            ready: false,
            expected_shutdown: false,
        });
        runtime.launch_in_progress = false;
    }

    info!("KST Tauri: backend process spawned (pid={pid})");

    // Read stdout lines until we see the JSON handshake.
    let mut handshake: Option<BackendHandshake> = None;
    let start = std::time::Instant::now();

    loop {
        if start.elapsed() > HANDSHAKE_TIMEOUT {
            error!("KST Tauri: timed out waiting for backend handshake after 30s");
            break;
        }

        match tokio::time::timeout(Duration::from_millis(500), rx.recv()).await {
            Ok(Some(CommandEvent::Stdout(line_bytes))) => {
                let line = String::from_utf8_lossy(&line_bytes);
                let line = line.trim();
                info!("KST Tauri [backend stdout]: {line}");

                if let Ok(hs) = serde_json::from_str::<BackendHandshake>(line) {
                    info!("KST Tauri: backend handshake received — port={} instanceId={}", hs.port, hs.instance_id);
                    handshake = Some(hs);
                    break;
                }
            }
            Ok(Some(CommandEvent::Stderr(line_bytes))) => {
                let line = String::from_utf8_lossy(&line_bytes);
                info!("KST Tauri [backend stderr]: {}", line.trim());
            }
            Ok(Some(CommandEvent::Error(msg))) => {
                error!("KST Tauri: backend process error: {msg}");
                break;
            }
            Ok(Some(CommandEvent::Terminated(status))) => {
                error!(
                    "KST Tauri: backend terminated before handshake: pid={pid} status={:?}",
                    status
                );
                handle_backend_terminated(
                    &app,
                    state.clone(),
                    pid,
                    status,
                    "Backend terminated before startup handshake.",
                )
                .await;
                break;
            }
            Ok(Some(_)) => {} // other events
            Ok(None) => {
                warn!("KST Tauri: backend stdout channel closed before handshake");
                break;
            }
            Err(_) => {} // timeout on this iteration — continue
        }
    }

    let hs = match handshake {
        Some(h) => h,
        None => {
            error!("KST Tauri: no backend handshake received; terminating spawned backend");
            let _ = shutdown_active_backend(
                &app,
                state,
                "handshake-timeout-or-failure",
                true,
                "Backend failed during startup handshake.",
            )
            .await;
            return;
        }
    };

    let base_url = format!("http://127.0.0.1:{}", hs.port);

    // Poll /ready with a timeout.
    info!("KST Tauri: polling {base_url}/ready");
    let ready = poll_ready(&base_url, READINESS_ATTEMPTS, READINESS_INTERVAL).await;

    if !ready {
        warn!("KST Tauri: backend did not reach /ready within timeout; terminating backend");
        let _ = shutdown_active_backend(
            &app,
            state,
            "readiness-timeout",
            true,
            "Backend readiness timed out.",
        )
        .await;
        return;
    }

    // Store ready-state only after successful readiness.
    {
        let mut runtime = state.lock().await;
        let Some(active) = runtime.active.as_mut() else {
            warn!(
                "KST Tauri: backend state missing during readiness transition (pid={pid})"
            );
            return;
        };

        if active.pid != pid {
            warn!(
                "KST Tauri: backend pid changed during readiness transition (expected {pid}, found {})",
                active.pid
            );
            return;
        }

        active.port = Some(hs.port);
        active.instance_id = Some(hs.instance_id.clone());
        active.base_url = Some(base_url.clone());
        active.ready = true;
    }

    // Inject the backend URL into the webview so the frontend can call it.
    if let Some(window) = app.get_webview_window("main") {
        let js = format!(
            "window.__KST_BACKEND_URL__ = '{}';",
            base_url.replace('\'', "\\'")
        );
        if let Err(e) = window.eval(&js) {
            warn!("KST Tauri: could not inject backend URL into webview: {e}");
        }
    }

    // Emit event so the frontend can react immediately.
    let _ = app.emit("backend-ready", BackendReadyEvent {
        base_url,
        port: hs.port,
        instance_id: hs.instance_id,
    });

    info!("KST Tauri: backend is ready");

    // Forward remaining stdout/stderr to the log.
    while let Some(event) = rx.recv().await {
        match event {
            CommandEvent::Stdout(bytes) => {
                info!("KST Tauri [backend]: {}", String::from_utf8_lossy(&bytes).trim());
            }
            CommandEvent::Stderr(bytes) => {
                info!("KST Tauri [backend err]: {}", String::from_utf8_lossy(&bytes).trim());
            }
            CommandEvent::Terminated(status) => {
                handle_backend_terminated(
                    &app,
                    state.clone(),
                    pid,
                    status,
                    "Backend process terminated.",
                )
                .await;
                break;
            }
            _ => {}
        }
    }
}

async fn clear_launch_flag(state: SharedBackendState) {
    let mut runtime = state.lock().await;
    runtime.launch_in_progress = false;
}

async fn shutdown_active_backend(
    app: &AppHandle,
    state: SharedBackendState,
    reason: &str,
    notify_frontend: bool,
    frontend_reason: &str,
) -> bool {
    let active = {
        let mut runtime = state.lock().await;

        if runtime.shutdown_in_progress {
            info!("KST Tauri: shutdown already in progress; ignoring duplicate request ({reason})");
            return false;
        }

        let Some(mut active) = runtime.active.take() else {
            runtime.launch_in_progress = false;
            return false;
        };

        active.expected_shutdown = true;
        runtime.shutdown_in_progress = true;
        runtime.launch_in_progress = false;
        active
    };

    let pid = active.pid;
    info!("KST Tauri: shutdown requested ({reason}); backend pid={pid}");

    let graceful_request = match active.child.kill() {
        Ok(()) => {
            info!("KST Tauri: graceful termination signal sent to backend pid={pid}");
            true
        }
        Err(e) => {
            warn!("KST Tauri: failed to request graceful backend termination for pid={pid}: {e}");
            false
        }
    };

    let exited_within_timeout = wait_for_process_exit(pid, SHUTDOWN_TIMEOUT).await;
    if exited_within_timeout {
        info!("KST Tauri: backend pid={pid} exited within shutdown timeout");
    } else {
        warn!(
            "KST Tauri: backend pid={pid} did not exit within {:?}; forcing termination",
            SHUTDOWN_TIMEOUT
        );

        let forced = force_kill_process(pid).await;
        if forced {
            info!("KST Tauri: forced termination succeeded for backend pid={pid}");
        } else {
            error!("KST Tauri: forced termination failed for backend pid={pid}");
        }
    }

    {
        let mut runtime = state.lock().await;
        runtime.shutdown_in_progress = false;
        runtime.launch_in_progress = false;
    }

    if notify_frontend {
        let reason_to_emit = if frontend_reason.is_empty() {
            "Backend is unavailable."
        } else {
            frontend_reason
        };

        emit_backend_unavailable(
            app,
            reason_to_emit,
            Some(pid),
            true,
            None,
            None,
        );
    }

    info!(
        "KST Tauri: shutdown complete for backend pid={pid}; gracefulRequest={graceful_request} exitedWithinTimeout={exited_within_timeout}"
    );

    true
}

async fn handle_backend_terminated(
    app: &AppHandle,
    state: SharedBackendState,
    pid: u32,
    status: TerminatedPayload,
    frontend_reason: &str,
) {
    let expected = {
        let mut runtime = state.lock().await;
        let Some(active) = runtime.active.take() else {
            return;
        };

        if active.pid != pid {
            runtime.active = Some(active);
            return;
        }

        runtime.shutdown_in_progress = false;
        runtime.launch_in_progress = false;
        active.expected_shutdown
    };

    info!(
        "KST Tauri: backend termination observed; pid={pid} expected={expected} exitCode={:?} signal={:?}",
        status.code,
        status.signal
    );

    if !expected {
        emit_backend_unavailable(
            app,
            frontend_reason,
            Some(pid),
            false,
            status.code,
            status.signal,
        );

        let _ = app.emit(
            "backend-terminated",
            BackendUnavailableEvent {
                reason: frontend_reason.to_string(),
                pid: Some(pid),
                expected: false,
                code: status.code,
                signal: status.signal,
            },
        );
    }
}

fn emit_backend_unavailable(
    app: &AppHandle,
    reason: &str,
    pid: Option<u32>,
    expected: bool,
    code: Option<i32>,
    signal: Option<i32>,
) {
    let _ = app.emit(
        "backend-unavailable",
        BackendUnavailableEvent {
            reason: reason.to_string(),
            pid,
            expected,
            code,
            signal,
        },
    );
}

async fn wait_for_process_exit(pid: u32, timeout: Duration) -> bool {
    let start = std::time::Instant::now();
    while start.elapsed() < timeout {
        if !is_process_running(pid).await {
            return true;
        }

        tokio::time::sleep(Duration::from_millis(200)).await;
    }

    !is_process_running(pid).await
}

async fn is_process_running(pid: u32) -> bool {
    #[cfg(target_os = "windows")]
    {
        let status = tokio::process::Command::new("powershell")
            .arg("-NoProfile")
            .arg("-Command")
            .arg(format!(
                "if (Get-Process -Id {pid} -ErrorAction SilentlyContinue) {{ exit 0 }} else {{ exit 1 }}"
            ))
            .status()
            .await;

        return status.map(|s| s.success()).unwrap_or(false);
    }

    #[cfg(not(target_os = "windows"))]
    {
        let status = tokio::process::Command::new("kill")
            .arg("-0")
            .arg(pid.to_string())
            .status()
            .await;

        status.map(|s| s.success()).unwrap_or(false)
    }
}

async fn force_kill_process(pid: u32) -> bool {
    #[cfg(target_os = "windows")]
    {
        let status = tokio::process::Command::new("taskkill")
            .arg("/PID")
            .arg(pid.to_string())
            .arg("/T")
            .arg("/F")
            .status()
            .await;

        return status.map(|s| s.success()).unwrap_or(false);
    }

    #[cfg(not(target_os = "windows"))]
    {
        let status = tokio::process::Command::new("kill")
            .arg("-9")
            .arg(pid.to_string())
            .status()
            .await;

        status.map(|s| s.success()).unwrap_or(false)
    }
}

#[cfg(test)]
mod capability_guard {
    //! Regression guard for `S0.2-F001` / S0.4B (see
    //! `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md`).
    //!
    //! Tauri 2 capabilities restrict webview (frontend) IPC only. KST's accepted
    //! process lifecycle — the `Kst.Api` sidecar launch and the PID-scoped
    //! powershell/taskkill shutdown handling — runs from this Rust host and is not
    //! gated by the capability file. The webview invokes no Tauri shell-plugin
    //! command, so the checked-in capability must grant no `shell:*` permission and
    //! nothing beyond the core default set. The sidecar boundary itself is protected
    //! by the `bundle.externalBin` declaration plus the constant the launch path
    //! uses.

    use super::BACKEND_SIDECAR_NAME;
    use serde_json::Value;

    fn read_manifest_file(name: &str) -> Value {
        let path = std::path::Path::new(env!("CARGO_MANIFEST_DIR")).join(name);
        let raw = std::fs::read_to_string(&path)
            .unwrap_or_else(|e| panic!("failed to read {}: {e}", path.display()));
        serde_json::from_str(&raw)
            .unwrap_or_else(|e| panic!("failed to parse {} as JSON: {e}", path.display()))
    }

    /// The effective capability must grant exactly `core:default` to the `main`
    /// window. This fails if `shell:allow-execute`, `shell:allow-open` (or any
    /// `shell:*` permission, including scoped/object forms) is reintroduced, if a
    /// new permission or scope entry is added, or if the window targeting drifts.
    #[test]
    fn default_capability_grants_core_default_only() {
        let capability = read_manifest_file("capabilities/default.json");

        let permissions = capability
            .get("permissions")
            .and_then(Value::as_array)
            .expect("capability 'permissions' must be a JSON array");

        let granted: Vec<String> = permissions
            .iter()
            .map(|entry| match entry {
                Value::String(identifier) => identifier.clone(),
                other => panic!(
                    "capability entries must be plain permission identifiers; found scoped/object entry: {other}"
                ),
            })
            .collect();

        let shell_grants: Vec<&String> = granted
            .iter()
            .filter(|id| id.starts_with("shell:"))
            .collect();
        assert!(
            shell_grants.is_empty(),
            "capability grants webview shell IPC authority KST does not use: {shell_grants:?}"
        );

        assert_eq!(
            granted,
            vec!["core:default".to_string()],
            "capability permission set drifted from the S0.4B accepted surface (core:default only); \
             review against docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md before changing"
        );

        let windows = capability.get("windows").and_then(Value::as_array);
        assert_eq!(
            windows.and_then(|w| w.first()),
            Some(&Value::String("main".into())),
            "capability must remain scoped to the main window"
        );
        assert_eq!(
            windows.map(|w| w.len()),
            Some(1),
            "capability must remain scoped to exactly one window"
        );
    }

    /// The executable boundary must stay exactly the declared `Kst.Api` sidecar:
    /// `bundle.externalBin` lists only `binaries/Kst.Api`, and the name used by the
    /// Rust launch path matches it.
    #[test]
    fn sidecar_boundary_is_exactly_kst_api() {
        let conf = read_manifest_file("tauri.conf.json");

        let external_bin = conf
            .pointer("/bundle/externalBin")
            .and_then(Value::as_array)
            .expect("tauri.conf.json bundle.externalBin must be a JSON array");

        let entries: Vec<&str> = external_bin
            .iter()
            .map(|v| v.as_str().expect("externalBin entry must be a string"))
            .collect();

        assert_eq!(
            entries,
            vec!["binaries/Kst.Api"],
            "bundle.externalBin must declare exactly the Kst.Api sidecar"
        );

        let declared = entries[0].rsplit('/').next().unwrap();
        assert_eq!(
            declared, BACKEND_SIDECAR_NAME,
            "runtime sidecar name (BACKEND_SIDECAR_NAME) must match bundle.externalBin"
        );
    }
}

#[cfg(test)]
mod csp_guard {
    //! Regression guard for `S0.3-G003` / S0.5 (see
    //! `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md`).
    //!
    //! The accepted desktop/webview network boundary (S0.2 baseline §10): the webview's
    //! outbound connections (`connect-src`) are restricted to loopback
    //! (`http://127.0.0.1:*`) plus `'self'` — the Kst.Api backend on loopback and the page's
    //! own origin. No remote API origin may be reachable from the webview.
    //!
    //! These tests parse `tauri.conf.json`'s `app.security.csp` structurally (directive →
    //! sources) and assert the *semantic* properties, not the exact CSP string, so harmless
    //! reordering or unrelated directive changes do not break the security test. A passing
    //! check proves the asserted CSP configuration only — not general webview exploit
    //! resistance.

    use serde_json::Value;

    fn read_csp() -> String {
        let path = std::path::Path::new(env!("CARGO_MANIFEST_DIR")).join("tauri.conf.json");
        let raw = std::fs::read_to_string(&path)
            .unwrap_or_else(|e| panic!("failed to read {}: {e}", path.display()));
        let conf: Value = serde_json::from_str(&raw)
            .unwrap_or_else(|e| panic!("failed to parse {} as JSON: {e}", path.display()));
        conf.pointer("/app/security/csp")
            .and_then(Value::as_str)
            .expect("tauri.conf.json app.security.csp must be a CSP string")
            .to_string()
    }

    /// Parse a CSP policy into ordered (directive, sources) pairs.
    fn parse_csp(csp: &str) -> Vec<(&str, Vec<String>)> {
        csp.split(';')
            .filter_map(|part| {
                let part = part.trim();
                if part.is_empty() {
                    return None;
                }
                let mut tokens = part.split_whitespace();
                let directive = tokens.next()?;
                Some((directive, tokens.map(str::to_string).collect()))
            })
            .collect()
    }

    fn sources<'a>(directives: &'a [(&str, Vec<String>)], name: &str) -> Option<&'a [String]> {
        directives
            .iter()
            .find(|(directive, _)| *directive == name)
            .map(|(_, sources)| sources.as_slice())
    }

    /// An accepted `connect-src` source is either `'self'` or an http(s) origin whose host is
    /// exactly `127.0.0.1` (any port, including a port wildcard). Anything else — a bare `*`,
    /// `localhost`, a LAN address, or any remote host — is a broadening of the accepted
    /// webview→backend boundary.
    fn is_accepted_connect_source(source: &str) -> bool {
        if source == "'self'" {
            return true;
        }

        let (scheme, rest) = match source.split_once("://") {
            Some((scheme, rest)) => (scheme, rest),
            None => return false,
        };
        if scheme != "http" && scheme != "https" {
            return false;
        }

        // rest is `host[:port]`; the host token must be exactly 127.0.0.1. (The accepted
        // architecture uses IPv4 loopback; the backend binds 127.0.0.1, so no IPv6 form is
        // required or accepted here.)
        let host = rest.split(':').next().unwrap_or("");
        host == "127.0.0.1"
    }

    #[test]
    fn csp_connect_src_is_restricted_to_loopback_and_self() {
        let csp = read_csp();
        let directives = parse_csp(&csp);
        let connect = sources(&directives, "connect-src").expect(
            "CSP must define an explicit connect-src — the webview→backend network boundary",
        );

        assert!(
            !connect.is_empty(),
            "connect-src must not be empty (the webview must be able to reach the loopback backend)"
        );

        for source in connect {
            assert_ne!(
                source, "*",
                "SECURITY REGRESSION (S0.3-G003): connect-src contains the '*' wildcard — the webview could \
                 connect to arbitrary remote hosts"
            );
            assert!(
                is_accepted_connect_source(source),
                "SECURITY REGRESSION (S0.3-G003): connect-src source {source:?} is not an accepted \
                 local/loopback destination — the webview may only connect to 127.0.0.1 (any port) or \
                 'self'. Review against docs/security/SECURITY_BASELINE.md §10 before any intentional change"
            );
        }
    }

    #[test]
    fn csp_default_src_remains_self_only() {
        let csp = read_csp();
        let directives = parse_csp(&csp);
        let default_sources = sources(&directives, "default-src").expect(
            "CSP must define default-src — the baseline source policy for every fetch kind",
        );

        assert_eq!(
            default_sources, ["'self'"],
            "SECURITY REGRESSION (S0.3-G003): default-src drifted from exactly 'self' — the baseline \
             source policy would allow non-self resources by default"
        );
    }

    #[test]
    fn csp_effective_script_sources_have_no_unsafe_or_remote_sources() {
        let csp = read_csp();
        let directives = parse_csp(&csp);
        // Without an explicit script-src, the effective script policy is default-src.
        let script_sources = sources(&directives, "script-src").unwrap_or_else(|| {
            sources(&directives, "default-src").expect("CSP must define default-src or script-src")
        });

        for source in script_sources {
            assert_ne!(
                source, "'unsafe-inline'",
                "SECURITY REGRESSION (S0.3-G003): the effective script policy includes 'unsafe-inline'"
            );
            assert_ne!(
                source, "'unsafe-eval'",
                "SECURITY REGRESSION (S0.3-G003): the effective script policy includes 'unsafe-eval'"
            );
            assert_ne!(
                source, "*",
                "SECURITY REGRESSION (S0.3-G003): the effective script policy includes the '*' wildcard"
            );
            // Quoted CSP keywords/hashes ('self', 'nonce-…', 'sha256-…') are allowed; any
            // unquoted origin (scheme://…) would be a remote script host.
            if !source.starts_with('\'') {
                assert!(
                    !source.contains("://"),
                    "SECURITY REGRESSION (S0.3-G003): the effective script policy includes the remote \
                     source {source:?} — scripts must remain same-origin"
                );
            }
        }
    }
}

/// Poll `{base_url}/ready` up to `attempts` times with `interval` between each.
async fn poll_ready(base_url: &str, attempts: u32, interval: Duration) -> bool {
    let url = format!("{base_url}/ready");
    let client = reqwest::Client::builder()
        .timeout(Duration::from_secs(5))
        .build()
        .unwrap_or_default();

    for attempt in 1..=attempts {
        match client.get(&url).send().await {
            Ok(resp) if resp.status().is_success() => {
                info!("KST Tauri: /ready responded OK (attempt {attempt})");
                return true;
            }
            Ok(resp) => {
                warn!("KST Tauri: /ready returned {} (attempt {attempt})", resp.status());
            }
            Err(e) => {
                info!("KST Tauri: /ready not yet available (attempt {attempt}): {e}");
            }
        }
        tokio::time::sleep(interval).await;
    }
    false
}
