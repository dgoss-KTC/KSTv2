use crate::component_mrp_native as native;
use std::path::PathBuf;

fn root() -> PathBuf {
    let root =
        PathBuf::from(std::env::var("KST_MRP_TEST_ROOT").expect("Set bounded synthetic root"))
            .join(native::new_id().unwrap());
    std::fs::create_dir_all(&root).unwrap();
    root
}
#[test]
fn production_publication_collision_cleanup_retry_and_artifact_evidence() {
    let root = root();
    let b = b"KST synthetic immutable workbook bytes B";
    let a = b"existing sentinel A";
    let destination = root.join("published.xlsx");
    let id = native::publish(&destination, b, false).unwrap();
    assert_eq!(std::fs::read(&destination).unwrap(), b);
    native::verify_artifact(&destination, id, b).unwrap();
    assert!(native::publish(&destination, a, false).is_err());
    assert_eq!(std::fs::read(&destination).unwrap(), b);
    let collision = root.join("sentinel.xlsx");
    std::fs::write(&collision, a).unwrap();
    assert!(native::publish(&collision, b, false).is_err());
    assert_eq!(std::fs::read(&collision).unwrap(), a);
    let retry = root.join("retry.xlsx");
    assert!(native::publish(&retry, b, true).is_err());
    assert!(!retry.exists());
    assert!(!std::fs::read_dir(&root).unwrap().any(|f| f
        .unwrap()
        .file_name()
        .to_string_lossy()
        .starts_with(".kst-mrp-")));
    let retry_id = native::publish(&retry, b, false).unwrap();
    native::verify_artifact(&retry, retry_id, b).unwrap();
    std::fs::write(&retry, a).unwrap();
    assert!(native::verify_artifact(&retry, retry_id, b).is_err());
    std::fs::remove_file(&retry).unwrap();
    assert!(native::verify_artifact(&retry, retry_id, b).is_err());
    assert_eq!(std::fs::read(&destination).unwrap(), b);
    println!("native production exact-byte publication, existing A/B collision refusal, owned cleanup, retry and changed/missing-artifact rejection passed; id={id:?}");
}

#[test]
fn file_backed_large_publication_and_verification_are_complete_and_bounded() {
    use std::io::Write;
    let root = root();
    let source = root.join("source.bin");
    let mut file = std::fs::OpenOptions::new()
        .read(true)
        .write(true)
        .create_new(true)
        .open(&source)
        .unwrap();
    let block = [0x5Au8; 65536];
    for _ in 0..145 {
        file.write_all(&block).unwrap();
    }
    let target = root.join("large.xlsx");
    let id = native::publish_stream_selected(&target, &mut file, false).unwrap();
    assert_eq!(std::fs::metadata(&target).unwrap().len(), 145 * 65536);
    native::verify_artifact_stream(&target, id, &mut file).unwrap();
    assert!(native::publish_stream_selected(&target, &mut file, false).is_err());
    let mut changed = std::fs::OpenOptions::new()
        .append(true)
        .open(&target)
        .unwrap();
    changed.write_all(b"changed").unwrap();
    drop(changed);
    assert!(native::verify_artifact_stream(&target, id, &mut file).is_err());
    native::publish_stream_selected(&target, &mut file, true).unwrap();
    assert_eq!(std::fs::metadata(&target).unwrap().len(), 145 * 65536);
}

#[test]
fn production_confirmed_replace_failure_preservation_and_cleanup() {
    use std::fs::OpenOptions;
    use std::os::windows::fs::OpenOptionsExt;
    let root = root();
    let target = root.join("replace.xlsx");
    let a = b"existing workbook A";
    let b = b"replacement workbook B";
    std::fs::write(&target, a).unwrap();
    assert!(native::publish_selected(&target, b, false, false).is_err());
    assert_eq!(std::fs::read(&target).unwrap(), a);
    assert!(native::publish_selected(&target, b, true, true).is_err());
    assert_eq!(std::fs::read(&target).unwrap(), a);
    let lock = OpenOptions::new()
        .read(true)
        .share_mode(1)
        .open(&target)
        .unwrap();
    assert!(native::publish_selected(&target, b, true, false).is_err());
    drop(lock);
    assert_eq!(std::fs::read(&target).unwrap(), a);
    let mut permissions = std::fs::metadata(&target).unwrap().permissions();
    permissions.set_readonly(true);
    std::fs::set_permissions(&target, permissions).unwrap();
    assert!(native::publish_selected(&target, b, true, false).is_err());
    assert_eq!(std::fs::read(&target).unwrap(), a);
    let mut permissions = std::fs::metadata(&target).unwrap().permissions();
    permissions.set_readonly(false);
    std::fs::set_permissions(&target, permissions).unwrap();
    let id = native::publish_selected(&target, b, true, false).unwrap();
    native::verify_artifact(&target, id, b).unwrap();
    assert_eq!(std::fs::read(&target).unwrap(), b);
    assert!(!std::fs::read_dir(root).unwrap().any(|p| p
        .unwrap()
        .file_name()
        .to_string_lossy()
        .starts_with(".kst-mrp-")));
    println!("Production approved replacement passed; no-consent, injected precommit, locked and read-only refusals preserved A; owned cleanup and committed B survived.");
}
#[test]
#[ignore = "One authorized synthetic workbook default-association dispatch only"]
fn synthetic_association_dispatch() {
    let root = root();
    let source =
        PathBuf::from(std::env::var("KST_MRP_TEST_ROOT").unwrap()).join("synthetic-source.xlsx");
    let bytes = std::fs::read(source).expect("Synthetic XLSX from scoped backend test");
    let destination = root.join("association-check.xlsx");
    let id = native::publish(&destination, &bytes, false).unwrap();
    native::verify_artifact(&destination, id, &bytes).unwrap();
    native::dispatch(&destination).unwrap();
    println!("ShellExecuteExW accepted one synthetic saved workbook dispatch; this is NOT viewer/load confirmation.");
}
