# API Contract Workflow

## Source of Truth

**C# DTOs in `Kst.Api` are the authoritative source.**  
TypeScript types are generated from them — never edited manually.

## Generation Pipeline

```
C# DTOs (Kst.Api/Dtos/)
    │
    ▼ dotnet build (Microsoft.Extensions.ApiDescription.Server)
OpenAPI Spec (docs/openapi/Kst.Api.json)
    │
    ▼ npm run generate:types (openapi-typescript)
TypeScript types (src/frontend/src/generated/api.ts)
    │
    ▼ ApiClient (src/frontend/src/api/client.ts)
    │
    ▼ React components
```

## Regenerating Types

Whenever the C# API changes:

```powershell
# 1. Rebuild the backend (auto-generates openapi spec)
cd src/backend
dotnet build Kst.slnx

# 2. Regenerate TypeScript types
cd ../frontend
npm run generate:types

# 3. Fix any TypeScript compile errors in the frontend
npm run typecheck
```

## Rules

- **Never** manually edit `src/frontend/src/generated/api.ts`.
- **Always** regenerate after changing DTOs, adding endpoints, or modifying response shapes.
- The `generate:types` script is defined in `src/frontend/package.json`.
- The OpenAPI spec is committed to `docs/openapi/Kst.Api.json` and regenerated on each build.

## Adding a New Endpoint

1. Add or update C# DTO records under `Kst.Api/Dtos/` (for example `ApiDtos.cs` or feature-specific DTO files such as `WorkspaceDtos.cs`).
2. Add the endpoint in `Kst.Api/Endpoints/`.
3. Run `dotnet build` — spec regenerates automatically.
4. Run `npm run generate:types` from `src/frontend/`.
5. The new types appear in `src/generated/api.ts` and are ready to use.

## Stage-Specific Endpoint Notes

The sections below record notable contract milestones/examples from when each stage's endpoints
were added. They are **not** an authoritative, exhaustive current API inventory — later stages
(for example Stage 5B, Stage 7, and Stage 8) added further endpoint groups that are not repeated
here. The generated OpenAPI document (`docs/openapi/Kst.Api.json`) and `src/frontend/src/generated/api.ts`
are the current, authoritative contract surface; `docs/architecture/BACKEND_PROJECT_BOUNDARIES.md`
documents current endpoint-group ownership at the architecture level.

## Stage 4 Workspace Endpoints (Example)

- `GET /api/v1/workspaces`
    - Returns saved workspace assignments and an optional nonfatal configuration warning.
- `POST /api/v1/workspaces`
    - Accepts workspace configuration input and returns created assignment.
    - Validation failures return Problem Details (`400`).
- `PUT /api/v1/workspaces/{assignmentId}`
    - Accepts the same workspace configuration input as create and returns the updated assignment.
    - Preserves `AssignmentId`, `SortOrder`, and `IsEnabled` regardless of submitted values.
    - Validation failures return Problem Details (`400`); unknown IDs return `404`.
- `POST /api/v1/workspaces/{assignmentId}/archive`
    - Sets `IsEnabled=false` on the assignment and returns the updated assignment.
    - Unknown IDs return `404`.
- `POST /api/v1/workspaces/{assignmentId}/restore`
    - Sets `IsEnabled=true` on the assignment and returns the updated assignment.
    - Unknown IDs return `404`.
- `DELETE /api/v1/workspaces/{assignmentId}`
    - Permanently removes the assignment. Returns `204` on success, `404` if the ID is unknown.
- `DELETE /api/v1/workspaces`
    - Permanently removes all workspace assignments. Idempotent — returns `204` even when already empty.

## Stage 6 Part Detail Endpoint (Example)

- `GET /api/v1/workspaces/{assignmentId}/part-detail?partNumber={partNumber}`
    - Returns QAD-sourced Part Info (planner, lead/safety time, status code+description, revision, description, IOS code, safety stock, on-hand/non-net quantity, MOQ/price tiers) for a single parent part already resolved into the workspace's currently-loaded MPS scope.
    - Never triggers an MPS auto-load — reads the existing MPS snapshot state only.
    - Served from an in-memory cache keyed to the MPS snapshot it was loaded against; a fresh MPS refresh invalidates the cache for that workspace, while a *failed* MPS refresh leaves the cache untouched.
    - `200` — loaded (body includes `isStale`/`warning` when serving a last-known-good cached value after a failed live refresh attempt).
    - `400` — blank/missing `partNumber`.
    - `404` — unknown workspace, part not in the workspace's resolved MPS scope, or no matching QAD `pt_mstr` record (the latter case's Problem Details `title` is exactly `"Part not found"`, used by the frontend to distinguish it from other 404s).
    - `409` — the workspace's MPS data has not been loaded yet.
    - `503` — QAD is unavailable and no cached value exists to fall back to.

