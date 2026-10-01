import { describe, expect, it } from 'vitest';
import { ApiError } from '../api/client';
import { qxtendExportFeedback } from './exportFeedback';

describe('QXtend export failure feedback', () => {
  const failed = (status: number, payload: unknown) => new ApiError(status, '/qxtend-export', JSON.stringify(payload));

  it('identifies an old sidecar route without attributing it to a validation rule', () => {
    expect(qxtendExportFeedback(new ApiError(404, '/qxtend-export', '')))
      .toMatch(/endpoint is unavailable in the running backend.*Rebuild\/restart the desktop sidecar/);
  });

  it('identifies a legitimate changed source, affected count and retained proposals', () => {
    expect(qxtendExportFeedback(failed(409, { issueCode: 'source-changed', affectedRowCount: 2 })))
      .toMatch(/2 changed rows: original values or line scope changed in QAD.*proposals remain/);
    expect(qxtendExportFeedback(failed(400, { issueCode: 'invalid-values-or-reason', affectedRowCount: 1 })))
      .toMatch(/1 changed row: a value or Reason Code is invalid/);
    expect(qxtendExportFeedback(failed(404, { issueCode: 'line-missing-or-closed', affectedRowCount: 3 })))
      .toMatch(/3 changed rows: a line is missing or no longer open/);
    expect(qxtendExportFeedback(failed(503, { issueCode: 'source-unavailable', affectedRowCount: 1 })))
      .toMatch(/1 changed row: QAD is unavailable/);
  });

  it('never displays unrecognized response contents', () => {
    const text = qxtendExportFeedback(failed(409, { issueCode: 'arbitrary', affectedRowCount: 1, detail: 'Sensitive source values' }));
    expect(text).toContain('HTTP 409');
    expect(text).not.toContain('Sensitive');
  });
});
