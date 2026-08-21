# AI Security Review

**Status:** Enacted / Accepted — 2026-08-21

This policy defines KST v2's model for independent AI security review. See
`docs/security/SECURITY_ASSURANCE_POLICY.md` for the cross-cutting principles this document
elaborates.

## Independence

Routine coding-agent self-review is not equivalent to independent security review. An independent
review should use:

- a separate context;
- a security-specific objective;
- the application's declared security profile
  (`docs/security/APPLICATION_SECURITY_PROFILE.md`);
- the relevant source or diff;
- dependency changes;
- available scanner/test evidence.

Where practical and permitted, a different or specialized model may be used for higher-risk
changes. This policy does not mandate a particular model or vendor.

## Data Handling

Security-model selection depends on both security-review capability and data-handling suitability.
None of the following is assumed by this policy:

- local = safe;
- frontier = automatically permitted;
- open = automatically safe;
- proprietary = automatically prohibited.

Before sending project material to an external AI service for review, apply the same data-handling
consideration required generally: consider whether the material contains credentials, API keys,
passwords, connection strings, production data, customer data, confidential operational information,
internal infrastructure details, proprietary source, schemas, security configuration, or sensitive
logs. Actual secrets must not be intentionally supplied unless explicitly organizationally approved.
Prefer redaction, placeholders, sanitized logs, or structural descriptions where actual values are
unnecessary.

Approved external AI providers remain an organizational decision not finalized by S0.1.

## Finding Evidence and State

AI-generated security findings are not automatically **Confirmed**. They require verification and
evidence before being treated as confirmed. Use the finding evidence requirements and finding states
defined in `docs/security/SECURITY_ASSURANCE_POLICY.md` §§"Security Findings Require Evidence" and
"Security Finding States".

## Review Triggers

Consistent with `docs/security/SECURITY_ASSURANCE_POLICY.md` §"Security-Relevant Change Triggers",
independent AI security review is warranted for changes involving:

- trust-boundary changes;
- networking;
- credentials;
- database access;
- dependencies;
- subprocess execution;
- filesystem privileges;
- external services;
- security-sensitive input handling;
- deployment/security-profile changes.

Mandatory frontier-model review triggers are not defined by this policy — that decision is
explicitly unresolved (see `docs/security/SECURITY_ASSURANCE_POLICY.md` §"Intentionally Unresolved
Policy Areas").

## Risk Acceptance

AI agents cannot accept material security risk on the project's behalf, whether performing routine
implementation or independent review. See `docs/security/SECURITY_ASSURANCE_POLICY.md` §"Risk
Acceptance".
