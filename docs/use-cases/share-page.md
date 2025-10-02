# Use Case: SharePage

## Summary
Generates a shareable link with token-based access to a page at a specific permission level.

## Actors
- **Sharer** (authenticated): User creating the share link (must have admin on page)

## Preconditions
- User must be authenticated
- User must have admin permission on page
- Page must exist

## Input
- `PageId` (Guid, from URL): Page to share
- `Capability` (Capability, required): Permission level (view, comment, edit, admin)
- `ExpiresAt` (DateTimeOffset?, optional): Link expiration (null = never expires)

## Business Rules
- Only users with admin capability can create share links
- Token must be cryptographically random (32+ bytes)
- Token hashed before storage (SHA256)
- ExpiresAt, if specified, must be in future
- Cleartext token only returned at creation (never retrievable later)

## Process Flow
1. Validate user has admin permission on page (via AuthorizationService)
2. Validate capability is valid
3. If ExpiresAt specified, validate it's in future
4. Generate cryptographically random token (48 bytes, URL-safe base64)
5. Hash token with SHA256
6. Create ShareLink entity:
   - Id: new Guid
   - ResourceId: pageId
   - Capability: capability
   - TokenHash: hash
   - ExpiresAt: expiresAt
   - CreatedBy: userId
   - CreatedAt: now
7. Persist ShareLink to database
8. Create Resource entry for page if doesn't exist
9. Emit `ShareLinkCreated` domain event
10. Return ShareLinkDto with cleartext token (only time it's visible)

## Postconditions
- ShareLink exists in database
- Token can be used to access page (via separate auth flow)
- Token never shown again after creation

## Output
```json
{
  "id": "uuid",
  "token": "random-url-safe-token-48-bytes",
  "expiresAt": "2025-02-15T10:30:00Z",
  "capability": "view",
  "pageId": "uuid"
}
```

## Error Scenarios
- **Forbidden** (403): User doesn't have admin permission
- **ValidationError** (400): ExpiresAt in past or invalid capability
- **NotFound** (404): Page doesn't exist
- **Unauthorized** (401): User not authenticated

## Testing
- **Happy path**: Valid capability → link created with token
- **Authorization**: Non-admin → 403 error
- **Expiration**: ExpiresAt in past → 400 error
- **Token uniqueness**: Generate many links → all unique tokens
- **Token entropy**: Verify cryptographic randomness (min 256 bits)
- **Hash storage**: Verify cleartext token not stored in DB

## Related Use Cases
- RevokeShare: Delete share link
- ValidateShareToken: Authenticate via share link (not in MVP use cases but implied)
- CreatePage: Page must exist first
