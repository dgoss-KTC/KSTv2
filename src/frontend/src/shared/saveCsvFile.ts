import { save } from '@tauri-apps/plugin-dialog';
import { writeFile } from '@tauri-apps/plugin-fs';
import { isRunningInTauri } from '../api/tauri-bridge';

export type SaveCsvResult = { kind: 'saved' | 'downloaded'; fileName: string } | { kind: 'cancelled' };

/**
 * User-initiated Save As for an already generated and validated CSV (base64 of the exact bytes). In the
 * desktop app it uses the Tauri dialog plus a write to the one user-selected path (the existing
 * `dialog:allow-save` / `fs:allow-write-file` capabilities, nothing broader); outside Tauri it starts a
 * browser download. A cancelled dialog writes nothing; any failure is thrown so the caller can keep its
 * state and offer a retry. Shared by the Stage 13 QXtend files and the Stage 14.6 Mass Updater file.
 */
export async function saveCsvFile(contentBase64: string, fileName: string, filterName: string): Promise<SaveCsvResult> {
  const binary = atob(contentBase64);
  const bytes = Uint8Array.from(binary, c => c.charCodeAt(0));
  if (!isRunningInTauri()) {
    const url = URL.createObjectURL(new Blob([bytes], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a');
    link.href = url; link.download = fileName; link.hidden = true;
    document.body.appendChild(link); link.click(); link.remove();
    window.setTimeout(() => {
      if (typeof URL.revokeObjectURL === 'function') URL.revokeObjectURL(url);
    }, 1_000);
    return { kind: 'downloaded', fileName };
  }
  const path = await save({ title: `Save ${fileName}`, defaultPath: fileName,
    filters: [{ name: filterName, extensions: ['csv'] }] });
  if (path === null) return { kind: 'cancelled' };
  await writeFile(path, bytes);
  const segments = path.split(/[\\/]/);
  return { kind: 'saved', fileName: segments[segments.length - 1] || path };
}
