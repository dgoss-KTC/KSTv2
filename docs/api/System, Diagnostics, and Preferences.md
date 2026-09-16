# System, Diagnostics, and Preferences

## Status and Authority

**Implementation status:** Implemented
**Documentation status:** Implemented
**Verification baseline:** `main` at `3e2805d70e5797b37317857c54d1184feda8e12c`

**Mechanical contract authority:** `docs/openapi/Kst.Api.json`

Primary implementation evidence:

- `src/backend/Kst.Api/Endpoints/DiagnosticEndpoints.cs`
- `src/backend/Kst.Api/Endpoints/SystemEndpoints.cs`
- `src/backend/Kst.Api/Endpoints/PreferencesEndpoints.cs`
- `src/backend/Kst.Api/Dtos/ApiDtos.cs`
- `src/backend/Kst.Api/Dtos/PreferencesDtos.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/HealthEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/ReadyEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/SystemStatusEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/SystemRefreshEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/PreferencesEndpointTests.cs`

---

## Purpose

The System capability provides local runtime diagnostics, application/source status, a shell-level refresh operation, and local user preferences.

These endpoints answer questions about the running KST backend and its configured data-source environment.

They do not represent MPS, Work Order, inventory, shortage, or other scheduling-domain data.

---

## Questions Answered

The System APIs answer questions such as:

- Is the backend process alive?
- Has backend initialization completed?
- Which backend instance is running?
- Which application/backend version is running?
- What status is currently reported for configured data sources?
- When was a shell-level refresh last attempted?
- When did a shell-level refresh last succeed?
- What local appearance preferences are configured?

They do not answer:

- Has a particular workspace's MPS loaded?
- Is a particular workspace snapshot current?
- Is a particular work order current?
- Does a part have inventory?
- Is material available for a work order?

Those questions belong to capability-specific APIs.

---

# Diagnostics

## Contract Reference

### Liveness

```text
GET /health
Operation: GetHealth
```

Exact schema: `docs/openapi/Kst.Api.json`

### Readiness

```text
GET /ready
Operation: GetReady
```

Exact schema: `docs/openapi/Kst.Api.json`

---

## Liveness Semantics

`GET /health` reports that the backend process is alive and able to answer the health request.

The current response includes process/application identity information and a timestamp.

Liveness is intentionally a process-level concept.

### Must Not Infer

Do not infer from a successful `/health` response that:

- QAD is reachable;
- the Shortage Database is configured;
- MPS data has loaded;
- any workspace exists;
- any business-data snapshot is current.

A healthy backend can legitimately report unavailable or unconfigured external sources.

---

## Readiness Semantics

`GET /ready` reports backend initialization readiness.

The current contract also exposes the generic system snapshot-availability state used by the original application/status shell.

### Must Not Infer

Do not treat `/ready` as a workspace MPS readiness endpoint.

A ready backend may have:

- no configured workspaces;
- no loaded workspace MPS snapshot;
- an unavailable QAD source;
- no current Part Detail or Work Order data.

Workspace-specific readiness is expressed by the corresponding capability behavior.

---

# System Status

## Contract Reference

```text
GET /api/v1/system/status
Operation: GetSystemStatus
```

Exact schema: `docs/openapi/Kst.Api.json`

---

## Response Grain

One response represents the current local backend instance.

The response is not workspace-scoped.

Current semantic groups include:

- application identity/version;
- backend framework and instance identity;
- process start/current time;
- generic system snapshot state;
- data-source statuses;
- shell-level refresh history.

---

## Business Semantics

### Application and backend identity

The response identifies the running KST application/backend instance.

The version is derived from the application's established versioning mechanism rather than from a separate API-documentation version.

### Data-source status

The response reports registered source names and current source status.

Current registrations include QAD and the Shortage Database integration boundary.

A source may truthfully report states such as not configured or other current connectivity/lifecycle states.

Source status is diagnostic context, not business data.

### Generic snapshot status

The system response retains a generic snapshot/status model established during the application-shell foundation.

This is distinct from the per-workspace MPS snapshot system implemented later.

### Refresh history

The current response can report:

- last refresh attempt;
- last successful refresh.

These timestamps belong to the system refresh/status mechanism.

They are not substitutes for capability-specific freshness metadata.

---

# System Refresh

## Contract Reference

```text
POST /api/v1/system/refresh
Operation: PostSystemRefresh
```

Exact schema: `docs/openapi/Kst.Api.json`

---

## Purpose

The system refresh endpoint runs the registered shell-level refresh/data-source status cycle and returns the resulting system status.

It exists to update application-level source/status information.

---

## Response Grain

The response has the same system-level grain as `GET /api/v1/system/status`.

---

## Critical Distinction: System Refresh vs. MPS Refresh

The system refresh mechanism and the workspace MPS refresh mechanism are separate.

Workspace MPS has its own snapshot store and its own endpoint:

```text
POST /api/v1/workspaces/{assignmentId}/mps/refresh
```

A call to:

```text
POST /api/v1/system/refresh
```

must not be interpreted as proof that any workspace's MPS snapshot was rebuilt.

Likewise, a workspace MPS refresh is not the same thing as updating the generic system snapshot/status model.

### Must Not Infer

Do not infer from a successful system refresh that:

- MPS was reloaded;
- Part Detail caches were invalidated;
- Work Order data was reloaded;
- BOM or Component Detail data was reloaded;
- every registered external source successfully returned business data.

Inspect the returned source/status fields and the capability-specific API when those distinctions matter.

---

# Preferences

## Contract Reference

```text
GET /api/v1/preferences
Operation: GetPreferences

PUT /api/v1/preferences
Operation: UpdatePreferences
```

Exact schemas: `docs/openapi/Kst.Api.json`

---

## Purpose

Preferences are local application configuration used for user presentation choices.

The current contract includes:

- theme;
- accent color;
- row density.

The response may also include a nonfatal configuration warning.

---

## Request Scope and Identity

Preferences are application-local rather than workspace-specific.

They do not identify a QAD site, parent part, work order, or business-data snapshot.

---

## Persistence

Preferences are persisted through the application's local configuration infrastructure.

The frontend consumes the preference API rather than writing the underlying persistence file directly.

---

## Error Behavior

`PUT /api/v1/preferences` returns Problem Details for invalid preference values according to the current contract.

Exact accepted values and mechanical schema should be confirmed from current OpenAPI and preference validation code rather than duplicated here as a second contract definition.

---

## Must Not Infer

Do not infer that:

- a preference change alters source data;
- changing density changes API response grain;
- changing theme or accent changes business status;
- preferences are QAD-backed;
- resetting workspace configuration necessarily resets preferences.

Preferences and workspace configuration are separate application-owned concerns.

---

# Source / Provenance

System and preference information is primarily application-owned rather than QAD business data.

QAD source status represents integration/connectivity state; it is not a QAD business-data payload.

The Shortage Database integration may be unconfigured even while the corresponding integration boundary exists in the repository.

Do not invent source behavior merely because an integration project or data-source status entry exists.

---

# Error and Data-Quality Behavior

The diagnostic and status APIs are intended to report runtime state truthfully.

A source being unavailable or unconfigured must not be transformed into fake successful business data.

Current system/diagnostic endpoints are mechanically defined in OpenAPI; capability-specific source failures are documented in the relevant API capability documents.

---

# Related Capabilities

- `WORKSPACES.md` — local scheduler workspace configuration
- `MPS.md` — per-workspace planning snapshot and explicit MPS refresh
- `PARTS.md` — lazy part details tied to MPS freshness generation
- `API_CONVENTIONS.md` — shared status, Problem Details, freshness, and null/zero conventions

---

# Verification / Evidence

Current implementation and behavior are supported by:

- `src/backend/Kst.Api/Endpoints/DiagnosticEndpoints.cs`
- `src/backend/Kst.Api/Endpoints/SystemEndpoints.cs`
- `src/backend/Kst.Api/Endpoints/PreferencesEndpoints.cs`
- `src/backend/Kst.Api/Dtos/ApiDtos.cs`
- `src/backend/Kst.Api/Dtos/PreferencesDtos.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/HealthEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/ReadyEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/SystemStatusEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/SystemRefreshEndpointTests.cs`
- `src/backend/tests/Kst.Api.IntegrationTests/PreferencesEndpointTests.cs`
- `docs/architecture/TECHNICAL_FOUNDATION.md`
- `docs/architecture/API_CONTRACT_WORKFLOW.md`

---

# Open / Provisional Items

None identified for the currently implemented System/Diagnostics/Preferences contract at this documentation checkpoint.

Future business-data integrations must not be documented as part of System merely because they later appear in source-status reporting.
