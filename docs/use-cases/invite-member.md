# Use Case: InviteMember

## Summary
Allows org owner or admin to invite a user to join the organization with a specific role.

## Actors
- **Inviter** (owner or admin): User with permission to invite members
- **Invitee**: User being added to org

## Preconditions
- Inviter must be authenticated
- Inviter must be owner or admin of the organization
- Target org must exist
- Invitee user must exist (by email)

## Input
- `OrgId` (Guid, from URL): Organization ID
- `Email` (string, required): Email of user to invite
- `Role` (OrgRole, required): Role to assign (owner, admin, member)

## Business Rules
- Only owner or admin can invite members
- Cannot invite user who is already a member (idempotent: no-op if already member)
- Cannot create additional owners (only one owner via ownership transfer)
- Email must be valid and match existing user
- Member uniqueness: (OrgId, UserId) composite key

## Process Flow
1. Validate inviter has admin or owner role on org
2. Look up user by email
3. If user not found → return 404
4. Check if user already member of org
5. If already member → return 204 (idempotent)
6. Validate role: if role is "owner" → reject (only one owner)
7. Create Member entity: (OrgId, UserId, Role)
8. Persist member to database
9. Create ACL: grant member view capability on org resource (base permission)
10. Emit `MemberInvited` domain event
11. Return 204 No Content

## Postconditions
- User is member of org with specified role
- ACL created for member
- Member can access org-scoped resources per role

## Output
- **204 No Content** on success

## Error Scenarios
- **NotFound** (404): Org doesn't exist or user email not found
- **Forbidden** (403): Inviter is not owner/admin
- **Unauthorized** (401): Inviter not authenticated
- **Conflict** (409): Attempting to create second owner
- **BadRequest** (400): Invalid email format or role

## Testing
- **Happy path**: Admin invites member → member added
- **Authorization**: Member invites → 403 error
- **Idempotency**: Invite existing member → 204 no change
- **Owner restriction**: Invite as owner → 409 error
- **Not found**: Invalid email → 404 error
- **Integration**: Verify member and ACL persisted

## Related Use Cases
- CreateOrg: Org must exist before inviting members
- TransferOwnership: Change org owner (future enhancement)
