# TypeScript Type Generation from Backend

This project uses `openapi-typescript` to automatically generate TypeScript types from the backend's OpenAPI specification.

## Setup

The backend exposes its OpenAPI spec at `/swagger/v1/swagger.json` when running in development mode.

## Generating Types

To regenerate types after backend changes:

```bash
# Make sure backend is running on port 5036
bun run generate:types
```

This will create/update `src/types/api.ts` with type-safe interfaces for all API endpoints and DTOs.

## Usage

Import types from the generated file:

```typescript
import type { components } from '@/types/api';

// Use schema types
type RegisterRequest = components['schemas']['RegisterRequestDto'];
type AuthResponse = components['schemas']['AuthResponseDto'];

// Example API call with type safety
export const api = {
  auth: {
    register: (data: RegisterRequest) =>
      fetcher<AuthResponse>('/auth/register', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
  },
};
```

## Benefits

- **Type Safety**: Frontend types automatically match backend DTOs
- **Auto-completion**: Full IntelliSense support for API requests/responses
- **Compile-time Errors**: Catch API contract mismatches before runtime
- **No Manual Typing**: Types stay in sync with backend changes

## When to Regenerate

Run `bun run generate:types` whenever:
- Backend DTO properties change
- New endpoints are added
- Response/request structures are modified
