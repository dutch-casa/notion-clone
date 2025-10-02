# Use Case: RevokeShare

## Summary
Revokes a share link, making it no longer usable for access.

## Actors
- **Revoker** (authenticated): User revoking the share link (must have admin on page)

## Preconditions
- User must be authenticated
- User must have admin permission on page associated with share link
- Share link must exist

## Input
- `ShareId` (Guid, from URL): Share link ID to revoke

## Business Rules
- Only users with admin capability on the page can revoke share links
- Revocation is permanent (cannot un-revoke)
- Token becomes invalid immediately
- Existing sessions using token remain active until next authorization check

## Process Flow
1. Load ShareLink by ID
2. If not found → return 404
3. Load associated page from ResourceId
4. Validate user has admin permission on page (via AuthorizationService)
5. Delete ShareLink from database
6. Emit `ShareLinkRevoked` domain event
7. Return 204 No Content

## Postconditions
- ShareLink deleted from database
- Token no longer grants access
- Future attempts to use token fail

## Output
- **204 No Content** on success

## Error Scenarios
- **NotFound** (404): Share link doesn't exist
- **Forbidden** (403): User doesn't have admin permission on associated page
- **Unauthorized** (401): User not authenticated

## Testing
- **Happy path**: Admin revokes link → deleted
- **Authorization**: Non-admin → 403 error
- **Not found**: Invalid shareId → 404 error
- **Immediate effect**: Use token after revocation → 401 error
- **Idempotency**: Revoke twice → second returns 404

## Related Use Cases
- SharePage: Create share link first
- ValidateShareToken: Token validation fails after revocation
