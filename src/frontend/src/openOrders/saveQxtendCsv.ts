import { save } from '@tauri-apps/plugin-dialog';
import { writeFile } from '@tauri-apps/plugin-fs';
import { isRunningInTauri } from '../api/tauri-bridge';

/** Each prepared file has its own user-initiated Save As; cancellation does not affect other files. */
export async function saveQxtendCsv(contentBase64: string, fileName: string): Promise<{ kind: 'saved' | 'downloaded'; fileName: string } | { kind: 'cancelled' }> {
  const binary = atob(contentBase64);
  const bytes = Uint8Array.from(binary, c => c.charCodeAt(0));
  if (!isRunningInTauri()) {
    const url = URL.createObjectURL(new Blob([bytes], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a');
    link.href = url; link.download = fileName; link.hidden = true;
    document.body.appendChild(link); link.click(); link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1_000);
    return { kind: 'downloaded', fileName };
  }
  const path = await save({ title: `Save ${fileName}`, defaultPath: fileName,
    filters: [{ name: 'QXtend CSV', extensions: ['csv'] }] });
  if (path === null) return { kind: 'cancelled' };
  await writeFile(path, bytes);
  const segments = path.split(/[\\/]/);
  return { kind: 'saved', fileName: segments[segments.length - 1] || path };
}
