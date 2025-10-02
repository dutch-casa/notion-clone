# Use Case: ApplyCrdtUpdate

## Summary
Applies a CRDT update from a client to the server, persists it, and broadcasts to other connected clients.

## Actors
- **Client** (authenticated): User editing the document

## Preconditions
- User must be authenticated
- User must have edit permission on page
- Page must exist
- Client connected via SignalR hub

## Input
- `PageId` (Guid): Target page
- `UpdateB64` (string, required): Base64-encoded CRDT update (Yjs delta)
- `StateVectorB64` (string?, optional): Base64-encoded state vector (for conflict resolution)

## Business Rules
- Update must be valid Yjs update format
- Updates are append-only: never overwrite existing seq
- Idempotency: duplicate (PageId, Seq) → ignore silently
- Updates broadcast to all clients in page group except sender
- Snapshot compaction triggered periodically (not synchronously)

## Process Flow
1. Validate user has edit permission on page (via AuthorizationService)
2. Decode updateB64 from base64 to byte[]
3. Validate update format (try deserialize with YDotNet)
4. Atomic operation:
   - Load current max seq for page
   - Increment seq
   - Insert DocState: (PageId, Seq, CrdtBlob: update, CreatedAt: now)
   - If conflict (duplicate seq) → catch exception, return existing seq (idempotency)
5. Apply update to in-memory CRDT instance (if cached)
6. Broadcast to SignalR group `page:{pageId}`:
   - `Clients.Group(...).CrdtUpdate(pageId, updateB64)`
   - Exclude sender (use `Clients.OthersInGroup(...)`)
7. Return 202 Accepted with seq number

## Postconditions
- Update persisted in doc_states table
- Other clients receive update and apply to their local Yjs docs
- Document converges across all clients (CRDT property)

## Output
```json
{
  "seq": 42,
  "pageId": "uuid"
}
```

## Error Scenarios
- **ValidationError** (400): Invalid update format or corrupted base64
- **Forbidden** (403): User doesn't have edit permission
- **NotFound** (404): Page doesn't exist
- **Unauthorized** (401): User not authenticated
- **Conflict** (409): Duplicate seq (handled as idempotent, returns existing seq)

## Testing
- **Happy path**: Valid update → persisted and broadcast
- **Idempotency**: Duplicate seq → no error, returns existing seq
- **Broadcast**: Other clients receive update
- **Authorization**: Non-editor → 403 error
- **Convergence**: Apply same updates in different orders → same final state (CRDT property test)
- **Load test**: Concurrent updates → all persisted with unique seqs

## Related Use Cases
- CreatePage: Page must exist first
- SignalR Hub: Transport for real-time updates
- Snapshot Compaction: Periodic cleanup of updates
