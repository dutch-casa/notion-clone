# Use Case: UploadFile (Presign)

## Summary
Generates a presigned POST URL for direct file upload to S3-compatible storage.

## Actors
- **User** (authenticated): User preparing to upload a file

## Preconditions
- User must be authenticated
- User must be member of organization
- MinIO (or S3-compatible storage) must be accessible

## Input
- `OrgId` (Guid, from context): User's current organization
- `Mime` (string, required): File MIME type (e.g., `image/png`)
- `Size` (long, required): File size in bytes
- `Filename` (string, required): Original filename

## Business Rules
- User must have upload permission in org (member or higher)
- File size must not exceed max limit (e.g., 100MB for MVP)
- MIME type must be in allowed list (images, PDFs, documents)
- Presigned URL expires after 15 minutes
- Storage key format: `{orgId}/{fileId}/{sanitized-filename}`

## Process Flow
1. Validate user is member of org (via AuthorizationService)
2. Validate MIME type is allowed
3. Validate size is within limits
4. Generate new fileId (Guid)
5. Sanitize filename (remove path traversal, special chars)
6. Generate storage key: `{orgId}/{fileId}/{filename}`
7. Call StorageService.GeneratePresignedPost():
   - Create presigned POST policy with MIME and size constraints
   - Set expiry to 15 minutes
   - Return URL and form fields
8. Create File entity (status: pending):
   - Id: fileId
   - OrgId: orgId
   - OwnerId: userId
   - Key: storage key
   - Mime: mime
   - Size: size
   - CreatedAt: now
9. Persist File to database (marks upload intent)
10. Return PresignResponse with URL and fields

## Postconditions
- File entity exists in pending state
- Client has presigned URL to upload directly to storage
- URL valid for 15 minutes

## Output
```json
{
  "fileId": "uuid",
  "url": "https://minio.example.com/files",
  "fields": {
    "key": "orgId/fileId/filename.png",
    "policy": "base64-encoded-policy",
    "x-amz-algorithm": "AWS4-HMAC-SHA256",
    "x-amz-credential": "...",
    "x-amz-date": "...",
    "x-amz-signature": "..."
  },
  "expiresAt": "2025-01-15T10:45:00Z"
}
```

## Error Scenarios
- **ValidationError** (400): Invalid MIME type, size too large, or bad filename
- **Forbidden** (403): User not member of org
- **Unauthorized** (401): User not authenticated
- **ServiceUnavailable** (503): Storage service unavailable

## Testing
- **Happy path**: Valid file → presigned URL generated
- **MIME validation**: Disallowed type → 400 error
- **Size validation**: Exceeds limit → 400 error
- **Expiry**: Use URL after 15 minutes → storage rejects
- **Policy enforcement**: Upload wrong MIME or size → storage rejects

## Related Use Cases
- AttachFileToBlock: After upload completes, attach file to block
- MinIO directly handles actual upload (no backend involvement)

## Notes
- This use case only generates the presigned URL
- Actual upload happens directly from client to MinIO
- Client must call AttachFileToBlock after upload succeeds to complete flow
