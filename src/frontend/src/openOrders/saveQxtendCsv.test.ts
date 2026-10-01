import { beforeEach, describe, expect, it, vi } from 'vitest';
import { saveQxtendCsv } from './saveQxtendCsv';

const dialog = vi.fn(); const write = vi.fn(); const tauri = vi.fn();
vi.mock('@tauri-apps/plugin-dialog', () => ({ save: (...args: unknown[]) => dialog(...args) }));
vi.mock('@tauri-apps/plugin-fs', () => ({ writeFile: (...args: unknown[]) => write(...args) }));
vi.mock('../api/tauri-bridge', () => ({ isRunningInTauri: () => tauri() }));

describe('QXtend user-selected Save As', () => {
  beforeEach(() => { dialog.mockReset(); write.mockReset(); tauri.mockReturnValue(true); });

  it('writes exact validated bytes only to the selected destination', async () => {
    dialog.mockResolvedValue('C:\\Exports\\Chosen.csv'); write.mockResolvedValue(undefined);
    const result = await saveQxtendCsv(btoa('M,SO\r\n'), 'UpdateQuantities.csv');
    expect(result).toEqual({ kind: 'saved', fileName: 'Chosen.csv' });
    expect(dialog).toHaveBeenCalledWith(expect.objectContaining({ defaultPath: 'UpdateQuantities.csv' }));
    expect(write).toHaveBeenCalledWith('C:\\Exports\\Chosen.csv', new Uint8Array([77, 44, 83, 79, 13, 10]));
  });

  it('cancellation never writes, and failures propagate for retry', async () => {
    dialog.mockResolvedValueOnce(null).mockResolvedValue('C:\\Exports\\Chosen.csv');
    expect(await saveQxtendCsv(btoa('a'), 'UpdatePrices.csv')).toEqual({ kind: 'cancelled' });
    expect(write).not.toHaveBeenCalled();
    write.mockRejectedValueOnce(new Error('disk full')).mockResolvedValueOnce(undefined);
    await expect(saveQxtendCsv(btoa('a'), 'UpdatePrices.csv')).rejects.toThrow('disk full');
    await expect(saveQxtendCsv(btoa('a'), 'UpdatePrices.csv')).resolves.toEqual({ kind: 'saved', fileName: 'Chosen.csv' });
  });
});
