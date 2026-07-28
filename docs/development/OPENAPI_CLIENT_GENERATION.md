# OpenAPI Client Generation

## Overview

- The OpenAPI specification is generated automatically when the .NET backend is built.
- TypeScript types are generated from the spec using `openapi-typescript`.
- **Never manually edit** `src/frontend/src/generated/api.ts`.

## Step 1 — Build the Backend (auto-generates spec)

```powershell
cd src/backend
dotnet build Kst.slnx
```

This uses `Microsoft.Extensions.ApiDescription.Server` to export the spec to:
```
docs/openapi/Kst.Api.json
```

## Step 2 — Generate TypeScript Types

```powershell
cd src/frontend
npm run generate:types
```

This runs:
```
openapi-typescript C:\Dev\kst_v2\docs\openapi\Kst.Api.json -o src/generated/api.ts
```

Output: `src/frontend/src/generated/api.ts`

## Step 3 — Use the Types

The generated file exports TypeScript interfaces matching every C# DTO.

Import in your API client:
```typescript
import type { components } from '../generated/api';

type SystemStatusResponse = components['schemas']['SystemStatusResponse'];
```

## Updating After API Changes

Whenever you add or change a C# endpoint or DTO:

1. `cd src/backend && dotnet build Kst.slnx`
2. `cd src/frontend && npm run generate:types`
3. `npm run typecheck` — fix any TypeScript errors
4. Commit `docs/openapi/Kst.Api.json` and `src/frontend/src/generated/api.ts` together.

## Notes on Single-File Publish

`Microsoft.Extensions.ApiDescription.Server` generates the spec at **build time** by
briefly running the application. This works correctly in the self-contained publish
pipeline because the spec is generated before single-file bundling occurs.

The generated spec file (`docs/openapi/Kst.Api.json`) is checked into source control
so TypeScript types can be regenerated without running the backend.
