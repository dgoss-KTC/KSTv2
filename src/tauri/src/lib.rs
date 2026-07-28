use std::sync::{Arc, Mutex};
use std::time::Duration;
use tauri::{AppHandle, Manager, Emitter};
use tauri_plugin_shell::ShellExt;
use serde::{Deserialize, Serialize};
use log::{info, warn, error};

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
pub struct BackendState {
    /// The port the backend is listening on (None until discovered).
    pub port: Option<u16>,
    /// The instance ID returned in the startup handshake.
    pub instance_id: Option<String>,
    /// The base URL string (e.g., "http://127.0.0.1:12345")
    pub base_url: Option<String>,
}

/// Emitted to the frontend when the backend URL is known.
#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct BackendReadyEvent {
    base_url: String,
    port: u16,
    instance_id: String,
}

/// Tauri command: returns the current backend base URL.
#[tauri::command]
fn get_backend_url(state: tauri::State<Arc<Mutex<BackendState>>>) -> Option<String> {
    state.lock().ok()?.base_url.clone()
}

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .manage(Arc::new(Mutex::new(BackendState::default())))
        .setup(|app| {
            let app_handle = app.handle().clone();
            let state = app.state::<Arc<Mutex<BackendState>>>().inner().clone();

            // Launch the backend sidecar in a background task.
            tauri::async_runtime::spawn(async move {
                launch_backend(app_handle, state).await;
            });

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![get_backend_url])
        .run(tauri::generate_context!())
        .expect("error while running KST application");
}

/// Finds the backend executable, starts it, reads the startup handshake,
/// polls /ready, then emits the backend-ready event to the frontend.
async fn launch_backend(app: AppHandle, state: Arc<Mutex<BackendState>>) {
    info!("KST Tauri: launching backend sidecar");

    // Resolve the backend sidecar path.
    // In development: looks for Kst.Api.exe next to the Tauri binary.
    // In production: packaged as a Tauri external binary via tauri.conf.json.
    let sidecar_result = app.shell().sidecar("Kst.Api");
    let sidecar = match sidecar_result {
        Ok(s) => s,
        Err(e) => {
            error!("KST Tauri: could not resolve backend sidecar: {e}");
            return;
        }
    };

    let (mut rx, _child) = match sidecar.spawn() {
        Ok(pair) => pair,
        Err(e) => {
            error!("KST Tauri: failed to spawn backend: {e}");
            return;
        }
    };

    info!("KST Tauri: backend process spawned");

    // Read stdout lines until we see the JSON handshake.
    let mut handshake: Option<BackendHandshake> = None;
    let timeout = Duration::from_secs(30);
    let start = std::time::Instant::now();

    use tauri_plugin_shell::process::CommandEvent;

    loop {
        if start.elapsed() > timeout {
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
                error!("KST Tauri: backend terminated unexpectedly: {:?}", status);
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
            error!("KST Tauri: no backend handshake received; frontend will remain in error state");
            return;
        }
    };

    let base_url = format!("http://127.0.0.1:{}", hs.port);

    // Poll /ready with a timeout.
    info!("KST Tauri: polling {base_url}/ready");
    let ready = poll_ready(&base_url, 30, Duration::from_secs(1)).await;

    if !ready {
        warn!("KST Tauri: backend did not reach /ready within timeout; emitting anyway");
    }

    // Store state.
    {
        let mut s = state.lock().expect("backend state poisoned");
        s.port = Some(hs.port);
        s.instance_id = Some(hs.instance_id.clone());
        s.base_url = Some(base_url.clone());
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
                info!("KST Tauri: backend exited with {:?}", status);
                break;
            }
            _ => {}
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
