import { beforeEach, describe, expect, it, vi } from 'vitest';
import { saveLongTermShortagesWorkbook } from './saveLongTermShortagesWorkbook';

const saveMock = vi.fn();
const writeFileMock = vi.fn();
const isRunningInTauriMock = vi.fn();

vi.mock('@tauri-apps/plugin-dialog', () => ({ save: (...args: unknown[]) => saveMock(...args) }));
vi.mock('@tauri-apps/plugin-fs', () => ({ writeFile: (...args: unknown[]) => writeFileMock(...args) }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => isRunningInTauriMock() }));

describe('saveLongTermShortagesWorkbook', () => {
  beforeEach(() => {
    saveMock.mockReset();
    writeFileMock.mockReset();
    isRunningInTauriMock.mockReturnValue(true);
  });

  it('uses native Save As with the server filename and Excel filter, then writes exact bytes', async () => {
    saveMock.mockResolvedValue('C:\\Exports\\Workspace-Shortages-2026-09-15.xlsx');
    const bytes = new Uint8Array([0x50, 0x4b, 0x03, 0x04]);
    const blob = { arrayBuffer: vi.fn().mockResolvedValue(bytes.buffer) } as unknown as Blob;

    const result = await saveLongTermShortagesWorkbook(blob, 'Workspace-Shortages-2026-09-15.xlsx');

    expect(saveMock).toHaveBeenCalledWith({ title: 'Save Workspace Shortages Workbook', defaultPath: 'Workspace-Shortages-2026-09-15.xlsx', filters: [{ name: 'Excel Workbook', extensions: ['xlsx'] }] });
    expect(writeFileMock).toHaveBeenCalledWith('C:\\Exports\\Workspace-Shortages-2026-09-15.xlsx', bytes);
    expect(result).toEqual({ kind: 'saved', fileName: 'Workspace-Shortages-2026-09-15.xlsx' });
  });

  it('does not write or report a save when Save As is cancelled', async () => {
    saveMock.mockResolvedValue(null);

    await expect(saveLongTermShortagesWorkbook(new Blob(['workbook']), 'Workspace.xlsx')).resolves.toEqual({ kind: 'cancelled' });
    expect(writeFileMock).not.toHaveBeenCalled();
  });

  it('propagates a native write failure', async () => {
    saveMock.mockResolvedValue('C:\\Exports\\Workspace.xlsx');
    writeFileMock.mockRejectedValue(new Error('access denied'));
    const blob = { arrayBuffer: vi.fn().mockResolvedValue(new Uint8Array([1]).buffer) } as unknown as Blob;

    await expect(saveLongTermShortagesWorkbook(blob, 'Workspace.xlsx')).rejects.toThrow('access denied');
  });
});
