# Files Bounded Context - Domain Model

## Overview
Manages file uploads, storage, and attachment to document blocks using presigned URLs.

## Aggregates

### File (Aggregate Root)
**Purpose**: Represents an uploaded file stored in S3-compatible storage.

**Properties**:
- `Id` (Guid): Unique identifier
- `OrgId` (Guid): Owning organization
- `OwnerId` (Guid): User who uploaded the file
- `Key` (string): Object key in storage (e.g., `{orgId}/{fileId}/{filename}`)
- `Mime` (string): MIME type (e.g., `image/png`, `application/pdf`)
- `Size` (long): File size in bytes
- `CreatedAt` (DateTimeOffset): Upload timestamp

**Invariants**:
- Key must be unique in storage
- Must belong to existing org
- OwnerId must reference existing user
- Size must be positive
- Mime must be valid MIME type format

**Business Rules**:
- Files are scoped to organizations
- Access controlled via Authorization context ACLs on file resource
- Presigned URLs used for direct upload/download (no proxying through backend)
- Files can be attached to blocks via file_blocks junction table
- Orphaned files (not attached to any block) can be cleaned up by background job

**Domain Events**:
- `FileUploaded`: When file metadata is created (after presigned upload completes)
- `FileAttached`: When file is linked to a block
- `FileDetached`: When file is unlinked from a block
- `FileDeleted`: When file is removed

---

### FileBlock (Entity, junction table)
**Purpose**: Links files to document blocks.

**Properties**:
- `BlockId` (Guid): Target block
- `FileId` (Guid): Attached file

**Invariants**:
- Composite key: (BlockId, FileId) must be unique
- Block must be of type "file"
- File and Block must exist
- Block and File must belong to same org

**Business Rules**:
- File blocks can have multiple files (future: galleries)
- MVP: one file per block
- When block is deleted, FileBlock entries are removed
- Files are NOT deleted when detached (soft reference)

---

## Value Objects

### StorageKey
**Purpose**: Encapsulates S3 object key generation logic.

**Properties**:
- `Value` (string): Full storage key

**Behavior**:
- `Generate(Guid orgId, Guid fileId, string filename) -> StorageKey`
  - Format: `{orgId}/{fileId}/{sanitized-filename}`
  - Sanitizes filename: remove path traversal, special chars
  - Ensures uniqueness via fileId

**Validation**:
- Cannot contain path traversal (`../`, `..\\`)
- Cannot contain null bytes or control characters
- Max length: 1024 characters

**Equality**: By value

---

## Domain Services

### StorageService (Port)
**Purpose**: Abstraction for S3-compatible storage operations.

**Operations**:
- `GeneratePresignedPost(string key, string mime, long maxSize, TimeSpan expiry) -> PresignedPost`
  - Returns presigned POST URL and form fields for direct browser upload
  - Enforces MIME type and size limits via policy
  - Expiry typically 15 minutes

- `GeneratePresignedGet(string key, TimeSpan expiry) -> string`
  - Returns presigned GET URL for direct browser download
  - Expiry typically 1 hour

- `DeleteAsync(string key)`
  - Removes object from storage

**Implementation**: MinIO adapter (S3-compatible)

---

### FileAttachmentService
**Purpose**: Coordinates file lifecycle with blocks.

**Operations**:
- `AttachFileToBlock(Guid blockId, Guid fileId)`
  - Validates block type is "file"
  - Validates file and block belong to same org
  - Creates FileBlock entry
  - Triggers FileAttached event

- `DetachFileFromBlock(Guid blockId, Guid fileId)`
  - Removes FileBlock entry
  - Does NOT delete file (soft reference)
  - Triggers FileDetached event

- `CleanupOrphanedFiles(TimeSpan age)`
  - Finds files not attached to any block for longer than `age`
  - Deletes from storage and DB
  - Background job (not MVP)

---

## Upload Flow (Presigned POST)

1. **Client prepares upload**:
   - User selects file in UI
   - Generate client-side `fileId` (Guid)

2. **Request presigned URL**:
   - `POST /files:presign { mime, size }`
   - Server validates org access
   - Generates storage key: `{orgId}/{fileId}/{filename}`
   - Returns presigned POST URL + fields

3. **Client uploads directly to storage**:
   - `POST {presignedUrl}` with form fields + file
   - MinIO validates MIME, size, expiry via policy
   - Returns 204 No Content on success

4. **Confirm upload**:
   - Client calls `POST /blocks/{blockId}/file { fileId }`
   - Server creates File entity and FileBlock association
   - File is now attached and visible in editor

---

## Download Flow (Presigned GET)

1. **Client requests file URL**:
   - `GET /files/{fileId}/download-url`
   - Server validates view permission via AuthorizationService
   - Generates presigned GET URL (1 hour expiry)
   - Returns URL

2. **Client downloads**:
   - Browser navigates to presigned URL
   - MinIO serves file directly
   - No proxying through backend

---

## Integration Points

**Outbound**:
- `IFileRepository`: Persistence for File entities
- `IFileBlockRepository`: Persistence for FileBlock associations
- `IStorageService`: S3-compatible storage operations
- `IAuthorizationService`: Check upload/view permissions

**Inbound**:
- REST API for presign, attach, detach operations
- File blocks in Documents context reference files

---

## Testing Strategy

### Domain Tests
- StorageKey generation: sanitization, uniqueness
- FileAttachmentService: same-org validation

### Use Case Tests
- PresignFile: valid MIME types, size limits
- AttachFile: block type validation, org match
- DetachFile: soft delete behavior

### Integration Tests
- MinIO presigned POST: upload with policy enforcement
- Presigned GET: download with expiration
- FileBlock associations: EF Core mapping
- File deletion: cascade behavior

### End-to-End Tests
- Full upload flow: presign → upload → attach → view
- Permission checks: unauthorized access blocked
- Expiry: expired presigned URLs rejected
