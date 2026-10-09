# Stage 18-I3.4 — F1 frontend completion ordering correction

Implemented 2026-10-09; pending fresh-context F1 re-review. Stage18 remains unaccepted.
Application `0.1.0-alpha.18` / installer `0.1.0` unchanged; frontend-only, no sidecar republication.

## Bounded correction

Current source matched F1. ReportsPanel routes native events, Save As replies and status replies
through one `applyResult` rule: mounted, current captured operation identity and not terminally
settled. Terminal results settle once; duplicate/obsolete completions cannot change message, unknown
state/reconciliation ID, artifact, open-failure state or activity. Intermediate Saved remains
nonterminal with the captured ID and observational Check save status. Error text is not an ID source.
Save/status rejection uses the same identity/settlement predicate. Effect-event callback reads current
result application without restarting the native listener; no native/IPC/request semantics changed.

Each busy action captures an epoch. Terminal settlement releases its action immediately and invalidates
obsolete finally paths; no wait for the obsolete invoke promise. New save/status/retry owns a new epoch.
Explicit retry success/failure requires its epoch, artifact and originating operation still match.
Retry completion is independent of already-terminal save completion; old Save As replies cannot restore
old open failures. Input invalidation continues to preserve saved-artifact retry. Existing legacy
mocked message-only open-failure result remains supported alongside authoritative openFailed.

No host/chooser/publication/contract/backend/workbook/source/algorithm changes. Independent R1/R2/R4
adjudications not reopened. This record describes implementation evidence, not independent resolution
of F1 or owner acceptance.

## Verification

19 ReportsPanel tests passed (11 prior,8 deterministic ordering regressions). New controlled promises
and React act cover: terminal A→active B→late A success/rejection and obsolete finally; terminal A→
unknown B→late success retains B's exact status ID; open-failed A→active/completed successful Retry
Open→late successful A; pending status→terminal A/new B→stale success/rejection; terminal A→Retry
Open success→stale status rejection; duplicate terminal events. Assertions include visible status,
buttons/navigation-busy callback, artifact recovery and save/status/open IPC counts/no dispatch replay.
Existing current replies, cancel/failure retry, intermediate Saved/final success/failure, hidden-panel
retention, preferences, defaults and generation invalidation pass.

Typecheck, scoped ESLint (including repository style rules) and fresh production `npm run build`
passed; complete output and explicit exit results retained. Earlier missing I31 build evidence stays
unverified historically; this build verifies I34 current source only. No separate formatter is configured
in package.json; surrounding style retained and lint used as applicable scoped style gate.
Initial scoped run exposed loss of message-only mocked open-failure compatibility; retained that
existing behavior, then full scope passed. This was corrected before delivery.

Evidence: `C:\dev\kst_v2_tmp\Stage 18\I34_20261009_1400\local-evidence` (tests/types/lint/build,
exit-results, baseline/final hashes/diff preservation). Tracked buildinfo restored from exact fresh
baseline after build. All other baseline content hashes retained; no dependency/capability/version
changes. AFT scoped TypeScript diagnostics reported0errors; Tier2 gaps remain, not full health proof.
Broad formatting/lockedStage10lint/historicalRustAFT exceptions remain outside scoped passes.

## Pending verification

Fresh-context review limited to F1/currently affected UI lifecycle, then existing bounded owner
desktop checklist (singleprompt, close/cancel/discard, hidden unknown recovery, saved/openfailed retry,
header/continuation). Tests use mocked host IPC/events; no app/association/live acquisition performed.
No backend/Rust/platform reruns or sidecar publish for unchanged layers. Stage18unaccepted.
