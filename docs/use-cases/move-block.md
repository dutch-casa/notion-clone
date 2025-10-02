# Use Case: MoveBlock

## Summary
Repositions an existing block to a new location (different parent or position among siblings).

## Actors
- **User** (authenticated): User moving the block

## Preconditions
- User must be authenticated
- User must have edit permission on page
- Block must exist
- New parent block (if specified) must exist and belong to same page

## Input
- `PageId` (Guid, from URL): Page containing block
- `BlockId` (Guid, from URL): Block to move
- `ParentBlockId` (Guid?, optional): New parent (null for root level)
- `Before` (Guid?, optional): Block ID to insert before
- `After` (Guid?, optional): Block ID to insert after

## Business Rules
- Cannot move block to create cycle (block cannot be ancestor of its new parent)
- New sort key calculated based on before/after siblings under new parent
- Moving block also moves all descendant blocks (subtree)
- If ParentBlockId = current parent and before/after specify same position → no-op

## Process Flow
1. Validate user has edit permission on page
2. Load block and validate exists
3. If ParentBlockId same as current and position unchanged → return 204 (no-op)
4. If ParentBlockId specified, validate parent exists and belongs to page
5. Validate no cycle: check if new parent is descendant of moving block
6. Calculate new sort key using BlockTreeService.MoveBlock():
   - Find before/after siblings under new parent
   - Generate sort key between them
7. Update block: set ParentBlockId and SortKey
8. Persist changes
9. Emit `BlockMoved` domain event
10. Return 204 No Content

## Postconditions
- Block positioned under new parent with correct sort key
- Block tree remains valid (no cycles)
- Descendant blocks follow parent

## Output
- **204 No Content** on success

## Error Scenarios
- **ValidationError** (400): Cycle detected or invalid position
- **Forbidden** (403): User doesn't have edit permission
- **NotFound** (404): Block or new parent doesn't exist
- **Unauthorized** (401): User not authenticated

## Testing
- **Happy path**: Move to new parent → updated position
- **Reorder siblings**: Move within same parent → sort key recalculated
- **Cycle detection**: Move to own descendant → 400 error
- **Subtree move**: Move with children → children follow
- **No-op**: Same position → 204 no changes

## Related Use Cases
- AppendBlock: Create initial block position
- BlockTreeService: Maintains tree validity
