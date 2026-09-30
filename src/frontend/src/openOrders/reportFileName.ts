/** Mirrors the API's suggested workspace prefix if an intermediary omits Content-Disposition. */
export function openOrdersFileName(displayName: string | null | undefined, site: string, acquiredAtUtc: string): string {
  const normalize = (value: string) => value.replace(/[<>:"/\\|?*\p{Cc}]/gu, ' ')
    .replace(/[\s_-]+/g, '-')
    .replace(/^[ ._-]+|[ ._-]+$/g, '');
  const prefix = normalize(displayName?.trim() || site) || normalize(site) || 'Workspace';
  return `${prefix}-Open-Orders-${acquiredAtUtc.slice(0, 10)}.xlsx`;
}
