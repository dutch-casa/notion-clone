# Use Case: CreateOrg

## Summary
Allows a user to create a new organization. The creator automatically becomes the owner.

## Actors
- **User** (authenticated): The user creating the organization

## Preconditions
- User must be authenticated
- User has valid JWT token

## Input
- `Name` (string, required): Organization name
- `OwnerId` (Guid, from auth context): ID of user creating the org

## Business Rules
- Organization name cannot be empty or whitespace
- Owner is automatically added as member with role "owner"
- Resource and ACL are automatically created for org-level permissions
- Owner gets admin capability on org resource

## Process Flow
1. Validate input: name is not empty
2. Create Org aggregate with name and ownerId
3. Add owner as Member with role "owner"
4. Persist Org to database (generates ID)
5. Create Resource entry for org (type: org)
6. Create ACL: grant admin capability to owner on org resource
7. Emit `OrgCreated` domain event
8. Return OrgDto with org ID and name

## Postconditions
- Org exists in database
- Owner is member with role "owner"
- Resource and ACL entries created
- Owner can invite other members

## Output
```json
{
  "id": "uuid",
  "name": "My Organization",
  "ownerId": "uuid",
  "createdAt": "2025-01-15T10:30:00Z"
}
```

## Error Scenarios
- **ValidationError** (400): Name is empty or whitespace
- **Unauthorized** (401): User not authenticated
- **InternalError** (500): Database or domain service failure

## Testing
- **Happy path**: Valid name → org created with owner
- **Validation**: Empty name → 400 error
- **Authorization**: Unauthenticated → 401 error
- **Idempotency**: Same name allowed (no uniqueness constraint)
- **Integration**: Verify resource and ACL created

## Related Use Cases
- InviteMember: Add members after org creation
- CreatePage: Org required to create pages
