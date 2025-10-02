# Use Case: AttachFileToBlock

## Summary
Links an uploaded file to a document block, completing the file upload flow.

## Actors
- **User** (authenticated): User attaching the file

## Preconditions
- User must be authenticated
- User must have edit permission on page containing block
- File must exist in database (created during presign step)
- File must be successfully uploaded to storage
- Block must exist and be of type "file"

## Input
- `BlockId` (Guid, from URL): Target block
- `FileId` (Guid, required): File to attach

## Business Rules
- Block must be type "file"
- File and block must belong to same org
- User must have edit permission on page
- File must be owned by user or user must have access via org
- MVP: one file per block (future: multiple files)

## Process Flow
1. Load block and validate exists
2. Load page containing block
3. Validate user has edit permission on page (via AuthorizationService)
4. Validate block type is "file"
5. Load file and validate exists
6. Validate file and block belong to same org
7. Verify file upload completed (check existence in storage - optional)
8. Create FileBlock entry: (BlockId, FileId)
9. Update file status to "attached" (optional tracking)
10. Persist FileBlock to database
11. Emit `FileAttached` domain event
12. Return 204 No Content

## Postconditions
- FileBlock association exists
- File visible in document block
- File can be downloaded by users with view permission on page

## Output
- **204 No Content** on success

## Error Scenarios
- **ValidationError** (400): Block is not type "file" or org mismatch
- **Forbidden** (403): User doesn't have edit permission on page
- **NotFound** (404): Block or file doesn't exist
- **Unauthorized** (401): User not authenticated
- **Conflict** (409): File already attached to another block (if enforcing uniqueness)

## Testing
- **Happy path**: Valid file and block → attached
- **Type validation**: Non-file block → 400 error
- **Org validation**: File from different org → 400 error
- **Authorization**: Non-editor → 403 error
- **Not found**: Invalid fileId → 404 error
- **Integration**: Verify FileBlock persisted, file visible in UI

## Related Use Cases
- UploadFile: Must presign and upload before attaching
- AppendBlock: Create file block first
- DetachFile: Remove file from block (future)

## Notes
- This completes the upload flow: presign → upload → attach
- Client flow: 1) call UploadFile, 2) POST to presigned URL, 3) call AttachFileToBlock
- File can be attached to multiple blocks (soft reference) but MVP restricts to one
