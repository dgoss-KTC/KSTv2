# S0.6 — Security Tool Admission: Capability Review 3 — Software Bill of Materials (SBOM)

**S0.6 Capability Review 3 — Software Bill of Materials (SBOM)**
**Status: OWNER DECISION RECORDED / SYFT v1.51.1 ADMITTED FOR INSTALLATION AND VERIFICATION / IMPLEMENTATION PENDING**

| Item | Value |
|---|---|
| Gap | `S0.3-G008` |
| Tool | Anchore Syft v1.51.1 |
| Owner admission decision | ADMITTED for installation and verification — 2026-08-27 |
| Implementation | PENDING |
| Project-owner acceptance | Not yet applicable |
| Overall S0.6 status | **IN PROGRESS** (this review closes one capability only; S0.6 as a whole is **not** complete; `G006` remains NOT STARTED) |
| Research evidence | `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md` |

This document is **evidence, not normative policy**. It records the S0.6 Capability Review 3
owner admission decision and (as implementation proceeds) installation, verification, and scan
evidence for the SBOM capability (accepted S0.3 gap `S0.3-G008`). Required security properties and
tool-admission governance remain defined by `SECURITY.md`,
`docs/security/SECURITY_ASSURANCE_POLICY.md`, and `docs/security/DEPENDENCY_ADMISSION.md`. This
document is separate from, and does not modify, the neutral research packet at
`docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`.

---

## 1. Purpose and Status

S0.6 evaluates missing security-tool capabilities **one at a time** under the enacted
dependency-admission process (`docs/security/DEPENDENCY_ADMISSION.md`), per the accepted
remaining-S0 plan (`docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md` §8).

Capability Review 3 addresses:

> **S0.3-G008** — no SBOM generator exists in the toolchain; no SBOM output format has been
> adopted as policy (accepted S0.3 evidence).

Capability Review 1 (Rust dependency advisories, `S0.3-G001`) and Capability Review 2 (dedicated
secret scanning, `S0.3-G007`) are separately COMPLETE / ACCEPTED — see
`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md` and
`docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`. This document does not modify that evidence.

## 2. Governing Scope

- Canonical remaining-S0 plan: `docs/implementation/KST_v2_S0_REMAINING_SECURITY_WORK_PLAN.md`
  (§8 — S0.6 Security Tool Admission).
- Enacted policy: `SECURITY.md`, `docs/security/SECURITY_ASSURANCE_POLICY.md`,
  `docs/security/DEPENDENCY_ADMISSION.md`, `AGENTS.md` (§8 security requirements).
- Research packet consulted (unmodified by this document):
  `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`. That packet made **no tool recommendation and
  no admission decision**; this document records the human admission decision and subsequent
  implementation evidence separately, preserving that boundary.

## 3. Starting State

- **Session provenance:** an earlier pass in this session correctly discovered that no SBOM
  research artifact actually existed in the repository (despite an initiating prompt assuming one
  did), and — rather than fabricate it — produced a genuine neutral research packet
  (`docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`) and stopped without installing anything. That
  packet remained uncommitted at the start of this pass.
- **Commit:** `2579368fecca4c85b6fa4a757d62a2fa157b60d7` (`docs: accept secret scanning
  capability`); `HEAD == origin/main` at the start of this pass.
- **Working tree at start of this pass:** the single untracked path
  `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`, no other changes — as expected.
- **Accepted security state:** S0.1–S0.5 COMPLETE / ACCEPTED; S0.6 Capability Review 1 and
  Capability Review 2 COMPLETE / ACCEPTED; S0.6 Capability Review 3 (this document) research
  complete, owner decision now recorded; `G006` NOT STARTED; S0.7/S0.8 NOT STARTED; Stage 9
  blocked pending S0 closeout.
- **Machine state (at owner-decision time):** no SBOM generator installed on the workstation
  (confirmed in the accepted S0.3 tool-availability pass and the Capability Review 3 research
  packet).

## 4. Owner Admission Decision

The project owner and independent reviewer reviewed the Capability Review 3 research and the
project owner made the following explicit human decision on 2026-08-27:

### 4.1 Anchore Syft v1.51.1 — ADMITTED

> **Anchore Syft v1.51.1 ADMITTED for installation and verification — 2026-08-27.**
>
> Purpose: local generation of KST Software Bills of Materials using repository/build dependency
> evidence, with complementary packaged-artifact inspection.

### 4.2 Microsoft sbom-tool v4.1.5 — DEFERRED

> Credible capability, but independent review identified current KST-relevant
> compatibility/correctness and maintenance uncertainties.

This is not a rejection.

### 4.3 CycloneDX ecosystem-native approach — DEFERRED

> Credible approach (`cyclonedx-dotnet` 6.2.0, `cyclonedx-npm` 6.0.1, `cargo-cyclonedx` 0.5.9),
> but it introduces three tool admissions, three maintenance/supply-chain surfaces, and an
> aggregation strategy. Retained as a fallback if Syft cannot meet KST's empirically verified
> coverage requirements.

None of the deferred candidates are rejected; they remain valid future candidates.

## 5. Accepted SBOM Model

### 5.1 Build/repository evidence view

Purpose: npm dependency inventory, Cargo dependency inventory, NuGet/.NET dependency inventory,
first-party/build context. This is the primary, most-complete view.

### 5.2 Packaged-artifact view

Purpose: shipped files, shipped binaries, native/bundled material, `Kst.Api` sidecar presence.
Complementary to, not a replacement for, the build/repository view.

The owner-approved model explicitly does **not** claim that the final executable alone
reconstructs the complete dependency graph — this must be measured empirically during
implementation, not assumed.

## 6. Admitted Operating Boundary

The admitted capability is **local SBOM generation** from KST build/repository evidence, with a
complementary packaged-artifact scan.

The admitted capability is explicitly **not**:

- vulnerability scanning (Syft has none built in; pairs with the separate tool Grype, which is
  **not admitted**);
- SBOM publication, upload, or signing;
- a permanent choice of SPDX vs. CycloneDX as KST policy;
- CI integration or a release gate;
- a claim that every lockfile component ships on Windows.

## 7. Output Formats for Verification

Implementation verification will attempt both:

```text
SPDX 2.3 JSON
CycloneDX 1.6 JSON
```

if installed Syft v1.51.1 supports those exact versions/formats — the installed CLI will be used
as syntax authority; format flags will not be guessed in advance. This checkpoint does not choose
a permanent organizational SBOM standard.

## 8. Next Step

Installation, release-integrity verification, build-evidence collection, main and
packaged-artifact scans, ecosystem coverage verification, and all remaining implementation
evidence will be recorded as a subsequent update to this document (§9 onward), following the
governing task's installation-authorization boundary.
