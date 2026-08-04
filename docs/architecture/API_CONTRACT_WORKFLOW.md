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

## Stage 4 Workspace Endpoints (Current)

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
