import { save } from '@tauri-apps/plugin-dialog';
import { writeFile } from '@tauri-apps/plugin-fs';
import { isRunningInTauri } from '../api/tauri-bridge';

export type WorkbookSaveResult =
  | { kind: 'saved'; fileName: string }
  | { kind: 'cancelled' };

/** Saves a generated Stage 11-A workbook only after the user chooses its destination. */
export async function saveLongTermShortagesWorkbook(blob: Blob, fileName: string): Promise<WorkbookSaveResult> {
  if (!isRunningInTauri()) {
    saveBrowserDownload(blob, fileName);
    return { kind: 'saved', fileName };
  }

  const path = await save({
    title: 'Save Workspace Shortages Workbook',
    defaultPath: fileName,
    filters: [{ name: 'Excel Workbook', extensions: ['xlsx'] }],
  });
  if (path === null) return { kind: 'cancelled' };

  await writeFile(path, new Uint8Array(await blob.arrayBuffer()));
  return { kind: 'saved', fileName: fileNameFromPath(path) };
}

function saveBrowserDownload(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.hidden = true;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 1_000);
}

function fileNameFromPath(path: string): string {
  const segments = path.split(/[\\/]/);
  return segments[segments.length - 1] || path;
}
