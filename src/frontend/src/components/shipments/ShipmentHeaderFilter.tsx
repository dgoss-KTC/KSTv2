import { useCallback, type RefObject } from 'react';
import type { ShipmentColumnFilter, WorkspaceShipmentsClient, WorkspaceShipmentsSpec } from '../../api/workspaceShipmentsApi';
import { ColumnFilterEditor } from '../shared/ColumnFilterEditor';

type Props = { column: WorkspaceShipmentsSpec['columns'][number]; anchor: RefObject<HTMLElement | null>;
  filter?: ShipmentColumnFilter; client: WorkspaceShipmentsClient; operationId: string;
  version: number | string; spec: WorkspaceShipmentsSpec; onClose: () => void;
  onApply: (filter: ShipmentColumnFilter | null) => void };

export function ShipmentHeaderFilter({ column, client, operationId, version, ...props }: Props) {
  const loadFacets = useCallback(async (search: string, retained: string[], signal: AbortSignal) => {
    const result = await client.values(column.id, search, retained, version, signal);
    if (result.operationId !== operationId || String(result.viewVersion) !== String(version) || result.column !== column.id)
      throw new Error('Facet identity changed.');
    return result;
  }, [client, column.id, operationId, version]);
  return <ColumnFilterEditor {...props} loadFacets={loadFacets} column={{ ...column, shipmentShortcuts: true,
    ...(column.id === 'onTime' || column.filterKind === 'values' ? { valueLabels: { '1': '✓ On time', '0': '✕ Late' },
      blankLabel: '— Not determined', valueHelp: 'Not determined: missing due/perform date or non-standard SO type. Excluded from On Time Rate.' } : {}) }} />;
}
