# S0.4C — npm Development-Dependency Advisory Remediation

**Status:** COMPLETE / ACCEPTED — 2026-08-25

**Implementation date:** 2026-08-25
**Acceptance date:** 2026-08-25 (project-owner acceptance of the implemented remediation)
**Starting commit:** `05f90576f38c81cb0456b30c05497d193b716947` (`fix: restrict Tauri shell capability`)
**Finding addressed:** `S0.3-F001` (npm development-tooling advisories — Confirmed)

This document is **remediation evidence**, not normative policy. It does not replace the accepted
S0.2 baseline (`docs/security/SECURITY_BASELINE.md`) or the accepted S0.3 evidence
(`docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`). Required security properties remain
defined by `SECURITY.md` and `docs/security/`.

Bounded result wording used throughout, per `S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §3:

- A native advisory check reporting "no advisories" means *no known advisories for the
  dependency graph it was able to evaluate at the time of execution* — not that dependencies are
  secure.
- npm/tool-reported severities are quoted as **npm-reported severity**, never as a KST risk
  severity. No KST risk-severity framework exists or was invented here.
- **npm-reported severity ≠ final KST risk severity.** **Confirmed advisory ≠ confirmed
  exploitability.** Development-only exposure ≠ production runtime vulnerability.

---

## 1. Purpose

S0.3 (`S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §5.2/§10, finding `S0.3-F001`, Confirmed) observed
three npm advisories in the frontend development tooling. S0.3 recorded the finding but performed
**no** remediation by design.

S0.4C's objective:

> Understand the affected development-tooling path, make the smallest controlled dependency change
> that removes the known advisory state, and prove the OpenAPI generation pipeline still behaves
> correctly.

Concretely, S0.4C: (1) establishes how each affected package is actually used by KST; (2)
distinguishes runtime from development/build-time exposure; (3) determines the minimum appropriate
dependency change; (4) remediates the known advisory dependency graph via a compatible supported
version path; (5) preserves the authoritative OpenAPI → TypeScript generation workflow; (6)
re-verifies the dependency graph with `npm audit`; (7) verifies generated-contract and frontend
behavior; and (8) records this durable evidence.

This is **not** "make npm audit green by whatever means necessary." No `npm audit fix`, no broad
`npm update`, no new direct dependency, and no unrelated package was changed (see §14).

## 2. Starting Advisory State

Pre-remediation `npm audit --json` (from `src/frontend`; non-mutating) confirmed the accepted
S0.3-F001 conditions exactly — npm-reported severity counts
`{info: 0, low: 0, moderate: 1, high: 2, critical: 0, total: 3}`; dependency counts
`{prod: 6, dev: 323, optional: 52, peer: 8, total: 328}`.

| Package (locked) | Direct/transitive | Path | npm-reported severity | Advisory range (per npm) |
|---|---|---|---|---|
| `openapi-typescript@6.7.6` | direct devDependency | `kst-frontend → openapi-typescript` | moderate | `5.1.1 – 6.7.6` (exposure via its `undici` dependency) |
| `undici@5.29.0` | transitive | `openapi-typescript → undici` (6.7.6 declares `undici@^5.28.4`) | high | `<=6.27.0` (12 GitHub advisories) |
| `nanoid@3.3.16` | transitive | `vite → postcss → nanoid` (postcss declares `nanoid@^3.3.16`) | high | `<3.3.18` (GHSA-2v37-7h3g-55p8) |

All three are **development-only** (`npm ls --omit=dev` reported an empty production graph for
these packages). This matches the accepted S0.3 observation; the S0.3 path was re-verified against
the live install/lockfile rather than assumed.

## 3. Dependency / Reachability Analysis

Reachability was established from the live `node_modules`/`package-lock.json` and by searching
`src/frontend/src` for actual imports.

| Package | Dependency type | Path | KST use | Runtime reachable? |
|---|---|---|---|---|
| `openapi-typescript` | direct devDependency | `kst-frontend → openapi-typescript` | Invoked **only** by the `generate:types` npm script: `openapi-typescript ../../docs/openapi/Kst.Api.json -o src/generated/api.ts`. Not imported by any application source (the only source reference is the generated file's header comment). | **No** — development/build-time only (run via `npm run generate:types`) |
| `undici` | transitive | `openapi-typescript → undici` | No direct KST import. `openapi-typescript` could use `undici` to fetch a **remote** OpenAPI schema; KST's invocation passes a **local checked-in file** (`../../docs/openapi/Kst.Api.json`), so no remote fetch occurs. | **No** — present in the dev graph, but the vulnerable HTTP-fetch functionality has **no KST execution path** (local-file input only) |
| `nanoid` | transitive | `vite → postcss → nanoid` | No application-source import. Used internally by `postcss` (unique-ID generation) during Vite CSS processing at build time. | **No** — development/build-time only |
| `postcss` (context) | transitive | `vite → postcss` | CSS processing during the Vite build. | **No** — development/build-time only |

Classification, per the S0.4C vocabulary:

- `openapi-typescript` — **Development/build-time reachable.**
- `undici` — **Present but no KST execution path identified** for its vulnerable network
  functionality (local-file invocation only). It is still present in the development dependency
  graph, which is what the advisory flags; presence in the supply chain is not dismissed merely
  because the network path is not exercised (see `DEPENDENCY_ADMISSION.md` §"Security Advisory
  Handling").
- `nanoid` — **Development/build-time reachable** (via `postcss` during Vite builds).

A development-only finding still warrants dependency hygiene, but the distinction is preserved:
**development-only ≠ production runtime vulnerability.** None of the three packages is in the
shipped application's runtime dependency graph.

## 4. Dependency Admission Review

Applied per `docs/security/DEPENDENCY_ADMISSION.md` to the **existing** direct-dependency version
change (this is not a new dependency — `openapi-typescript` is already a declared devDependency;
only its version changes).

- **Dependency:** `openapi-typescript`
- **Change type:** existing direct development dependency — **major version update** (`6.7.6 → 7.13.0`)
- **Purpose:** OpenAPI → TypeScript contract generation (`generate:types`)
- **Why change is required:** resolve the known advisory state in the existing development
  dependency graph (`S0.3-F001`). The `openapi-typescript` advisory range is `5.1.1–6.7.6`; the
  offered and only supported fix is the 7.x line, which also removes the vulnerable `undici`
  dependency at the root.
- **Runtime inclusion:** **No.** It is a devDependency; `npm ls --omit=dev` confirms it is not in
  the production/runtime graph.
- **Executable/build behavior:** **Yes** — invoked explicitly through `npm run generate:types`.
- **Network behavior:** In KST's actual invocation the input is a **local checked-in schema file**
  (`../../docs/openapi/Kst.Api.json`); no remote fetch is performed. (The 7.x line's
  `@redocly/openapi-core` includes `https-proxy-agent` for *optional* remote-spec support, but KST
  does not invoke that path.)
- **Credential access:** None expected; no special credential mechanism was identified in the
  package or its KST invocation.
- **Alternatives considered:**
  - *Retain vulnerable 6.x* — rejected: leaves `S0.3-F001` open.
  - *Replace the generator* — rejected: would introduce a **new direct dependency** and a workflow
    change, is out of S0.4C scope, and is prohibited by the checkpoint boundary (a separate
    dependency-admission decision).
  - *Upgrade the existing generator* — **selected.**
- **Decision:** Upgrade the existing `openapi-typescript` devDependency to the 7.x line
  (`7.13.0`). Project-owner authorization of S0.4C authorizes this bounded version remediation,
  subject to the verification gates in this document. No fictional organizational approval is
  claimed; the admission is recorded against the existing dependency per policy.

## 5. Version Selection

- **Old version:** `openapi-typescript@6.7.6`
- **Selected version:** `openapi-typescript@7.13.0`
- **Why selected:** The advisory requires leaving the `5.1.1–6.7.6` range, i.e. moving to the 7.x
  line (a major). npm metadata confirms **every stable 7.x release removed the vulnerable
  `undici@^5.28.4` dependency** (replaced by `@redocly/openapi-core` + `parse-json`; `fast-glob` and
  `js-yaml` also dropped), so any stable 7.x clears **both** the `openapi-typescript` and `undici`
  advisories. Within the 7.x line the choice is governed by **support and stability, not by
  "newer is safer"**: `7.13.0` is the current maintained stable release and the version npm audit's
  fix metadata points to. It was verified to (a) carry no `undici` dependency anywhere in its
  subtree, and (b) satisfy the installed toolchain (`@redocly/openapi-core` requires
  `node>=18.17.0`/`npm>=9.5.0`; the environment is Node `v26.5.0` / npm `11.17.0`).
- **Alternatives rejected:**
  - `6.7.6` (retain) — vulnerable.
  - `7.0.0` (earliest stable 7.x) — clears the advisories identically to `7.13.0` but is an
    unmaintained early point-release of the 7.x rewrite; it offers **no** security advantage and
    would ship an unsupported release. Rejected on support grounds, not security grounds.
  - Replacing the generator — out of scope / new dependency (see §4).

## 6. Dependency Changes

**Manifest (`src/frontend/package.json`)** — a single line, preserving the existing caret
(`^`) version-specification convention:

```diff
-    "openapi-typescript": "^6.7.6",
+    "openapi-typescript": "^7.13.0",
```

**Lockfile (`src/frontend/package-lock.json`)** — full accounting of every direct and
transitive change (computed by diffing the resolved `packages` maps of the committed vs. working
lockfile):

*Version-shifted (3):*

| Package | Old → New | Why | Direct/transitive | Expected consequence of authorized remediation? |
|---|---|---|---|---|
| `openapi-typescript` | `6.7.6 → 7.13.0` | the authorized major fix | direct devDependency | Yes |
| `supports-color` | `9.4.0 → 10.2.2` | `openapi-typescript@7.x` declares `supports-color@^10.2.2`; it is the only consumer of `supports-color` in the tree | transitive | Yes (direct consequence of the major bump) |
| `nanoid` | `3.3.16 → 3.3.18` | targeted `npm update nanoid`; postcss's `^3.3.16` constraint already permits `3.3.18`, so no parent upgrade or override was needed; `3.3.18` is the latest 3.x and clears the `<3.3.18` advisory | transitive (leaf) | Yes |

*Added (17)* — all belong to the new `openapi-typescript@7.x` subtree (root:
`@redocly/openapi-core@1.34.19`):
`@redocly/openapi-core@1.34.19`, `@redocly/ajv@8.11.2`, `@redocly/config@0.22.0`,
`change-case@5.4.4`, `colorette@1.4.0`, `index-to-position@1.2.0`, `js-levenshtein@1.1.6`,
`parse-json@8.3.0`, `pluralize@8.0.0`, `require-from-string@2.0.2`, `type-fest@4.41.0`,
`uri-js-replace@1.0.1`, `yaml-ast-parser@0.0.43`, plus version-conflict nested copies
`@redocly/openapi-core/node_modules/{balanced-match@1.0.2, brace-expansion@2.1.4,
minimatch@5.1.9}` and `@redocly/ajv/node_modules/json-schema-traverse@1.0.0`.

*Removed (18)* — all were `openapi-typescript@6.x`-only dependencies:
`undici@5.29.0` (**the vulnerable package**), `@fastify/busboy@2.1.1` (a dependency of `undici`),
and the `fast-glob` subtree (`fast-glob@3.3.3`, `@nodelib/fs.scandir`, `@nodelib/fs.stat`,
`@nodelib/fs.walk`, `braces@3.0.3`, `micromatch@4.0.8`, `picomatch@2.3.2`, `fill-range@7.1.1`,
`is-number@7.0.0`, `merge2@1.4.1`, `fastq@1.20.1`, `queue-microtask@1.2.3`, `reusify@1.1.0`,
`run-parallel@1.2.0`, `to-regex-range@5.0.1`, `fast-glob/node_modules/glob-parent@5.1.2`).

*Reused unchanged (already present at the same version, deduplicated — no lockfile churn):*
`js-yaml@4.3.1`, `https-proxy-agent@7.0.6`, `agent-base@7.1.4`, `debug@4.4.3`, and the
pre-existing top-level `brace-expansion@5.0.9` / `balanced-match@4.0.4` / `minimatch@10.2.6`.

**No unrelated package was upgraded.** Every added/removed/shifted entry is a direct, explainable
consequence of the `openapi-typescript` 6→7 dependency-set change plus the single targeted `nanoid`
re-resolution. `undici` is confirmed **fully absent** from the resulting lockfile.

## 7. OpenAPI Generation Compatibility

The authoritative pipeline is unchanged: `C# DTOs → (dotnet build) → docs/openapi/Kst.Api.json →
(npm run generate:types, openapi-typescript) → src/frontend/src/generated/api.ts`. S0.4C changes no
C# DTO or endpoint.

| Property | Value |
|---|---|
| OpenAPI input hash (`docs/openapi/Kst.Api.json`, pre **and** post) | `98f1276b318bffbc4ef44d948377fceed741d0ed1e0604a9676c4d6b0beb46ae` (**unchanged** — no OpenAPI contract drift) |
| Pre-upgrade generated hash (`api.ts`, openapi-typescript 6.7.6) | `4eae162f566321cc671654b28e701113474ae6b6d0b3400cd71661bad64c1bdf` |
| Post-upgrade generated hash (`api.ts`, openapi-typescript 7.13.0) | `704173aa6123c01f3cf64defc60c3750f53a032e4d59959ea8444ca82acbc68d` |
| Byte-identical to pre-upgrade? | **No** (Outcome B — generated output differs) |
| Repeatability | **PASS** — a second `npm run generate:types` reproduced hash `704173aa…` exactly (no further diff); the generator is deterministic and converges to a stable checked-in result |

**Pre-remediation baseline (no pre-existing drift):** before any dependency change, running
`generate:types` with the *current* deps (6.7.6) reproduced the committed `api.ts`
byte-identically and left `git status` clean — i.e., the current generator produced the committed
contract from the committed OpenAPI document.

**Nature of the generated diff (Outcome B analysis):** The change is **representational only**, not
a semantic contract change:

- The **`components.schemas.*` block — the DTO field definitions the frontend actually consumes —
  is byte-identical after whitespace/comment normalization** (240 lines in both; zero diff). No
  schema name, field, type, or optionality changed.
- The `paths`/`operations` sections changed representation: 4-space indentation (was 2); explicit
  `parameters` objects and `requestBody?: never` markers added to each operation; a `headers`
  index signature added to responses; all HTTP methods listed with `?: never` for absent ones; and
  a dropped `external` stub. **No operation name, path key, or request/response schema reference
  changed.**
- Frontend consumption is unaffected: the only import of the generated file is
  `import type { components } from '../generated/api'` in `src/frontend/src/api/client.ts`, which
  accesses `components['schemas'][…]` exclusively — it does not reference the `paths`/`operations`
  representation that changed.

Per the workflow, the C# DTOs / committed OpenAPI schema are authoritative and the generated
TypeScript is derived; the new generator produced legitimate, stable, semantically-identical
output, and frontend verification passes (§10), so the regenerated `api.ts` is part of the S0.4C
change set. The generated file was **not** manually edited to hide any generator difference.

## 8. Post-Remediation Dependency Graph

`npm ls openapi-typescript undici nanoid postcss` (after remediation):

```text
kst-frontend@0.1.0-alpha.2
+-- openapi-typescript@7.13.0
`-- vite@6.4.3
  `-- postcss@8.5.25
    `-- nanoid@3.3.18
```

| Package | Resolved version | State vs. S0.3-F001 |
|---|---|---|
| `openapi-typescript` | `7.13.0` | upgraded (out of `5.1.1–6.7.6`); no `undici` child |
| `undici` | **absent** | removed from the graph entirely |
| `nanoid` | `3.3.18` | upgraded (out of `<3.3.18`) |
| `postcss` | `8.5.25` | unchanged (its `nanoid@^3.3.16` range permitted the fix) |

The original vulnerable versions are gone from the active dependency graph.

## 9. Advisory Verification

Post-remediation `npm audit --json` (from `src/frontend`; non-mutating; no fix command run):

- **Total advisories: 0** — npm-reported severity counts `{info: 0, low: 0, moderate: 0, high: 0,
  critical: 0, total: 0}`. Dependency counts `{prod: 6, dev: 322, optional: 52, peer: 8, total:
  327}`.
- **All three original S0.3-F001 advisory conditions are absent:**
  - `openapi-typescript@6.7.6` advisory — gone (now `7.13.0`, outside `5.1.1–6.7.6`).
  - `undici@5.29.0` advisory — gone (package no longer in the graph).
  - `nanoid@3.3.16` advisory — gone (now `3.3.18`, outside `<3.3.18`).

Bounded statement: **npm reported no known advisories for the dependency graph it evaluated at the
time of S0.4C verification.** This is not a statement that the frontend dependencies are secure.

## 10. Regression Verification

Repository-documented frontend commands (`docs/development/BUILD_AND_TEST.md`), run from
`src/frontend`:

| # | Command | Result |
|---|---|---|
| 1 | `npm run generate:types` | **PASS** (exit 0; openapi-typescript 7.13.0; output stable — see §7) |
| 2 | `npm run typecheck` (`tsc --noEmit`) | **PASS** (exit 0) |
| 3 | `npm run lint` (`eslint … --max-warnings 0`) | **PASS** (clean, 0 errors/0 warnings) |
| 4 | `npm test` (`vitest run`) | **PASS** — **281/281 tests, 14 files** (matches the S0.3 baseline count) |
| 5 | `npm run build` (`tsc -b && vite build`) | **PASS** (98 modules transformed) |

Working-tree integrity (`git status --short`) was checked after each step: only the three intended
files (`package.json`, `package-lock.json`, `src/generated/api.ts`) were modified; build output
(`dist/`) is gitignored and did not touch tracked files.

**Not run (with reason):** .NET backend `dotnet test` — no `.cs` file, backend configuration, or
`docs/openapi/Kst.Api.json` changed (verified byte-identical), so no .NET regression is required
solely for a frontend development dependency. `npx @tauri-apps/cli build` (packaged installer) — no
Tauri/Rust/desktop-host dependency changed (see §14); the frontend `npm run build` establishes
frontend packaging compatibility, consistent with the bounded treatment in S0.4B for an equivalent
scope. Production integration — out of S0.4C scope.

## 11. Finding Disposition

```
S0.3-F001 — Resolved
Resolution:
    S0.4C npm Development-Dependency Advisory Remediation, accepted 2026-08-25
```

Justification against the S0.4C acceptance gates (all applicable gates pass):

1. Affected dependency paths confirmed (§3, §8).
2. Dev/runtime reachability classified (§3) — all three development-only; none in the runtime graph.
3. Existing direct-dependency change reviewed under Dependency Admission (§4).
4. Vulnerable `openapi-typescript@6.7.6` removed (§8).
5. Vulnerable `undici@5.29.0` removed (§8 — fully absent).
6. Vulnerable `nanoid@3.3.16` removed (§8 — now `3.3.18`).
7. Post-change `npm audit` no longer reports the original three advisories (§9 — total 0).
8. OpenAPI generator succeeds (§10 #1).
9. Generated output is stable/representational-only, schemas byte-identical, frontend-compatible
   (§7, §10 #2–5).
10. TypeScript typecheck passes (§10 #2).
11. Lint passes (§10 #3).
12. Frontend tests pass — 281/281 (§10 #4).
13. Frontend build passes (§10 #5).
14. No unrelated dependency change (§6, §14).
15. No S0.4B dependency cleanup / `@tauri-apps/plugin-shell` retained (§14).
16. No new package/tool introduced (§14).
17. No Stage 9 work (§14).

The project owner accepted the S0.4C implementation on **2026-08-25**. `S0.3-F001` is therefore
**`Resolved`**: the three original advisory conditions are demonstrably gone from the resolved
dependency graph (`openapi-typescript@6.7.6` and `undici@5.29.0` removed; `nanoid` resolved to
`3.3.18`), `npm audit` reported 0 known advisories for the graph it evaluated, and the OpenAPI
generation workflow plus full frontend verification (typecheck, lint, 281/281 tests, build)
passed. This is `Resolved`, **not** `Accepted Risk`.

## 12. New Findings

**None.** The post-remediation `npm audit` reported **0** advisories, so no new advisory appeared
during S0.4C verification. No `S0.4C-Fxxx` finding was created.

## 13. Residual Limitations

- **Advisory database is time-dependent.** `npm audit` reflects the npm advisory database at the
  time of S0.4C verification; a future re-run could report new advisories. The bounded "no known
  advisories" wording applies.
- **Dev-only exposure differs from runtime exposure.** All three remediated packages are
  development/build-time only; none is in the shipped application's runtime dependency graph. That
  reduces practical exposure but is not itself the basis of the disposition — the advisories were
  remediated, not dismissed.
- **Major generator migration assumptions.** `openapi-typescript` 7.x is a rewrite
  (`@redocly/openapi-core` backend). Compatibility was verified for KST's actual usage (local
  checked-in schema, `components.schemas` consumption, CLI `-o` output). KST does **not** exercise
  the 7.x remote-schema fetch path, so that path's behavior is not verified by this work.
- **Reachability nuance for `undici`.** The vulnerable `undici` network functionality was never
  exercised by KST's local-file `generate:types` invocation; it was remediated by removing the
  package from the graph (via the generator major), not by a confirmed exploitability analysis.

## 14. Non-Work

Confirmed no out-of-scope work occurred in S0.4C:

- **No QAD changes**; no `keytronicshortage` change (both verified unchanged).
- **No Tauri capability changes**; no Rust dependency changes
  (`src/tauri/capabilities/default.json`, `src/tauri/src/lib.rs`, `src/tauri/Cargo.toml`,
  `src/tauri/Cargo.lock` all unchanged; `src/tauri` working tree clean).
- **No backend / C# source or test changes** (`src/backend` unchanged; `docs/openapi/Kst.Api.json`
  byte-identical).
- **No new direct dependency added** — `openapi-typescript` is an existing direct devDependency;
  only its version changed. No scanner, shim, replacement generator, or extra build framework was
  added.
- **No security tool installed.**
- **No `npm audit fix`**, **no `npm audit fix --force`**, **no broad `npm update`**. The only
  update command was a **targeted single-package** `npm update nanoid` (a leaf node), after a
  `--dry-run` confirmed it changes exactly one package.
- **`@tauri-apps/plugin-shell` not removed** — `S0.4B-F001` (Informational) remains unchanged; the
  dependency is still present at `^2.2.0`.
- **Accepted evidence snapshots unchanged:** `docs/security/SECURITY_BASELINE.md`,
  `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md`,
  `docs/security/S0_4A_QAD_SQL_TRANSPORT_REMEDIATION.md`,
  `docs/security/S0_4B_TAURI_SHELL_CAPABILITY_REMEDIATION.md` (and other accepted S0.1/S0.2/S0.3
  documents) were not edited.
- **No S0.5+ work. No Stage 9 work.**
