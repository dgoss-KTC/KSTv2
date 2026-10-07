import { saveCsvFile, type SaveCsvResult } from '../shared/saveCsvFile';

/** Each prepared file has its own user-initiated Save As; cancellation does not affect other files. */
export function saveQxtendCsv(contentBase64: string, fileName: string): Promise<SaveCsvResult> {
  return saveCsvFile(contentBase64, fileName, 'QXtend CSV');
}
