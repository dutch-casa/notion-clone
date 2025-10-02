# Use Case: AppendBlock

## Summary
Adds a new block to a page with specified type and position.

## Actors
- **User** (authenticated): User adding the block

## Preconditions
- User must be authenticated
- User must have edit permission on page
- Parent page must exist
- Parent block (if specified) must exist and belong to same page

## Input
- `PageId` (Guid, from URL): Target page
- `Type` (BlockType, required): Block type (paragraph, heading, todo, file)
- `ParentBlockId` (Guid?, optional): Parent block for nesting (null for root)
- `Before` (Guid?, optional): Block ID to insert before (for sort key calculation)
- `After` (Guid?, optional): Block ID to insert after (for sort key calculation)
- `Json` (JsonDocument, optional): Block-specific attributes

## Business Rules
- Type must be valid BlockType
- If ParentBlockId specified, must exist and belong to page
- Cannot create cycles in parent chain
- Sort key calculated based on before/after siblings
- Json schema must match block type (heading → level, todo → checked)

## Process Flow
1. Validate user has edit permission on page (via AuthorizationService)
2. Load page and validate exists
3. If ParentBlockId specified, validate parent exists and belongs to page
4. Calculate sort key using BlockTreeService.InsertBlock():
   - If both before and after specified: sort key between them
   - If only before: sort key before it
   - If only after: sort key after it
   - If neither: append to end (largest sort key + 1)
5. Create Block entity: (PageId, ParentBlockId, SortKey, Type, Json)
6. Validate Json schema matches type
7. Persist block to database
8. Emit `BlockAdded` domain event
9. Return BlockDto

## Postconditions
- Block exists in database with correct position
- Block tree remains valid (no cycles)
- Sort keys remain unique among siblings

## Output
```json
{
  "id": "uuid",
  "pageId": "uuid",
  "parentBlockId": null,
  "sortKey": 1.500000000,
  "type": "paragraph",
  "json": {}
}
```

## Error Scenarios
- **ValidationError** (400): Invalid type, json schema mismatch, or cycle detected
- **Forbidden** (403): User doesn't have edit permission
- **NotFound** (404): Page or parent block doesn't exist
- **Unauthorized** (401): User not authenticated

## Testing
- **Happy path**: Valid block → added with correct sort key
- **Positioning**: Insert before/after → sort key between siblings
- **Nesting**: Parent specified → block nested correctly
- **Cycle detection**: Parent chain creates cycle → 400 error
- **Type validation**: Invalid type → 400 error
- **Json schema**: Heading without level → 400 error

## Related Use Cases
- CreatePage: Page must exist first
- MoveBlock: Reposition existing block
- ApplyCrdtUpdate: Block content edited via CRDT
