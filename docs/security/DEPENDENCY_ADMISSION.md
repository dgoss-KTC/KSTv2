# Dependency Admission

**Status:** Enacted / Accepted — 2026-08-21

This policy defines admission rules for third-party dependencies, both application and
development. See `docs/security/SECURITY_ASSURANCE_POLICY.md` for the cross-cutting principles this
document elaborates, and `docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md` for the
licensing and commercial-use admission gate this document incorporates (see "Licensing and
Commercial-Use Gate" below).

## What Counts as a Dependency

At minimum, a dependency includes:

- a NuGet package;
- an npm package;
- a Cargo crate;
- a Python package;
- an executable tool;
- a downloaded binary;
- a Git-hosted dependency;
- a build tool;
- a development dependency;
- a generated-code tool;
- a package-manager plugin;
- an agent extension/package/plugin/skill/MCP server, when it introduces executable capability.

This applies even when the dependency is used only during development, build, or test.

## General Rule

No AI coding agent may silently introduce a new third-party executable dependency.

## Dependency Preference Order

When solving a problem, prefer, in order:

1. existing project functionality;
2. an existing approved dependency;
3. standard library / platform capability;
4. an established third-party dependency;
5. a new, obscure, or specialized dependency.

A new package must not be introduced merely because it saves a small amount of code. This is not a
prohibition on dependencies — it is a requirement that dependency trust be deliberate.

## Dependency Proposal Requirements

Before adding a new direct dependency, disclose:

- package/tool name;
- version or version range;
- ecosystem;
- purpose;
- whether it is runtime, build, test, or development-only;
- why existing capabilities are insufficient;
- known alternatives, where relevant.

## Admission Evidence

As security tooling matures, admission evaluation should consider:

- known security advisories;
- known malicious-package reports;
- package provenance;
- upstream project activity;
- maintainer information where useful;
- recent ownership or publishing anomalies;
- package age and release history;
- install/build scripts;
- dependency-tree impact;
- lockfile changes;
- upstream security posture;
- licensing and commercial-use status (see "Licensing and Commercial-Use Gate" below).

Not all of this evidence is currently automated. The policy exists before every enforcement
mechanism exists — see `docs/security/SECURITY_ASSURANCE_POLICY.md` §"Intentionally Unresolved
Policy Areas".

## Licensing and Commercial-Use Gate

Before a dependency reaches the **Accept** decision state below, its licensing and commercial-use
status must also be established, per
`docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md` (Enacted / Accepted — 2026-08-27).
This licensing gate **supplements** the security/supply-chain evidence above; it does not replace
it — a dependency must satisfy both dimensions to reach **Accept**.

At minimum, admission evidence must record:

- exact version being admitted;
- authoritative license/terms source for that exact version (not merely a registry summary or
  README badge);
- intended-use classification (developer-only, build, runtime/distributed, or hosted
  service — see the licensing policy §3);
- commercial/business-use status under that license;
- redistribution status, where applicable;
- attribution/notice obligations, where applicable;
- any copyleft, network-copyleft, custom/source-available, or proprietary-license trigger;
- any private-repository or field-of-use restriction;
- cost/seat/subscription status;
- transitive-license considerations, where relevant (see the licensing policy §13).

**Unresolved or ambiguous licensing/commercial terms are not treated as permission.** If any of the
above cannot be established with authoritative evidence, admission does not proceed to **Accept**;
it is held pending human review (see the licensing policy §9 for the normal permissive-license
path and §10 for cases that require explicit human/organizational escalation, such as copyleft,
proprietary terms, paid licenses, or missing/unclear licensing).

**AI agents cannot accept legal or commercial risk on the company's behalf.** An agent may identify
license/terms evidence, summarize stated restrictions, and flag ambiguity or cost exposure, but may
not decide that ambiguous terms are acceptable, agree to paid/commercial terms, or authorize a
purchase or subscription. See the licensing policy §17 for the full AI-authority boundary.

## Decision States

- **Accept** — the dependency may be added.
- **Review / Hold** — admission is undecided pending additional evidence or human review.
- **Block** — the dependency must not be added.

These are process outcomes, not numeric risk scores. No numeric severity threshold is defined by
this policy.

## Transitive Dependencies

Direct dependencies alone are insufficient for security inventory. Lockfiles and resolved dependency
trees are security-relevant artifacts and must be retained. KST currently uses at least three
dependency ecosystems: NuGet, npm, and Cargo. Inventorying their actual resolved trees is an
**S0.2 — Baseline Discovery** activity, not performed here. Transitive dependencies also carry
licensing exposure distinct from their direct parent's license; see
`docs/governance/THIRD_PARTY_SOFTWARE_AND_LICENSING_POLICY.md` §13.

## Security Advisory Handling

A known-affected dependency must not be dismissed merely because:

- the vulnerable functionality is believed unreachable;
- it is a transitive dependency;
- the application is internal;
- endpoint protection exists;
- replacement is difficult.

The actual exposure may ultimately be judged acceptable, but the disposition must be explicit and
evidence-backed, not silently assumed.

## Development-Tool Admission

Agent extensions, packages, plugins, skills, and MCP servers that introduce executable capability
are dependencies under this policy and follow the same admission rule: no silent introduction, no
autonomous installation by an AI agent. See
`docs/security/DEVELOPMENT_ENVIRONMENT_SECURITY.md` §"Installation and Activation".
