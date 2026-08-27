# S0.6 — Security Tool Admission: Capability Review 1 — Rust Dependency Advisory Capability

**S0.6 Capability Review 1 — Rust Dependency Advisory Capability**
**Status: COMPLETE / ACCEPTED — 2026-08-26**

| Item | Value |
|---|---|
| Capability | Rust Dependency Advisory Detection |
| Tool | cargo-audit 0.22.2 |
| Owner admission decision | ADMITTED — 2026-08-26 |
| Implementation | COMPLETE |
| Project-owner acceptance | ACCEPTED — 2026-08-26 |
| Overall S0.6 status | **IN PROGRESS** (this review closes one capability only; S0.6 as a whole is **not** complete) |

This document is **evidence, not normative policy**. It records the S0.6 Capability Review 1
admission, implementation, verification, and owner acceptance for the Rust dependency advisory
capability (accepted S0.3 gap `S0.3-G001`). Required security properties and tool-admission
governance remain defined by `SECURITY.md`, `docs/security/SECURITY_ASSURANCE_POLICY.md`, and
`docs/security/DEPENDENCY_ADMISSION.md`. An advisory scan result is a point-in-time observation
against the advisory database evaluated — it is not a security certification and does not
establish that Rust dependencies are secure.

---

## 1. Purpose and Status

S0.6 evaluates missing security-tool capabilities **one at a time** under the enacted
dependency-admission process (`docs/security/DEPENDENCY_ADMISSION.md`), per the accepted
remaining-S0 plan (`docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8).

Capability Review 1 addresses the first measured gap:

> **S0.3-G001** — Rust dependency advisories: no authorized/available Rust advisory scanner
> (`cargo-audit`/`cargo-deny` absent); `Cargo.lock` (480 entries) has no native `cargo`
> advisory-database check (accepted S0.3 evidence,
> `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` §5.3, §11).

**Why native functionality is insufficient:** standard Cargo has no first-party
vulnerability-advisory check. `cargo check`, `cargo clippy`, `cargo tree`, and `cargo metadata`
are compiler/static-correctness and inspection commands, not vulnerability-database checks.
The accepted S0.3 pass confirmed this and recorded the capability absence as `S0.3-G001`.

**Result of the review:** the project owner explicitly admitted **cargo-audit 0.22.2** (exact
pinned version) as developer security tooling. The tool was installed at the pinned version,
executed against `src/tauri/Cargo.lock`, and verified. The owner then **accepted** the
implemented capability on 2026-08-26.

**Status: S0.6 Capability Review 1 — Rust Dependency Advisory Capability —
COMPLETE / ACCEPTED — 2026-08-26.**

## 2. Governing Scope

- Canonical remaining-S0 plan: `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md`
  (§8 — S0.6 Security Tool Admission).
- Enacted policy: `SECURITY.md`, `docs/security/SECURITY_ASSURANCE_POLICY.md`,
  `docs/security/DEPENDENCY_ADMISSION.md`, `AGENTS.md` (§8 security requirements).
- Accepted evidence consulted (unmodified): `docs/security/SECURITY_BASELINE.md` (S0.2),
  `docs/security/S0_3_EXISTING_TOOL_SECURITY_CHECKS.md` (S0.3 — source of `S0.3-G001`),
  `docs/security/S0_5_SECURITY_REGRESSION_ARCHITECTURE_CHECKS.md` (S0.5).
- S0.6 process followed per the accepted plan: define the need → determine native
  sufficiency → evaluate candidates → document trust/supply-chain implications → obtain human
  approval **before** installation → integrate and record the decision.

## 3. Starting State

- **Commit:** `f6d09b914dd88d2f528aca590f314b76730c0f41` (`test: add security architecture
  regression checks`) on branch `main`; local `main` == `origin/main`.
- **Accepted security state:** S0.1–S0.5 COMPLETE / ACCEPTED; S0.6 Security Tool Admission
  started with Capability Review 1 (gap `S0.3-G001`); S0.7/S0.8 PLANNED / NOT STARTED;
  Stage 9 NOT STARTED / blocked pending S0 closeout.
- **Machine state:** no Rust advisory scanner installed on the workstation (confirmed in the
  accepted S0.3 tool-availability pass).

## 4. Tool Evaluation and Owner Decisions

Candidates for the Rust dependency advisory capability are `cargo-audit` and `cargo-deny`
(accepted S0.3 candidate table). The review evaluated them against the actual need
(advisory-database detection over `Cargo.lock`) and the dependency-admission process.

### 4.1 cargo-audit 0.22.2 — ADMITTED / ACCEPTED

- **Scope fit:** detects RustSec vulnerability advisories affecting the `Cargo.lock`
  dependency graph; also reports yanked, unmaintained, and unsound dependency health signals
  as informational categories. This matches the `S0.3-G001` need without adding policy
  surfaces KST has not enacted.
- **Classification:** developer security tooling operated locally by the developer; it is
  **not** an application dependency. It is not added to `Cargo.toml` or `Cargo.lock` and has
  no effect on the application's dependency graph.
- **Owner admission decision:** **ADMITTED — 2026-08-26** (obtained before installation, per
  the dependency-admission process).
- **Exact version pin:** `0.22.2`, pinned by project documentation (this document and the
  canonical status documents). The pin is documentation-level, not lockfile-level, because the
  tool is outside the application dependency graph.

### 4.2 cargo-deny 0.20.2 — DEFERRED

> **cargo-deny 0.20.2 — DEFERRED**
>
> Its broader bans/licenses/sources/dependency-policy surface does not currently correspond
> to an enacted KST requirement. It remains a valid future candidate if such policy
> requirements arise.

cargo-deny was **not** installed, admitted, or rejected; it is deferred.

## 5. Installation Identity

| Item | Value |
|---|---|
| Tool | cargo-audit |
| Exact version | 0.22.2 |
| Installation mechanism | `cargo install cargo-audit --version 0.22.2 --locked` |
| Observed executable | `C:\Users\dgoss\.cargo\bin\cargo-audit.exe` |
| Version reconfirmation | `cargo audit --version` reports `cargo-audit 0.22.2` |
| Lockfile impact | None — `src/tauri/Cargo.lock` and `src/tauri/Cargo.toml` unchanged |

No upgrade, reinstall, alternate version, prebuilt-ZIP switch, or PATH configuration change
occurred. The version remains pinned by project documentation only.

## 6. Implementation: First Audit Run

`cargo-audit` was executed against `src/tauri/Cargo.lock` (the Tauri/desktop Rust dependency
graph). The accepted first-run result:

| Metric | Value |
|---|---|
| Cargo.lock entries evaluated | 480 |
| RustSec vulnerability advisories | 0 |
| Allowed informational warnings | 17 |
| — unmaintained | 16 |
| — unsound | 1 |
| Yanked findings | 0 |

**Bounded statement:** cargo-audit 0.22.2 reported no RustSec vulnerability advisories
affecting the `Cargo.lock` dependency graph it evaluated on 2026-08-26.

This is a point-in-time advisory-database observation. It does **not** establish that Rust
dependencies are secure.

## 7. S0.6-F001 — Informational / Dependency Health Observation

**Disposition: S0.6-F001 — Informational / Dependency Health Observation.**

Accepted description:

> cargo-audit 0.22.2 reported zero vulnerability advisories and 17 allowed informational
> warnings:
>
> - 16 unmaintained dependency advisories;
> - 1 unsound advisory.

The report established:

- **Linux-target-specific GTK dependency path:** includes the `gtk-rs` stack, `glib`, and
  `proc-macro-error` findings;
- **Windows dependency graph:** includes the five `unic-*` unmaintained dependencies.

The `glib 0.18.5` unsound advisory is on the **Linux-target-specific** dependency path
identified during the review, not on the Windows build path.

**Final disposition:** Retain as dependency-health evidence. Reassess during normal
dependency upgrades, future security review, or if platform/reachability conditions
materially change.

No remediation was performed: these dependencies were **not** upgraded; no ignores were
introduced; no `audit.toml` was created; no findings were suppressed; no KST severity was
assigned; and S0.6-F001 is **not** an Accepted Risk.

## 8. RustSec Advisory-Distribution Trust Limitation

The RustSec advisory database is distributed through the public RustSec advisory repository
and associated hosting/access controls.

The admission review established that the RustSec library extracts commit-signature
information but did **not** establish end-to-end signature verification by cargo-audit.

This remains a **documented trust limitation** of the advisory-distribution path — it is
**not** an Accepted Risk, and the advisory-database trust model is not addressed during this
finalization.

## 9. Network and Data-Handling Behavior

Accepted observed behavior:

| Operation | Observed behavior |
|---|---|
| Online audit | Fetches public RustSec advisory metadata; updates the crates.io index for yanked checking |
| Offline / no-fetch audit | Uses the cached advisory database; no fetch/update status observed |
| KST source upload | None intentionally performed |
| Cargo.lock upload | None intentionally performed |
| Database / customer / credential / secret upload | None |

This records accepted observed behavior of the tool's documented operation. It is not a
packet-level proof of all network traffic.

## 10. Structured Output Capability

| Output | Result |
|---|---|
| JSON | Verified valid |
| SARIF | Verified SARIF 2.1.0 |
| CI integration | **NOT IMPLEMENTED** |

CI integration remains intentionally unresolved. No CI configuration was added. Raw audit
JSON/SARIF output is **not** committed to the repository; the bounded summary in §6 is the
accepted record.

## 11. Rust Regression Verification

Accepted implementation verification (executed on the unchanged `src/tauri` Rust workspace):

| Check | Command | Result |
|---|---|---|
| Tests | `cargo test --locked --offline` | PASS — 5/5 |
| Compile check | `cargo check --locked --offline` | PASS |
| Lint | `cargo clippy --locked --offline` | PASS, with 2 pre-existing `needless_return` warnings |

The two Clippy warnings are pre-existing style observations unrelated to this capability.
No source code was modified to eliminate them during finalization.

## 12. S0.3-G001 Disposition — Covered / Resolved

**S0.3-G001 — Covered / Resolved by accepted cargo-audit capability.**

The gap was **Capability Implemented / Awaiting Project-Owner Acceptance** after the
implementation and verification pass, and was promoted to the final disposition above upon
project-owner acceptance on 2026-08-26.

Accepted basis:

- standard Cargo has no first-party vulnerability-advisory check;
- cargo-audit 0.22.2 was explicitly admitted by the project owner;
- exact pinned version installed successfully;
- `Cargo.lock` graph remained unchanged;
- first audit completed successfully;
- JSON output verified;
- SARIF output verified;
- offline/no-fetch operation verified;
- Rust regression checks remained green;
- the tool is developer security tooling rather than an application dependency.

`S0.3-G001` is **not** an Accepted Risk: it is resolved by implementing the missing
capability with an explicitly admitted, pinned, locally operated tool.

**Working principle — what this capability establishes:**

> KST now has an explicitly admitted, pinned, locally operated mechanism for detecting
> RustSec dependency advisories in its `Cargo.lock` graph.

It does **not** establish:

> automatic remediation, dependency-health policy, advisory suppression policy, or general
> Rust dependency security.

Those remain separate human/security decisions.

## 13. Acceptance

- **Project-owner acceptance:** **ACCEPTED — 2026-08-26** (acceptance of the implemented
  cargo-audit 0.22.2 capability, the first audit result, and the recorded dispositions).
- **Final status:** **S0.6 Capability Review 1 — Rust Dependency Advisory Capability —
  COMPLETE / ACCEPTED — 2026-08-26.**

## 14. Finalization and Change Surface

Finalization (recording acceptance) changed documentation status only. Finalization did not
rerun `cargo audit`, `cargo test`, `cargo check`, or `cargo clippy` unless an unexpected
implementation/environment change occurred, and did not trigger an advisory fetch; a
lightweight `cargo audit --version` reconfirmation of the admitted executable is permitted
but not required to establish acceptance.

Across the entire Capability Review 1 (review, implementation, and finalization), the only
repository changes are documentation:

- this evidence document (`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`);
- canonical status updates in `KST-v2-Master-Project-Checklist.md`, `SECURITY.md`, and
  `docs/status/CURRENT_PROJECT_STATUS.md`.

**Not changed / not introduced:** `src/tauri/Cargo.toml`, `src/tauri/Cargo.lock`,
`src/frontend/package.json`, `src/frontend/package-lock.json`, `Directory.Packages.props`,
application source or test code, Tauri capabilities, CI configuration, `audit.toml`,
cargo-deny configuration, SARIF/JSON audit artifacts, Git hooks, or any other security tool.
No dependency was upgraded or added; no suppression or severity policy was created; no
Accepted Risk decision was made. Prior accepted evidence (S0.2, S0.3, S0.4A/B/C, S0.5
documents) remains unchanged.

## 15. Remaining S0.6 Position

| Item | Status |
|---|---|
| S0.6 — Security Tool Admission | **IN PROGRESS** |
| Capability Review 1 — Rust Dependency Advisory Capability (`S0.3-G001`) | **COMPLETE / ACCEPTED — 2026-08-26** |
| cargo-audit 0.22.2 | ADMITTED / ACCEPTED |
| cargo-deny 0.20.2 | DEFERRED |
| S0.3-G001 | Covered / Resolved |
| S0.6-F001 | Informational / Dependency Health Observation |
| S0.3-G006 (dedicated SAST) | NOT STARTED — future capability review |
| S0.3-G007 (dedicated secret scanning) | NOT STARTED — future capability review |
| S0.3-G008 (SBOM) | NOT STARTED — future capability review |
| S0.7 — Runtime & Infrastructure Verification | NOT STARTED |
| S0.8 — Independent Assurance & S0 Closeout | NOT STARTED |
| Stage 9 | NOT STARTED / BLOCKED PENDING S0 CLOSEOUT |

Capability Review 2 was **not** selected or begun as part of this finalization.
