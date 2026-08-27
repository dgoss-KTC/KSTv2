# Third-Party Software & Licensing Governance Policy

**Status: ENACTED / ACCEPTED — 2026-08-27**

This document is enacted, owner-accepted, normative repository policy, consistent with the
`AGENTS.md` §1 Documentation Authority Tiers model (Tier 1 — Enacted repository rules, alongside
`AGENTS.md` itself and any future enacted security policy). It binds AI-agent and developer action
regarding third-party software, tools, packages, and services from this effective date forward.

This document is **not legal advice** and does not itself resolve any specific license's legal
meaning for KST or the company. It establishes a governance **process**: what evidence must be
gathered, what triggers human/organizational review, and what an AI agent may and may not decide
on its own. It does not invent company legal, procurement, or executive authority that repository
evidence does not establish — see §17 and §20.

Nothing in this document authorizes installing, executing, admitting, purchasing, subscribing to,
or accepting terms for any third-party software, service, or tool. Enactment of this policy is not
itself an admission decision for any component, tool, or service.

| Item | Value |
|---|---|
| Draft date | 2026-08-27 |
| Effective / enacted date | 2026-08-27 |
| Drafting session starting commit | `464cde0c33c4d33dfb9142098c9a46c719614d32` (subject: "docs: record SAST capability research") |
| Enactment authority | Project owner review and acceptance (this session) |
| Governing scope | Third-party software, tools, packages, and services used or considered for use anywhere in the KST v2 development, build, and runtime lifecycle |
| Relationship to other enacted policy | Supplements, and does not replace, `docs/security/SECURITY_ASSURANCE_POLICY.md`, `docs/security/DEPENDENCY_ADMISSION.md`, `docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`, and `docs/security/AI_SECURITY_REVIEW.md`. See §2 and §17. |

---

## 1. Purpose

AI-assisted development can introduce technically useful third-party software without adequately
considering license terms, commercial-use restrictions, redistribution obligations, copyleft
obligations, attribution requirements, paid-license/seat/subscription costs, private-repository
restrictions, field-of-use restrictions, source-disclosure obligations, terms of service, or
transitive-license exposure.

This is **not purely a cybersecurity concern**. It is a distinct governance dimension —
**Third-Party Software / Licensing / Commercial-Use Governance** — with a deliberate integration
point into security, dependency admission, developer-tooling admission, AI-agent rules, and
software supply-chain governance generally.

The policy exists to help prevent both:

- unintended legal/compliance obligations; and
- unplanned company costs.

## 2. Relationship to Security

Security and licensing/commercial governance ask different questions about the same third-party
component:

- **Security** asks whether third-party software is trustworthy and safe to obtain, execute,
  integrate, and maintain.
- **Licensing/commercial governance** asks whether KST/the company is *permitted* to use, modify,
  bundle, redistribute, deploy, or access that software under the intended use model, and what
  obligations or costs result from doing so.

Dependency/tool admission must ultimately satisfy **both** dimensions:

- A component can be technically secure yet unacceptable under its license or commercial terms.
- A component can be permissively licensed yet unacceptable for security reasons.

Neither dimension replaces the other. This policy governs the licensing/commercial dimension. It
does not redefine licensing as a vulnerability class, and it does not alter or supersede
`docs/security/SECURITY_ASSURANCE_POLICY.md`, `docs/security/DEPENDENCY_ADMISSION.md`, or any other
enacted security document.

## 3. Software Classes

At least the following classes are distinguished, because the relevant licensing/commercial
concerns differ materially between them:

### A. Developer-only tooling

Conceptually includes: security scanners, IDE extensions, code generators, developer utilities,
local agents.

Relevant concerns: commercial/business-use permission; seat/user restrictions; subscription
requirements; private-repository restrictions; terms of service; telemetry/data handling; tool
distribution rights where applicable.

### B. Build tooling

Conceptually includes: compilers, packagers, generators, build-time utilities.

Relevant concerns: commercial-use rights; whether generated output carries obligations;
redistribution; toolchain licensing; build-server/CI licensing.

### C. Runtime / distributed dependencies

Examples: npm packages, NuGet packages, Rust crates, native libraries, bundled executables.

This class receives the **highest redistribution scrutiny**, because KST ships/bundles these
artifacts as part of the running application.

Relevant concerns: redistribution rights; notices; attribution; source obligations; copyleft;
linked-library obligations; license compatibility.

### D. External services / hosted tools / APIs

Relevant concerns: subscription cost; usage-based cost; user/seat licensing; terms of service;
data-processing terms; source/code upload; commercial-use restrictions; service
termination/vendor dependency.

This policy does not assert that KST currently uses any external service beyond what repository
evidence establishes (e.g., the already-admitted local developer-security tools researched under
S0.6). Class D is defined for completeness and future use, not because repository evidence
currently shows an admitted hosted service. This policy also does not hard-code a single permanent
KST distribution model (internal-only, internally distributed, or externally/customer distributed);
see §4's classification field and §11's re-review trigger for how a change in that model is
handled.

## 4. Required Admission Record

Any newly introduced third-party component/tool intended for anything beyond momentary,
non-repository-affecting exploration should have a licensing/commercial review record containing at
least the following fields. Every field must be completable with either a substantive answer, **Not
Applicable**, or **Unable to Verify** — no field may be silently omitted.

| Field | Notes |
|---|---|
| Component/tool name | |
| Exact version | Per §5, licensing is evaluated against the exact admitted version |
| Official source | e.g., official repository, package registry, vendor site |
| Publisher/maintainer | |
| License identifier/name | e.g., SPDX identifier where one exists |
| Authoritative license/terms source | Direct citation (file, URL, or document), not a secondhand summary |
| Intended KST use category | See §3 classes (A–D) |
| Developer-only / build / runtime / distributed / service classification | May be more than one if the component serves multiple roles |
| Commercial/business use permitted? | |
| Redistribution involved? | |
| Redistribution permitted? | |
| Attribution/notice requirement? | |
| Source/disclosure obligation? | |
| Copyleft characteristic? | |
| Network-copyleft characteristic? | |
| Custom/source-available/proprietary restrictions? | |
| Private-repository restriction? | Directly relevant given KST is a private repository |
| Field-of-use restriction? | e.g., "academic use only", "non-production use only" |
| Seat/user requirement? | |
| Subscription/payment requirement? | |
| Usage-based cost possibility? | |
| Trial/community-edition restriction? | |
| Transitive-license concern? | See §13 |
| Terms may change independently of version? | See §5 |
| Human review/escalation required? | See §8, §10 |
| Decision/status | See §18 outcome categories |
| Evidence date | Terms and licensing pages change over time; record when evidence was gathered |

## 5. Exact-Version Principle

Licensing must be evaluated against the **exact admitted version** and its authoritative terms,
where terms are version-specific.

Do not assume that any of the following necessarily establishes the terms for the exact
artifact/version being admitted:

- a project's historical license;
- the latest/current license (if the admitted version predates a license change);
- a package-registry summary page;
- a README badge.

Where commercial/service terms exist independently of the software version (e.g., a vendor's
Terms of Service, cloud-product terms, or a Registry-content license that is separate from the
engine's open-source license), the applicable terms/version/date must be recorded separately from
the software version.

## 6. Unknown-License Rule

Unknown, missing, conflicting, or ambiguous licensing is **not** silently treated as permission.

Use **Unable to Verify / Human Review Required** until authoritative evidence resolves the
question.

AI agents may **not** infer permission from any of the following alone:

- public availability;
- GitHub hosting;
- free download;
- source availability;
- package-manager availability;
- "community" branding;
- the apparent absence of a license file.

## 7. Commercial-Cost Rule

"Free to download" does not automatically mean free for commercial use, free for private
repositories, free for teams, free for CI, free for enterprise use, free indefinitely, free of seat
limits, or free of usage charges.

Admission research must identify known: license fees; subscriptions; seat/user requirements; paid
private-repository features; usage-based charges; enterprise-only functionality; trial
limitations.

**AI agents may not authorize purchasing, subscriptions, paid upgrades, or acceptance of commercial
terms on behalf of the company.** Concretely, no AI agent may: purchase software; start a paid
subscription; approve seat licensing; convert a trial to a paid tier; accept paid/commercial terms;
or authorize usage charges. See §17 for the full AI-authority boundary and the "Organizational
authority: TO BE ESTABLISHED" note for who, if anyone, may ultimately approve such items on the
company's behalf.

## 8. License Category Handling

This policy describes general review categories to guide *process*. It does not provide legal
advice and does not claim definitive legal interpretation of any license. Case-specific review is
always required; no license is declared categorically safe or unsafe for KST by this document.

- **Permissive open-source licenses** (examples may include MIT, BSD-family, Apache-2.0) — generally
  lower-friction, but still require a recorded review, especially for attribution/notice
  requirements. See §9 for the normal permissive-license admission path.
- **Weak copyleft** (examples may include LGPL/MPL-style obligations) — requires explicit human
  review before distributed/runtime admission.
- **Strong copyleft** (examples may include GPL-family licenses) — requires explicit human review
  before integration/distribution.
- **Network copyleft** (example may include AGPL-family terms) — requires explicit human review.
- **Source-available/custom licenses** — requires explicit human review.
- **Proprietary/commercial terms** — requires explicit human review.
- **No license / unclear license** — do not admit until resolved.

## 9. Normal Permissive-License Admission Path (Owner Decision)

Components under ordinary permissive licenses, including common MIT, BSD-family, and Apache-2.0
cases, **may use the normal project admission process** (i.e., the technical admission path in
`docs/security/DEPENDENCY_ADMISSION.md`, as supplemented by §17 below) when **all** of the
following facts are established:

- exact component/version identified (§5);
- authoritative license/terms identified (not a registry summary or badge alone);
- commercial/business use permitted under that license;
- intended KST use classification recorded (§3, §4);
- no unresolved private-repository or field-of-use restriction;
- no unexpected paid/seat/subscription requirement;
- distribution/redistribution implications recorded where applicable;
- attribution/notice obligations recorded;
- no material licensing ambiguity remains.

**This is a lower-friction review path, not automatic approval.** A permissive license does not by
itself satisfy admission; the facts above must still be established and recorded. No named license
is stated to be universally safe or obligation-free by this policy — attribution/notice obligations
in particular apply even to the most permissive common licenses.

If any of the above facts cannot be established, the component falls out of this normal path and
into §10 (Escalation Cases) as "ambiguous/unresolved," not into silent normal-path approval.

## 10. Escalation Cases (Owner Decision)

Explicit human organizational review is required before admission for any of the following,
regardless of software class (§3):

- weak copyleft;
- strong copyleft;
- network copyleft;
- source-available/custom licenses;
- proprietary/commercial licenses or terms;
- missing/no license;
- ambiguous/conflicting license evidence;
- private-repository restrictions;
- field-of-use restrictions;
- paid licenses;
- seats/subscriptions;
- usage-based fees;
- trial/community-edition restrictions relevant to the intended use;
- any other term capable of materially binding the company.

**Organizational authority: TO BE ESTABLISHED.** Until company authority is formally identified,
this policy does not invent a Legal, Procurement, IT, executive, or other approving body for these
escalation cases — see §17 and §20.

## 11. License / Use-Model Change Trigger

A previously admitted third-party component, tool, or service must be **re-reviewed** when a
material change occurs, including:

- a version change where licensing/terms may differ from the previously reviewed version;
- a governing-license change;
- a commercial-terms change;
- a previously free capability becoming paid or restricted;
- developer/build-only use becoming runtime/distributed use;
- internal deployment becoming external/customer distribution;
- new hosted/cloud functionality being enabled for a previously local-only tool;
- private-repository/team/enterprise restrictions becoming relevant to the current use (e.g., team
  size or repository visibility changes);
- a material change in transitive-license exposure.

Re-review is proportional to what changed: it does not require an unnecessary full re-review for an
objectively identical artifact and use model when the governing terms are unchanged, unless existing
policy (e.g., a specific escalation condition already flagged) requires it.

## 12. Tooling vs. Distributed Dependency Review Depth

Review depth is proportional to how a component is used, not identical across all classes (§3).

**Developer-only tooling** (Class A) still requires:

- known license/terms;
- commercial/business-use status;
- cost/seat/subscription status;
- account/private-repository restrictions;
- data/service terms where relevant.

Developer-only tooling does **not** automatically require the same redistribution analysis as
shipped runtime software, because it is not itself distributed as part of the KST product.

**Runtime/distributed dependencies** (Class C, and any Class D component whose output or client
library is bundled) require the strongest review, specifically of:

- redistribution rights;
- attribution/notices;
- source/disclosure obligations;
- copyleft effects;
- bundled/transitive components (see the transitive-license field in §4, and §13 below).

**Build tooling** (Class B) is evaluated on **both**:

- the tool's own use terms (as for developer-only tooling); **and**
- any obligations that attach to the generated/distributed output the tool produces.

## 13. Transitive Dependencies

A direct dependency's license does not necessarily establish the complete licensing picture for its
transitive dependency graph. For distributed/runtime software, transitive dependencies should
eventually be reviewable through package-manager metadata, SBOM inventory, and other authoritative
license evidence.

This policy does **not** require, and does not itself perform, a full retrospective
transitive-license audit. Such an audit is recorded as future work (§18).

## 14. SBOM Relationship

**SBOM answers:** what third-party components are present?

**Licensing governance (this policy) answers:** what are we permitted or obligated to do with them
under the intended use?

The two are complementary, not interchangeable. Syft or other SBOM/license-metadata tooling can
assist inventory but is **not** authoritative legal interpretation. Known incomplete license
metadata in SBOM output (a limitation already recorded for KST's admitted Syft capability — see
`docs/security/S0_6_SBOM_ADMISSION.md` and `docs/security/S0_6_SBOM_ADMISSION_RESEARCH.md`, neither
of which is modified by this policy) must not be treated as "no license," "no obligation," or
"permission."

## 15. Notice / Attribution

A distributed dependency may require copyright notices, license text, attribution, NOTICE-file
handling, a source offer, or other distribution material, depending on its license. This policy
requires identification and tracking of such obligations as part of admission review:

- license-text obligations;
- copyright notices;
- attribution;
- NOTICE requirements;
- source/disclosure obligations;
- other distribution-material requirements.

This policy establishes the requirement to recognize when such obligations may apply. It does
**not** create the final KST third-party-notices artifact (format and location remain future work —
see §18).

## 16. AI-Assisted Development

This is a known project risk and receives a dedicated section.

AI coding agents must not add a third-party package/tool merely because it is technically
convenient. Before permanent introduction, an agent must establish or surface:

- identity;
- exact version;
- purpose;
- source;
- license/terms status;
- commercial-use status;
- cost status;
- distribution classification.

An agent **must STOP** rather than silently proceed when material license/use terms are unknown or
restricted.

Temporary exploratory packages/tools must still obey the existing developer-environment security
rules (`docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md`) and should not silently become
repository dependencies.

## 17. Human Authority

Consistent with existing KST security governance:

AI agents **may**:

- identify exact versions;
- locate authoritative license/terms evidence;
- summarize factual stated restrictions/obligations;
- identify cost/use restrictions;
- flag copyleft/custom/proprietary terms;
- identify ambiguity;
- recommend escalation.

AI agents **may not**:

- provide binding legal interpretation;
- infer permission from ambiguity;
- waive licensing obligations;
- accept legal/commercial risk for the company;
- agree to contracts/terms for the company;
- authorize purchases/subscriptions;
- silently change to a differently licensed alternative;
- introduce permanent third-party software without the required admission review;
- accept click-through terms on behalf of the organization;
- decide that ambiguous terms are acceptable;
- override organizational/legal/procurement authority.

The project owner may make KST technical/project admission decisions within the process this policy
establishes. The project owner is **not** stated to be authorized to bind the company contractually,
waive legal obligations, accept licensing/legal risk for the company, or approve company
purchases/subscriptions, unless future company policy explicitly establishes that authority.

No AI agent may purchase software, start a paid subscription, approve seat licensing, convert a
trial to a paid tier, accept paid/commercial terms, or authorize usage charges.

**Organizational authority: TO BE ESTABLISHED.** This policy does not invent a legal department,
procurement department, IT authority, or executive-approval chain, because repository/company
evidence does not currently establish one. See §20 for the open question this leaves for the
project owner.

## 18. Admission Outcomes

Process-oriented, factual outcome categories (not numeric risk scores, and not legal
determinations):

- **Eligible for normal technical admission review** — no material licensing/commercial concern
  identified; proceeds through the existing technical admission path in
  `docs/security/DEPENDENCY_ADMISSION.md` (§21).
- **Human licensing/commercial review required** — a specific concern (copyleft, paid tier,
  seat/subscription requirement, custom/source-available terms, etc.) was identified and requires a
  human decision before admission.
- **Organizational/legal/procurement review required** — the concern exceeds what a project-owner
  technical decision can resolve (see §20 open questions on where this authority sits).
- **Not admitted pending clarification** — evidence is currently insufficient (Unable to Verify)
  and admission does not proceed until resolved.
- **Deferred** — admission is not pursued at this time, without a permanent rejection.

This policy does **not** create an "Accepted Legal Risk" authority for AI agents, and does not
imply that the project owner alone can bind the company to contractual terms unless enacted company
policy establishes that authority (§17, §20).

### Future Retrospective Inventory (not performed by this policy)

A future, bounded work item — **KST Third-Party Software & License Inventory / Reconciliation** —
is identified here but not performed as part of this task. Potential evidence sources: the
admitted Syft SBOM output, `package-lock.json`, `Cargo.lock`, NuGet restore metadata, runtime
packaging inventory, and existing developer security-tool admission records. Potential deliverable:
a management-readable third-party software inventory containing component, version, role, license,
distribution status, attribution obligations, review status, and known cost/commercial
restrictions. This inventory is **not** performed now, and already-admitted tools (cargo-audit,
Gitleaks, Anchore Syft — Capability Reviews 1–3) are **not** reopened, reassessed, or marked
noncompliant by this policy merely because it did not exist when they were admitted; where their
existing accepted evidence already records a license (see
`docs/security/S0_6_RUST_DEPENDENCY_ADMISSION.md`, `docs/security/S0_6_SECRET_SCANNING_ADMISSION.md`,
`docs/security/S0_6_SBOM_ADMISSION.md`), that evidence is noted as existing, not re-adjudicated
here.

## 19. Connection to S0.6 Capability Review 4 (SAST)

`docs/security/S0_6_SAST_ADMISSION_RESEARCH.md` (S0.6 Capability Review 4 — Dedicated SAST) is
already **RESEARCH COMPLETE / AWAITING INDEPENDENT REVIEW / NO TOOL ADMITTED**. That research
surfaced at least one material licensing/use-restriction question — specifically, that the CodeQL
CLI's standalone-use Terms and Conditions restrict ordinary use against a private, non-open-source
codebase (such as KST) absent a paid GitHub Advanced Security entitlement — which demonstrates the
practical need for this governance layer.

This policy does **not** choose a SAST candidate and does **not** modify the SAST research packet.
The independent SAST candidate review (and any eventual SAST admission decision) must apply this
now-enacted licensing governance policy. Semgrep, CodeQL, and DevSkim remain unadjudicated
candidates; the CodeQL research finding recorded above is a candidate fact and a future admission
gate, not a final admission decision, and is not converted into one by this policy.

## 20. Policy Questions for Project-Owner Review

The following questions remain open. Repository evidence does not currently establish answers to
them, and this policy does not invent answers:

1. Who holds final organizational authority for licensing/legal exceptions?
2. Should ordinary permissive-license components use the normal project-owner admission path (§9),
   while copyleft/custom/proprietary cases require organizational escalation (§10) — and if so,
   escalation to whom?
3. What distribution model should licensing review assume for KST: will KST be internal-only,
   internally distributed (e.g., to other company sites/teams), or externally/customer distributed?
   (This policy deliberately does not hard-code one answer — see §3, §4, and §11.)
4. Who approves paid developer tooling/subscriptions?
5. Where should third-party notices eventually be stored/distributed (e.g., a repository
   `NOTICE`/`THIRD_PARTY_NOTICES` file, a packaged-release artifact, or elsewhere)?
6. Should CI/service licensing be governed in this same policy, or in a companion
   procurement/service policy? (See §3.D — this policy does not create that companion policy now.)

## 21. Integration with Existing Enacted Policy

This section records the integration made into already-enacted normative documents as part of
enacting this policy, and confirms what was deliberately **not** changed.

### 21.1 `docs/security/DEPENDENCY_ADMISSION.md`

`docs/security/DEPENDENCY_ADMISSION.md` (Enacted/Accepted — 2026-08-21) has been updated in the same
change as this policy's enactment to add a licensing/commercial-use admission gate that must be
satisfied, alongside its existing security-oriented "Admission Evidence" requirements, before a
dependency reaches the "Accept" decision state. See that document directly for the exact enacted
text. This licensing gate **supplements** the existing security/supply-chain admission
requirements; it does not replace them, and no existing security requirement in that document was
weakened.

### 21.2 `AGENTS.md`

`AGENTS.md` has been updated in the same change to add a narrow, cross-referencing rule set
consistent with §16–§17 above: no permanent third-party dependency/tool without known license/terms
and commercial-use status; mandatory surfacing of paid/subscription/seat, private-repository,
field-of-use, and copyleft/network-copyleft/custom/proprietary characteristics; STOP on
ambiguous/missing licensing; no AI acceptance of paid terms or legal/commercial risk; no inferring
permission from public availability; no silent substitution of a differently licensed
tool/component; and the §11 re-review trigger for material version/use/distribution changes. See
`AGENTS.md` directly for the exact enacted text. No existing security rule was weakened.

### 21.3 `SECURITY.md`

`SECURITY.md` has been updated in the same change to reference this policy as part of third-party
software admission, developer tooling, and supply-chain governance, while preserving the
distinction established in §2 above: licensing/commercial governance is not itself a vulnerability
class. `SECURITY.md` is not expanded to restate this policy's full content.

## 22. Status

**ENACTED / ACCEPTED — 2026-08-27.**

This document is enacted, owner-accepted governance policy (see the header table). It is not legal
advice and does not itself resolve the legal meaning of any specific license for KST or the company.
Amendments to this policy follow the same review discipline as its initial enactment: a proposed
change is drafted, reviewed by the project owner, and only then takes effect. The open questions in
§20 remain unresolved by this enactment and are expected to be addressed in a future amendment once
the project owner (and, where applicable, organizational authority per §17/§20) resolves them.
