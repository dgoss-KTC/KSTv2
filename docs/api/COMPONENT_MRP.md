# Component MRP screen and retained export

2026-10-21 — Part Info & MRP follow-up **COMPLETE / OWNER-ACCEPTED**.
Owner: “The new MRP screen looks great. Accepted.” This decision is separate from offline/artifact
evidence; it does not establish the exact executable tested or every suggested desktop check.
The accepted Stage 18 workbook/calculations remain authoritative. This adds transport/presentation,
not planning rules. C# `ComponentMrpDtos.cs` → built OpenAPI → generated TypeScript is the contract.

## Routes and ownership

All requests require the established GUID `X-KST-Report-Session`. In desktop operation the managed
host owns this stable session; it is never returned to the webview. Browser development read/prepare
uses one process-page session, but protected saving still requires the desktop.

- `POST /api/v1/reports/component-mrp/projection`: request Site, exact applied Part, HorizonWeeks
  (13/26/52/72), Period (Day/Week/Month), IncludeUnconfirmed. Without AcquisitionId this acquires
  through `ComponentMrpService.GenerateAsync`. With AcquisitionId + AsOfDate this selects/regroups
  held evidence only. Horizon changes deliberately acquire anew; no larger-horizon cache optimization.
- `POST /api/v1/reports/component-mrp/screen-preparations`: the same settings plus AcquisitionId and
  AsOfDate are required. Generates the existing Component MRP workbook from held evidence **without
  a source read**. There is no service/reader dependency in this handler. Missing, unknown, expired,
  mismatched or foreign acquisitions return 410, never direct generation. A second validity check
  before publishing prepared bytes rejects expiry/date changes during workbook generation.
- Existing `GET /component-mrp/preparations/{id}/bytes` retains established prepared-byte ownership,
  capacity and expiry. It does not depend on the acquisition's lifetime.
- Existing `POST /component-mrp` remains separate legacy direct generation. It rejects requests
  carrying an AcquisitionId; screen export never uses this route.

Initial missing master is 404 (`Part not found`); invalid inputs/session 400; capacity, busy or failed
acquisition/preparation 503. Failures are sanitized; none becomes a successful empty projection.
Zero demand/receipt flows still return real periods and opening/projected balances.

## Evidence and bounds

`ComponentMrpService` retains **both** `BuildBoth` projections. Selection changes the actual
Confirmed/AllReceipts row, not just an IncludeUnconfirmed argument on an already selected balance.
`ComponentMrpPeriods.Build` supplies labels, bounds, Past Due, partial week/month grouping,
planned-order metrics and unconfirmed contribution markers. DTO quantities are invariant exact
decimal strings. Screen UOM display uses existing `formatLongTermQuantity`; workbook UOM rounding
and its sheet/content/filename generator are unchanged. No Safety Stock amber is added onscreen.
The screen echoes requested applied identity; canonical source identity remains in workbook evidence.

`ComponentMrpAcquisitions`: one latest successful acquisition per owner, eight owners, 30-minute
absolute lifetime (no extension on regroup/export), 100,000 facts per acquisition / 200,000 total,
8 MiB serialized source per acquisition / 16 MiB total. Serialized-source budgets are admission
bounds, not claims of exact managed-heap usage; the two bounded projection graphs also remain held.
Over-capacity fails, without evicting another owner's valid entry. IClock controls retention and local
business-date compatibility. Get validates owner, token, site, requested part, exact horizon and as-of
date, including current local date. Period/mode are pure selections over the same retained pair.
Expired entries prune on access; departure clears screen/export authority immediately, and unreachable
backend evidence remains bounded until replacement/expiry/process shutdown.

The two-slot generation limiter is reused. Native transport reuses the main-window-only existing
`component_mrp_generate`, bounded constructed request and fixed routes; optional AcquisitionId,
AsOfDate and ScreenRead are explicitly forwarded. Screen responses are limited to 512 KiB/506 periods;
existing workbook metadata/byte limits, timeout, no redirects, managed backend identity, epoch rejection,
close lease and report-kind-specific save/status/open authority stay intact. No new capability,
filesystem privilege, webview session authority or arbitrary URL/path transport is introduced.

## Expiry decision and screen lifecycle

Owner decision supersedes design's reacquire-and-warn fallback: **Refresh MRP must publish a refreshed
projection before a new export**. Export never reacquires. Acquisition expiry alone blocks new
preparation without changing export context or releasing an existing exact-byte retry/saved artifact.
Existing host preparation/artifact expiry still applies independently. Explicit Refresh/control/
identity/departure changes retain established context invalidation and recovery behavior.

The lookup starts MRP only after successful identity validation, even if optional sections failed.
Controls target the applied identity, coalesce for 150ms and use cancellation + request-key rejection.
Native MRP reads and preparation serialize close leases; a control read waits for cancelled
preparation transport to finish, and superseded queued requests never dispatch. Old dimmed grids keep
their own captions/settings until replacement. New lookup resets 26/Week/Include; full Refresh retains
settings; inventory-only refresh does not touch MRP. Inputs/cancel/departure/unmount invalidate work.
Existing lookup IDs, unique Lookup action, three tabpanels, comments/supplier sections and shared
display pieces remain. Saved site is validated against the lookup spec before use.

Offline evidence, limitations, retained desktop checklist and explicit owner acceptance:
`docs/implementation/KST_v2_S22_PART_INFO_MRP.md`. Offline/artifact evidence does not imply live-source
parity, itemized desktop verification or proof of the owner-tested executable hash.
