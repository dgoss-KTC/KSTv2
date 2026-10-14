import { reconcileAnalyticalSave, useAnalyticalSaveRecovery } from '../shared/analyticalSaveRecovery';

export function AnalyticalSaveStatus() {
  const operation = useAnalyticalSaveRecovery();
  if (!operation) return null;
  return <aside aria-label="Excel save outcome"><span role="status">{operation.message}</span>
    {operation.pending && <button type="button" disabled={operation.checking} onClick={() => void reconcileAnalyticalSave()}>Check captured Excel save status</button>}
  </aside>;
}
