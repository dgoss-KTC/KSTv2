import type { ComponentMrpRequest } from '../api/componentMrpApi';
export interface MrpSettings { horizonWeeks: number; period: string; includeUnconfirmed: boolean }
export const defaultMrpSettings: MrpSettings = { horizonWeeks: 26, period: 'Week', includeUnconfirmed: true };
export interface MrpExportContext { request: ComponentMrpRequest; state: 'loading' | 'ready' | 'error' | 'empty'; expired: boolean }
