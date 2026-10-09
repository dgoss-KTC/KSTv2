//! Windows platform facilities only. Deliberate concurrent namespace manipulation is outside scope.
use std::os::windows::{ffi::OsStrExt, fs::OpenOptionsExt, io::AsRawHandle};
use std::{
    ffi::c_void,
    fs::{File, OpenOptions},
    io::{Read, Seek, SeekFrom, Write},
    path::{Path, PathBuf},
};

#[repr(C)]
#[derive(Default, Copy, Clone)]
struct Guid {
    a: u32,
    b: u16,
    c: u16,
    d: [u8; 8],
}
#[link(name = "ole32")]
extern "system" {
    fn CoCreateGuid(guid: *mut Guid) -> i32;
    fn CoTaskMemFree(pointer: *mut c_void);
    fn CoInitializeEx(reserved: *mut c_void, flags: u32) -> i32;
    fn CoUninitialize();
}
#[link(name = "shell32")]
extern "system" {
    fn SHGetKnownFolderPath(
        id: *const Guid,
        flags: u32,
        token: *mut c_void,
        path: *mut *mut u16,
    ) -> i32;
    fn ShellExecuteExW(info: *mut ShellInfo) -> i32;
}
#[link(name = "kernel32")]
extern "system" {
    fn SetFileInformationByHandle(
        file: *mut c_void,
        class: u32,
        data: *const c_void,
        length: u32,
    ) -> i32;
    fn GetFileInformationByHandle(file: *mut c_void, info: *mut FileInfo) -> i32;
    fn GetVolumePathNameW(file: *const u16, volume: *mut u16, length: u32) -> i32;
    fn GetVolumeInformationW(
        root: *const u16,
        name: *mut u16,
        name_len: u32,
        serial: *mut u32,
        component: *mut u32,
        flags: *mut u32,
        fs: *mut u16,
        fs_len: u32,
    ) -> i32;
    fn GetDriveTypeW(root: *const u16) -> u32;
}
#[repr(C)]
#[derive(Default)]
struct FileInfo {
    attributes: u32,
    times: [u32; 6],
    volume: u32,
    size_high: u32,
    size_low: u32,
    links: u32,
    id_high: u32,
    id_low: u32,
}
#[repr(C)]
struct ShellInfo {
    size: u32,
    mask: u32,
    hwnd: *mut c_void,
    verb: *const u16,
    file: *const u16,
    parameters: *const u16,
    directory: *const u16,
    show: i32,
    instance: *mut c_void,
    id_list: *mut c_void,
    class: *const u16,
    key: *mut c_void,
    hot_key: u32,
    icon: *mut c_void,
    process: *mut c_void,
}

pub fn new_id() -> Result<String, String> {
    let mut g = Guid::default();
    if unsafe { CoCreateGuid(&mut g) } < 0 {
        return Err("Cannot allocate operation identity.".into());
    }
    Ok(format!(
        "{:08x}-{:04x}-{:04x}-{:02x}{:02x}-{}",
        g.a,
        g.b,
        g.c,
        g.d[0],
        g.d[1],
        g.d[2..]
            .iter()
            .map(|v| format!("{v:02x}"))
            .collect::<String>()
    ))
}
pub fn documents() -> Option<PathBuf> {
    let id = Guid {
        a: 0xfdd39ad0,
        b: 0x238f,
        c: 0x46af,
        d: [0xad, 0xb4, 0x6c, 0x85, 0x48, 0x03, 0x69, 0xc7],
    };
    let mut pointer = std::ptr::null_mut();
    let result = unsafe { SHGetKnownFolderPath(&id, 0, std::ptr::null_mut(), &mut pointer) };
    let path = if result >= 0 && !pointer.is_null() {
        let mut len = 0;
        unsafe {
            while len < 32768 && *pointer.add(len) != 0 {
                len += 1;
            }
        }
        if len < 32768 {
            Some(PathBuf::from(String::from_utf16_lossy(unsafe {
                std::slice::from_raw_parts(pointer, len)
            })))
        } else {
            None
        }
    } else {
        None
    };
    if !pointer.is_null() {
        unsafe { CoTaskMemFree(pointer.cast()) }
    }
    path.filter(|p| p.is_dir())
}
pub fn identity(file: &File) -> Result<(u32, u64), String> {
    let mut info = FileInfo::default();
    if unsafe { GetFileInformationByHandle(file.as_raw_handle(), &mut info) } == 0 {
        return Err(std::io::Error::last_os_error().to_string());
    }
    Ok((
        info.volume,
        ((info.id_high as u64) << 32) | info.id_low as u64,
    ))
}
pub fn validate_destination(path: &Path) -> Result<(), String> {
    let text = path.to_str().ok_or("Destination is not valid Unicode.")?;
    if !path.is_absolute() || text.starts_with("\\\\") || text.len() > 240 {
        return Err("Choose an ordinary local absolute path of at most 240 characters.".into());
    }
    let leaf = path
        .file_name()
        .and_then(|v| v.to_str())
        .ok_or("Choose a filename.")?;
    let wide: Vec<u16> = path.as_os_str().encode_wide().chain(Some(0)).collect();
    let mut volume = [0u16; 261];
    let mut fs = [0u16; 32];
    if unsafe { GetVolumePathNameW(wide.as_ptr(), volume.as_mut_ptr(), 261) } == 0
        || unsafe { GetDriveTypeW(volume.as_ptr()) } != 3
        || unsafe {
            GetVolumeInformationW(
                volume.as_ptr(),
                std::ptr::null_mut(),
                0,
                std::ptr::null_mut(),
                std::ptr::null_mut(),
                std::ptr::null_mut(),
                fs.as_mut_ptr(),
                32,
            )
        } == 0
        || String::from_utf16_lossy(&fs[..fs.iter().position(|v| *v == 0).unwrap_or(32)]) != "NTFS"
    {
        return Err("Initial save support requires a local fixed NTFS destination.".into());
    }
    let stem = leaf.split('.').next().unwrap_or("").to_uppercase();
    if leaf.chars().any(|c| c < ' ' || "<>:\"/\\|?*".contains(c))
        || leaf.ends_with([' ', '.'])
        || !leaf.to_lowercase().ends_with(".xlsx")
        || ["CON", "PRN", "AUX", "NUL"].contains(&stem.as_str())
        || (stem.len() == 4
            && (stem.starts_with("COM") || stem.starts_with("LPT"))
            && "123456789¹²³".contains(stem.chars().last().unwrap()))
    {
        return Err("Choose an ordinary, nonreserved .xlsx filename.".into());
    }
    let mut current = path.parent().ok_or("Destination parent unavailable.")?;
    loop {
        let metadata =
            std::fs::symlink_metadata(current).map_err(|_| "Destination folder unavailable.")?;
        use std::os::windows::fs::MetadataExt;
        if metadata.file_attributes() & 0x400 != 0 || !metadata.is_dir() {
            return Err("Reparse or non-directory destination routes are unsupported.".into());
        }
        match current.parent() {
            Some(parent) => current = parent,
            None => break,
        }
    }
    Ok(())
}

/// Only this function creates/publishes report files. RAII closes H, never deletes it implicitly.
#[cfg(test)]
pub fn publish(path: &Path, bytes: &[u8], fail_before_commit: bool) -> Result<(u32, u64), String> {
    publish_selected(path, bytes, false, fail_before_commit)
}

pub fn publish_selected(
    path: &Path,
    bytes: &[u8],
    replace: bool,
    fail_before_commit: bool,
) -> Result<(u32, u64), String> {
    publish_controlled(path, bytes, replace, fail_before_commit, |_| {})
}

#[derive(Clone, Copy, Debug)]
pub(crate) enum PublicationPhase {
    Staged,
    BeforeCommit,
    Committed,
}

pub(crate) fn publish_controlled(
    path: &Path,
    bytes: &[u8],
    replace: bool,
    fail_before_commit: bool,
    checkpoint: impl Fn(PublicationPhase),
) -> Result<(u32, u64), String> {
    validate_destination(path)?;
    let parent = path.parent().ok_or("Parent missing")?;
    let temp = parent.join(format!(".kst-mrp-{}.tmp", new_id()?));
    let mut file = OpenOptions::new()
        .read(true)
        .write(true)
        .create_new(true)
        .access_mode(0x80000000 | 0x40000000 | 0x10000)
        .share_mode(0)
        .open(&temp)
        .map_err(|_| "Exclusive report staging failed.")?;
    let staged = (|| -> Result<(), String> {
        file.write_all(bytes)
            .map_err(|_| "Workbook staging write failed.")?;
        file.seek(SeekFrom::Start(0))
            .map_err(|_| "Staging seek failed.")?;
        let mut actual = Vec::new();
        Read::by_ref(&mut file)
            .take(bytes.len() as u64 + 1)
            .read_to_end(&mut actual)
            .map_err(|_| "Staging verification failed.")?;
        if actual != bytes {
            return Err("Staged workbook differs from preparation.".into());
        }
        file.sync_all().map_err(|_| "Workbook flush failed.")?;
        checkpoint(PublicationPhase::Staged);
        if fail_before_commit {
            return Err("Injected prepublication failure.".into());
        }
        Ok(())
    })();
    if let Err(error) = staged {
        cleanup(&file)?;
        return Err(error);
    }
    let id = identity(&file).map_err(|error| {
        let _ = cleanup(&file);
        error
    })?;
    let wide: Vec<u16> = path.as_os_str().encode_wide().collect();
    // x64 FILE_RENAME_INFO: union0, root8, length16, name20, sizeof24. Vec<u64> alignment8.
    let size = 24 + wide.len() * 2 + 2;
    let mut buffer = vec![0u64; (size + 7) / 8];
    let pointer = buffer.as_mut_ptr().cast::<u8>();
    unsafe {
        *pointer = u8::from(replace);
        std::ptr::write_unaligned(pointer.add(16).cast::<u32>(), (wide.len() * 2) as u32);
        std::ptr::copy_nonoverlapping(wide.as_ptr().cast::<u8>(), pointer.add(20), wide.len() * 2);
    }
    checkpoint(PublicationPhase::BeforeCommit);
    let result =
        unsafe { SetFileInformationByHandle(file.as_raw_handle(), 3, pointer.cast(), size as u32) };
    if result == 0 {
        let error = std::io::Error::last_os_error();
        cleanup(&file)?;
        return Err(if matches!(error.raw_os_error(), Some(80 | 183)) {
            "Destination exists. Choose another confirmed filename; nothing was replaced.".into()
        } else {
            format!("Publication refused (destination may be locked, read-only or access denied): {error}")
        });
    }
    // Successful namespace commitment: no cleanup reachable below this line.
    checkpoint(PublicationPhase::Committed);
    drop(file);
    Ok(id)
}
fn cleanup(file: &File) -> Result<(), String> {
    let delete = 1u8;
    if unsafe {
        SetFileInformationByHandle(file.as_raw_handle(), 4, (&delete as *const u8).cast(), 1)
    } == 0
    {
        return Err(
            "Save failed; owned temporary file could not be removed. No cleanup by name attempted."
                .into(),
        );
    }
    Ok(())
}
pub fn verify_artifact(path: &Path, id: (u32, u64), bytes: &[u8]) -> Result<(), String> {
    let mut file = File::open(path).map_err(|_| "Saved workbook is missing.")?;
    if identity(&file)? != id {
        return Err("Saved workbook identity changed.".into());
    }
    let mut actual = Vec::new();
    Read::by_ref(&mut file)
        .take(bytes.len() as u64 + 1)
        .read_to_end(&mut actual)
        .map_err(|_| "Saved workbook cannot be verified.")?;
    if actual != bytes {
        return Err("Saved workbook contents changed.".into());
    }
    Ok(())
}
pub fn dispatch(path: &Path) -> Result<(), String> {
    // Dedicated synchronous STA call: launch acknowledgment, never proof of loaded bytes.
    let initialized = unsafe { CoInitializeEx(std::ptr::null_mut(), 2) };
    if initialized < 0 {
        return Err("Associated-application worker initialization failed.".into());
    }
    let verb: Vec<u16> = "open\0".encode_utf16().collect();
    let file: Vec<u16> = path.as_os_str().encode_wide().chain(Some(0)).collect();
    let mut info = ShellInfo {
        size: std::mem::size_of::<ShellInfo>() as u32,
        mask: 0x100 | 0x400,
        hwnd: std::ptr::null_mut(),
        verb: verb.as_ptr(),
        file: file.as_ptr(),
        parameters: std::ptr::null(),
        directory: std::ptr::null(),
        show: 1,
        instance: std::ptr::null_mut(),
        id_list: std::ptr::null_mut(),
        class: std::ptr::null(),
        key: std::ptr::null_mut(),
        hot_key: 0,
        icon: std::ptr::null_mut(),
        process: std::ptr::null_mut(),
    };
    let result = unsafe { ShellExecuteExW(&mut info) };
    let error = if result == 0 {
        Some(std::io::Error::last_os_error().to_string())
    } else {
        None
    };
    unsafe { CoUninitialize() };
    error.map_or(Ok(()), Err)
}
