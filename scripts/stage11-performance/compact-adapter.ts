// Offline bridge into the unchanged production grid. No business calculations here:
// every displayed ending/severity and sort key is supplied by the C# prototype.
import type { LongTermShortageRow } from '../../src/frontend/src/api/longTermShortagesApi';
interface Mode {
  severity: string; firstShortDate: string | null; firstAtRiskDate: string | null;
  maximumShortage: number; firstRecoveryDate: string | null; ending: number[]; weeklySeverity: string[];
}
interface Component {
  componentPart: string; isKss: boolean; confirmed: Mode; all: Mode;
}
interface Screen { components: Component[]; weekStarts: string[] }
export function expandScreen(screen: Screen) {
  const rows = (include: boolean) => screen.components.map((component) => {
    const mode = include ? component.all : component.confirmed;
    return { ...component, ...mode, presentation: { isKss: component.isKss },
      episodes: [{ maximumShortage: mode.maximumShortage, firstRecoveryDate: mode.firstRecoveryDate }],
      weeks: screen.weekStarts.map((weekStart: string, index: number) => ({ weekNumber: index + 1, weekStart,
        projectedQoh: mode.ending[index], severity: mode.weeklySeverity[index] })) };
  });
  // The diagnostic bridge only provides fields proven used by the grid. Selected detail is
  // supplied separately before opening the unchanged drawer; never ship this compatibility cast.
  return { ...screen, rows: rows(false) as unknown as LongTermShortageRow[], allReceiptsRows: rows(true) as unknown as LongTermShortageRow[], evidenceIncluded: false };
}
