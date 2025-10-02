# Use Case: CreatePage

## Summary
Creates a new collaborative document page within an organization.

## Actors
- **User** (authenticated): User creating the page

## Preconditions
- User must be authenticated
- User must be member of the organization
- Target org must exist

## Input
- `OrgId` (Guid, from URL): Parent organization
- `Title` (string, required): Page title

## Business Rules
- Title cannot be empty or whitespace
- User must have edit permission on org (member or higher)
- Page automatically gets empty CRDT document (YDoc)
- Resource and ACL created for page-level permissions
- Creator gets admin capability on page resource

## Process Flow
1. Validate user is member of org (via AuthorizationService)
2. Validate title is not empty
3. Create Page aggregate: (OrgId, Title, CreatedBy: userId)
4. Persist Page to database (generates ID)
5. Initialize empty CRDT doc: create initial YDoc state
6. Create DocState entry with seq=1, isSnapshot=true (initial snapshot)
7. Create Resource entry for page (type: page)
8. Create ACL: grant admin capability to creator on page resource
9. Emit `PageCreated` domain event
10. Return PageDto

## Postconditions
- Page exists in database
- Empty CRDT document initialized
- Resource and ACL entries created
- Creator can edit page and manage permissions

## Output
```json
{
  "id": "uuid",
  "orgId": "uuid",
  "title": "My Document",
  "createdBy": "uuid",
  "createdAt": "2025-01-15T10:30:00Z"
}
```

## Error Scenarios
- **ValidationError** (400): Title is empty
- **Forbidden** (403): User not member of org
- **NotFound** (404): Org doesn't exist
- **Unauthorized** (401): User not authenticated

## Testing
- **Happy path**: Valid title → page created with empty doc
- **Validation**: Empty title → 400 error
- **Authorization**: Non-member → 403 error
- **Integration**: Verify page, doc_states snapshot, resource, and ACL created

## Related Use Cases
- AppendBlock: Add blocks to page
- ApplyCrdtUpdate: Collaborative editing
- SharePage: Grant access to others
