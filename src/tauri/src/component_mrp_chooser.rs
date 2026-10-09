//! Windows Save As with attempt-local candidate evidence. No filesystem publication here.
use std::{
    ffi::c_void,
    path::{Path, PathBuf},
    sync::{
        atomic::{AtomicU32, Ordering},
        Mutex,
    },
};

type Hr = i32;
#[repr(C)]
#[derive(Clone, Copy, PartialEq, Eq)]
struct Guid(u32, u16, u16, [u8; 8]);
const SAVE: Guid = Guid(
    0xc0b4e2f3,
    0xba21,
    0x4773,
    [0x8d, 0xba, 0x33, 0x5e, 0xc9, 0x46, 0xeb, 0x8b],
);
const FILE_DIALOG: Guid = Guid(
    0x42f85136,
    0xdb7e,
    0x439c,
    [0x85, 0xf1, 0xe4, 0x07, 0x5d, 0x13, 0x5f, 0xc8],
);
const SHELL_ITEM: Guid = Guid(
    0x43826d1e,
    0xe718,
    0x42ee,
    [0xbc, 0x55, 0xa1, 0xe2, 0x61, 0xc3, 0x7b, 0xfe],
);
const UNKNOWN: Guid = Guid(0, 0, 0, [0xc0, 0, 0, 0, 0, 0, 0, 0x46]);
const EVENTS: Guid = Guid(
    0x973510db,
    0x7d7f,
    0x452b,
    [0x89, 0x75, 0x74, 0xa8, 0x58, 0x28, 0xd3, 0x54],
);
#[link(name = "ole32")]
extern "system" {
    fn CoInitializeEx(reserved: *mut c_void, flags: u32) -> Hr;
    fn CoUninitialize();
    fn CoCreateInstance(
        class: *const Guid,
        outer: *mut c_void,
        context: u32,
        iid: *const Guid,
        result: *mut *mut c_void,
    ) -> Hr;
    fn CoTaskMemFree(pointer: *mut c_void);
}
#[link(name = "shell32")]
extern "system" {
    fn SHCreateItemFromParsingName(
        path: *const u16,
        context: *mut c_void,
        iid: *const Guid,
        result: *mut *mut c_void,
    ) -> Hr;
}
#[repr(C)]
struct UnknownV {
    query: usize,
    add: usize,
    release: unsafe extern "system" fn(*mut c_void) -> u32,
}
#[repr(C)]
struct Filter {
    name: *const u16,
    spec: *const u16,
}
#[repr(C)]
struct DialogV {
    base: UnknownV,
    show: unsafe extern "system" fn(*mut c_void, *mut c_void) -> Hr,
    types: unsafe extern "system" fn(*mut c_void, u32, *const Filter) -> Hr,
    set_index: usize,
    get_index: usize,
    advise: unsafe extern "system" fn(*mut c_void, *mut c_void, *mut u32) -> Hr,
    unadvise: unsafe extern "system" fn(*mut c_void, u32) -> Hr,
    options: unsafe extern "system" fn(*mut c_void, u32) -> Hr,
    get_options: unsafe extern "system" fn(*mut c_void, *mut u32) -> Hr,
    default_folder: usize,
    folder: unsafe extern "system" fn(*mut c_void, *mut c_void) -> Hr,
    get_folder: usize,
    selection: usize,
    filename: unsafe extern "system" fn(*mut c_void, *const u16) -> Hr,
    get_filename: usize,
    title: usize,
    ok_label: usize,
    file_label: usize,
    result: unsafe extern "system" fn(*mut c_void, *mut *mut c_void) -> Hr,
    place: usize,
    extension: unsafe extern "system" fn(*mut c_void, *const u16) -> Hr,
}
#[repr(C)]
struct ItemV {
    base: UnknownV,
    bind: usize,
    parent: usize,
    name: unsafe extern "system" fn(*mut c_void, u32, *mut *mut u16) -> Hr,
}
struct Com(*mut c_void);
impl Drop for Com {
    fn drop(&mut self) {
        unsafe {
            let v = *(self.0 as *const *const UnknownV);
            ((*v).release)(self.0);
        }
    }
}
struct Apartment;
impl Drop for Apartment {
    fn drop(&mut self) {
        unsafe {
            CoUninitialize();
        }
    }
}
fn check(hr: Hr) -> Result<(), String> {
    if hr < 0 {
        Err(format!("Windows chooser failed: 0x{:08X}", hr as u32))
    } else {
        Ok(())
    }
}
fn wide(s: &std::ffi::OsStr) -> Vec<u16> {
    use std::os::windows::ffi::OsStrExt;
    s.encode_wide().chain(Some(0)).collect()
}
unsafe fn item_path(item: *mut c_void) -> Result<PathBuf, String> {
    let v = *(item as *const *const ItemV);
    let mut p = std::ptr::null_mut();
    check(((*v).name)(item, 0x80058000, &mut p))?;
    let mut n = 0;
    while *p.add(n) != 0 {
        n += 1;
    }
    use std::os::windows::ffi::OsStringExt;
    let result = std::ffi::OsString::from_wide(std::slice::from_raw_parts(p, n));
    CoTaskMemFree(p.cast());
    Ok(result.into())
}
unsafe fn result_path(dialog: *mut c_void) -> Result<PathBuf, String> {
    let v = *(dialog as *const *const DialogV);
    let mut p = std::ptr::null_mut();
    check(((*v).result)(dialog, &mut p))?;
    let p = Com(p);
    item_path(p.0)
}

#[derive(Default, Debug)]
struct Candidate {
    overwrite: Option<PathBuf>,
    overwrite_identity: Option<(u32, u64)>,
    accepted: Option<(PathBuf, bool)>,
    trace: Vec<String>,
}
impl Candidate {
    fn reset(&mut self, event: &str) {
        self.overwrite = None;
        self.overwrite_identity = None;
        self.accepted = None;
        if self.trace.len() < 256 {
            self.trace.push(event.into());
        }
    }
    fn overwrite(&mut self, path: PathBuf) {
        self.reset("overwrite");
        self.overwrite = Some(path);
        self.overwrite_identity = self.overwrite.as_deref().and_then(file_identity);
    }
    fn accept(&mut self, path: PathBuf) {
        let replace = self.overwrite.as_ref() == Some(&path)
            && self.overwrite_identity.is_some()
            && self.overwrite_identity == file_identity(&path);
        if self.trace.len() < 256 {
            self.trace.push(format!("file-ok replace={replace}"));
        }
        self.accepted = Some((path, replace));
        self.overwrite = None;
        self.overwrite_identity = None;
    }
    fn finish(&self, path: PathBuf) -> Result<Selection, String> {
        match &self.accepted {
            Some((p, replace)) if p == &path => Ok(Selection {
                path,
                replace: *replace,
            }),
            _ => Err("Chooser final candidate was not validated; nothing saved.".into()),
        }
    }
}
fn file_identity(path: &Path) -> Option<(u32, u64)> {
    use std::os::windows::fs::OpenOptionsExt;
    let file = std::fs::OpenOptions::new()
        .read(true)
        .share_mode(7)
        .open(path)
        .ok()?;
    crate::component_mrp_native::identity(&file).ok()
}
#[derive(Debug)]
pub(crate) struct Selection {
    pub path: PathBuf,
    pub replace: bool,
}
#[repr(C)]
struct EventV {
    query: unsafe extern "system" fn(*mut Sink, *const Guid, *mut *mut c_void) -> Hr,
    add: unsafe extern "system" fn(*mut Sink) -> u32,
    release: unsafe extern "system" fn(*mut Sink) -> u32,
    ok: unsafe extern "system" fn(*mut Sink, *mut c_void) -> Hr,
    changing: unsafe extern "system" fn(*mut Sink, *mut c_void, *mut c_void) -> Hr,
    changed: unsafe extern "system" fn(*mut Sink, *mut c_void) -> Hr,
    selection: unsafe extern "system" fn(*mut Sink, *mut c_void) -> Hr,
    share: unsafe extern "system" fn(*mut Sink, *mut c_void, *mut c_void, *mut u32) -> Hr,
    type_changed: unsafe extern "system" fn(*mut Sink, *mut c_void) -> Hr,
    overwrite: unsafe extern "system" fn(*mut Sink, *mut c_void, *mut c_void, *mut u32) -> Hr,
}
#[repr(C)]
struct Sink {
    v: *const EventV,
    refs: AtomicU32,
    state: Mutex<Candidate>,
}
unsafe extern "system" fn query(s: *mut Sink, iid: *const Guid, out: *mut *mut c_void) -> Hr {
    if out.is_null() {
        return 0x80004003u32 as i32;
    }
    *out = std::ptr::null_mut();
    if *iid == UNKNOWN || *iid == EVENTS {
        *out = s.cast();
        add(s);
        0
    } else {
        0x80004002u32 as i32
    }
}
unsafe extern "system" fn add(s: *mut Sink) -> u32 {
    (*s).refs.fetch_add(1, Ordering::Relaxed) + 1
}
unsafe extern "system" fn release(s: *mut Sink) -> u32 {
    let n = (*s).refs.fetch_sub(1, Ordering::AcqRel) - 1;
    if n == 0 {
        drop(Box::from_raw(s));
    }
    n
}
unsafe extern "system" fn ok(s: *mut Sink, d: *mut c_void) -> Hr {
    match result_path(d) {
        Ok(p) => {
            (*s).state
                .lock()
                .unwrap_or_else(|e| e.into_inner())
                .accept(p);
            0
        }
        Err(_) => {
            (*s).state
                .lock()
                .unwrap_or_else(|e| e.into_inner())
                .reset("invalid file-ok");
            0x80004005u32 as i32
        }
    }
}
unsafe extern "system" fn changing(s: *mut Sink, _: *mut c_void, _: *mut c_void) -> Hr {
    changed(s, std::ptr::null_mut())
}
unsafe extern "system" fn changed(s: *mut Sink, _: *mut c_void) -> Hr {
    (*s).state
        .lock()
        .unwrap_or_else(|e| e.into_inner())
        .reset("selection/folder/type change");
    0
}
unsafe extern "system" fn share(s: *mut Sink, _: *mut c_void, _: *mut c_void, r: *mut u32) -> Hr {
    *r = 0;
    (*s).state
        .lock()
        .unwrap_or_else(|e| e.into_inner())
        .reset("share");
    0
}
unsafe extern "system" fn overwrite(
    s: *mut Sink,
    _: *mut c_void,
    item: *mut c_void,
    r: *mut u32,
) -> Hr {
    *r = 0; // FDEOR_DEFAULT: keep Windows' prompt, never respond ACCEPT here.
    match item_path(item) {
        Ok(p) => {
            (*s).state
                .lock()
                .unwrap_or_else(|e| e.into_inner())
                .overwrite(p);
            0
        }
        Err(_) => {
            (*s).state
                .lock()
                .unwrap_or_else(|e| e.into_inner())
                .reset("invalid overwrite");
            0x80004005u32 as i32
        }
    }
}
static EVENTS_V: EventV = EventV {
    query,
    add,
    release,
    ok,
    changing,
    changed,
    selection: changed,
    share,
    type_changed: changed,
    overwrite,
};

/// Called on a dedicated STA worker. Owner HWND is borrowed only for synchronous modal Show.
pub(crate) fn choose(
    owner: isize,
    name: &str,
    folder: Option<&Path>,
) -> Result<Option<Selection>, String> {
    unsafe {
        check(CoInitializeEx(std::ptr::null_mut(), 2))?;
        let _apartment = Apartment;
        let mut raw = std::ptr::null_mut();
        check(CoCreateInstance(
            &SAVE,
            std::ptr::null_mut(),
            1,
            &FILE_DIALOG,
            &mut raw,
        ))?;
        let dialog = Com(raw);
        let v = &**(raw as *const *const DialogV);
        let mut options = 0;
        check((v.get_options)(raw, &mut options))?;
        check((v.options)(raw, options | 0x2 | 0x40 | 0x02000000))?;
        let label = wide("Excel workbook".as_ref());
        let pattern = wide("*.xlsx".as_ref());
        let filter = Filter {
            name: label.as_ptr(),
            spec: pattern.as_ptr(),
        };
        check((v.types)(raw, 1, &filter))?;
        let extension = wide("xlsx".as_ref());
        check((v.extension)(raw, extension.as_ptr()))?;
        let name = wide(name.as_ref());
        check((v.filename)(raw, name.as_ptr()))?;
        if let Some(folder) = folder {
            let path = wide(folder.as_os_str());
            let mut item = std::ptr::null_mut();
            if SHCreateItemFromParsingName(
                path.as_ptr(),
                std::ptr::null_mut(),
                &SHELL_ITEM,
                &mut item,
            ) >= 0
            {
                let item = Com(item);
                check((v.folder)(raw, item.0))?;
            }
        }
        let sink = Box::into_raw(Box::new(Sink {
            v: &EVENTS_V,
            refs: AtomicU32::new(1),
            state: Mutex::new(Candidate::default()),
        }));
        let mut cookie = 0;
        let advised = (v.advise)(raw, sink.cast(), &mut cookie);
        if advised < 0 {
            release(sink);
            return Err(format!("Chooser event binding failed: {advised}"));
        }
        let hr = (v.show)(raw, owner as *mut c_void);
        let result = if hr == 0x800704c7u32 as i32 {
            Ok(None)
        } else if hr < 0 {
            Err(format!("Windows chooser failed: 0x{:08X}", hr as u32))
        } else {
            result_path(raw)
                .and_then(|p| {
                    (*sink)
                        .state
                        .lock()
                        .unwrap_or_else(|e| e.into_inner())
                        .finish(p)
                })
                .map(Some)
        };
        #[cfg(test)]
        println!(
            "actual chooser trace {:?}; result={result:?}",
            (*sink)
                .state
                .lock()
                .unwrap_or_else(|e| e.into_inner())
                .trace
        );
        (v.unadvise)(raw, cookie);
        release(sink);
        drop(dialog);
        result
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn candidate_changes_and_final_completion_do_not_transfer_consent() {
        let mut c = Candidate::default();
        let root = PathBuf::from(std::env::var("KST_MRP_TEST_ROOT").unwrap())
            .join(crate::component_mrp_native::new_id().unwrap());
        std::fs::create_dir_all(&root).unwrap();
        let a = root.join("A.xlsx");
        std::fs::write(&a, b"A").unwrap();
        let b = root.join("B.xlsx");
        c.overwrite(a.clone());
        assert!(c.finish(a.clone()).is_err());
        c.reset("No then change");
        c.accept(b.clone());
        assert!(!c.finish(b).unwrap().replace);
        c.overwrite(a.clone());
        c.accept(a.clone());
        assert!(c.finish(a.clone()).unwrap().replace);
        c.reset("another submission");
        c.accept(a.clone());
        assert!(!c.finish(a).unwrap().replace);
    }
    #[test]
    fn accepted_new_candidate_collision_refuses_production_replacement() {
        let root = PathBuf::from(std::env::var("KST_MRP_TEST_ROOT").unwrap())
            .join(crate::component_mrp_native::new_id().unwrap());
        std::fs::create_dir_all(&root).unwrap();
        let target = root.join("late.xlsx");
        let mut state = Candidate::default();
        state.accept(target.clone());
        let selection = state.finish(target.clone()).unwrap();
        std::fs::write(&target, b"late A").unwrap();
        assert!(!selection.replace);
        assert!(crate::component_mrp_native::publish_selected(
            &selection.path,
            b"B",
            selection.replace,
            false
        )
        .is_err());
        assert_eq!(std::fs::read(&target).unwrap(), b"late A");
        assert!(!std::fs::read_dir(root).unwrap().any(|p| p
            .unwrap()
            .file_name()
            .to_string_lossy()
            .starts_with(".kst-mrp-")));
    }
    #[test]
    #[ignore = "Interactive isolated Windows chooser; no publication or associations"]
    fn actual_windows_chooser() {
        let folder = PathBuf::from(std::env::var("KST_MRP_TEST_ROOT").unwrap());
        std::fs::create_dir_all(&folder).unwrap();
        let name = std::env::var("KST_CHOOSER_NAME").unwrap_or("existing.xlsx".into());
        let result = choose(0, &name, Some(&folder));
        println!("CHOOSER RESULT {result:?}");
        assert!(result.is_ok());
    }
}
