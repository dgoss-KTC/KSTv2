import { beforeEach, describe, expect, it, vi } from 'vitest';
import { saveCsvFile } from './saveCsvFile';
import { saveQxtendCsv } from '../openOrders/saveQxtendCsv';

const dialog = vi.fn(); const write = vi.fn(); const tauri = vi.fn();
vi.mock('@tauri-apps/plugin-dialog', () => ({ save: (...args: unknown[]) => dialog(...args) }));
vi.mock('@tauri-apps/plugin-fs', () => ({ writeFile: (...args: unknown[]) => write(...args) }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => tauri() }));

describe('shared CSV Save As', () => {
  beforeEach(() => { dialog.mockReset(); write.mockReset(); tauri.mockReturnValue(true); });

  it('writes the exact bytes only to the selected destination with the caller-specific file filter', async () => {
    dialog.mockResolvedValue('C:\\Exports\\Mine.csv'); write.mockResolvedValue(undefined);

    const result = await saveCsvFile(btoa('P-1,5,9/14/2026\r\n'), 'SW-MPS-Adjustments-2026-09-14.csv', 'Mass Updater CSV');

    expect(result).toEqual({ kind: 'saved', fileName: 'Mine.csv' });
    expect(dialog).toHaveBeenCalledWith({
      title: 'Save SW-MPS-Adjustments-2026-09-14.csv', defaultPath: 'SW-MPS-Adjustments-2026-09-14.csv',
      filters: [{ name: 'Mass Updater CSV', extensions: ['csv'] }],
    });
    expect(write).toHaveBeenCalledTimes(1);
    expect(write.mock.calls[0][0]).toBe('C:\\Exports\\Mine.csv');
    expect(Array.from(write.mock.calls[0][1] as Uint8Array)).toEqual(Array.from(new TextEncoder().encode('P-1,5,9/14/2026\r\n')));
  });

  it('cancelling writes nothing and a write failure propagates for the caller to retry', async () => {
    dialog.mockResolvedValueOnce(null);
    expect(await saveCsvFile(btoa('a'), 'a.csv', 'Mass Updater CSV')).toEqual({ kind: 'cancelled' });
    expect(write).not.toHaveBeenCalled();

    dialog.mockResolvedValue('C:\\Exports\\a.csv'); write.mockRejectedValueOnce(new Error('disk full'));
    await expect(saveCsvFile(btoa('a'), 'a.csv', 'Mass Updater CSV')).rejects.toThrow('disk full');
  });

  it('the Stage 13 QXtend wrapper keeps its own filter name and behavior', async () => {
    dialog.mockResolvedValue('C:\\Exports\\DateChange.csv'); write.mockResolvedValue(undefined);

    expect(await saveQxtendCsv(btoa('x'), 'DateChange.csv')).toEqual({ kind: 'saved', fileName: 'DateChange.csv' });
    expect(dialog).toHaveBeenCalledWith(expect.objectContaining({ filters: [{ name: 'QXtend CSV', extensions: ['csv'] }] }));
  });

  it('outside the desktop app it starts a browser download with the same bytes and never calls Tauri', async () => {
    tauri.mockReturnValue(false);
    const created: Blob[] = [];
    vi.stubGlobal('URL', { createObjectURL: (blob: Blob) => { created.push(blob); return 'blob:x'; }, revokeObjectURL: vi.fn() });
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});

    const result = await saveCsvFile(btoa('abc'), 'f.csv', 'Mass Updater CSV');

    expect(result).toEqual({ kind: 'downloaded', fileName: 'f.csv' });
    expect(click).toHaveBeenCalledTimes(1);
    const text = await new Promise<string>(done => { const reader = new FileReader(); reader.onload = () => done(String(reader.result)); reader.readAsText(created[0]); });
    expect(text).toBe('abc');
    expect(dialog).not.toHaveBeenCalled();
    expect(write).not.toHaveBeenCalled();
    click.mockRestore(); vi.unstubAllGlobals();
  });
});
