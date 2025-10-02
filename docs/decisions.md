# Architecture Decision Records (ADRs)

## ADR-001: Blocks as Rows + CRDT Blob (vs Full JSONB Document)

**Date**: 2025-01-15
**Status**: Accepted

### Context
We need to store document structure for block-based editing. Two main approaches:

1. **JSONB Document**: Store entire document as single JSONB column in pages table
   - Pros: Simple, fast to load entire doc, easy to serialize
   - Cons: No block-level queries, no block-level ACLs, harder to implement fine-grained permissions

2. **Block Rows + CRDT Blob**: Store block metadata as rows, CRDT content separately
   - Pros: Fine-grained queries (search, filter by type), block-level ACLs possible, sort keys enable O(1) moves
   - Cons: More complex schema, join queries, CRDT state separate

### Decision
**Blocks as rows with CRDT blob in separate table (`doc_states`).**

**Rationale**:
- Block metadata (type, position, parent) needed for:
  - Structural queries (find all headings, todo completion stats)
  - Potential future feature: block-level comments or permissions
  - File attachments (file_blocks junction table)
- CRDT handles collaborative text editing, not structural changes
- Separation allows independent optimization:
  - Block rows indexed for fast queries
  - CRDT blob optimized for append-only writes
- Fractional sort_key enables O(1) reordering without renumbering siblings

**Consequences**:
- Block CRUD operations require both block metadata updates and potential CRDT updates
- Need to keep block tree valid (no cycles, valid parent refs)
- `doc_states` table grows unbounded (requires snapshot compaction)

---

## ADR-002: Append-Only CRDT Updates + Snapshot Compaction

**Date**: 2025-01-15
**Status**: Accepted

### Context
CRDT updates accumulate over time. Two storage strategies:

1. **Overwrite Latest State**: Replace doc state on each update
   - Pros: Constant space, simple
   - Cons: No history, no conflict resolution, lost updates on race conditions

2. **Append-Only Log**: Store every update, compact periodically
   - Pros: History preserved, idempotency via (page_id, seq), replaying updates reconstructs state
   - Cons: Unbounded growth, requires compaction

### Decision
**Append-only log with periodic snapshot compaction.**

**Rationale**:
- Append-only enables idempotency: duplicate (page_id, seq) → no-op
- Sequence numbers provide total order for conflict resolution
- History useful for debugging, audit, potential time-travel feature
- Compaction mitigates unbounded growth

**Snapshot Thresholds**:
- **Count-based**: Compact after 100 updates
- **Size-based**: Compact after 1MB of deltas
- **Time-based**: Compact updates older than 7 days (keeps recent deltas for potential undo)

**Compaction Process**:
1. Load latest snapshot (if exists) or rebuild from beginning
2. Apply all deltas since snapshot
3. Encode full YDoc state as new snapshot
4. Insert snapshot with `is_snapshot=true`, new seq
5. Delete old deltas (keep last 100 or last 24 hours for safety)

**Consequences**:
- `doc_states` table grows until compaction runs
- Compaction worker (HostedService) runs periodically (nightly)
- Page load: fetch latest snapshot + apply deltas since snapshot
- Trade-off: disk space vs history granularity

---

## ADR-003: Redis Backplane for SignalR Scale-Out

**Date**: 2025-01-15
**Status**: Accepted

### Context
SignalR connections are stateful (WebSocket). Horizontal scaling requires message synchronization across server instances.

Options:
1. **Sticky sessions**: Route user to same server
   - Pros: No backplane needed
   - Cons: Uneven load, session loss on server restart

2. **Redis backplane**: Pub/sub for cross-server messaging
   - Pros: Stateless load balancing, high availability
   - Cons: Redis dependency, slight latency

3. **Azure SignalR Service**: Managed service
   - Pros: Fully managed, elastic scaling
   - Cons: Vendor lock-in, cost

### Decision
**Redis backplane with StackExchange.Redis.**

**Rationale**:
- Enables stateless horizontal scaling (critical for cloud deployment)
- Redis already used for caching (potential future use)
- Low latency: Redis on same network as app servers
- Open-source, self-hosted (no vendor lock-in)
- Azure SignalR Service reserved for future if Redis bottleneck

**Configuration**:
- Channel prefix: `DocHub:` to namespace messages
- Redis connection: persistent connection pool
- Message size limit: 100KB (large updates trigger full sync)

**Consequences**:
- Redis becomes critical dependency (requires monitoring, HA setup)
- Network hop adds ~1-5ms latency to broadcasts
- Redis memory usage: ~1KB per active connection + message queue

---

## ADR-004: YDotNet Adapter for Yjs Compatibility

**Date**: 2025-01-15
**Status**: Accepted

### Context
CRDT library choice for .NET backend:

1. **Automerge.NET**: Native C# CRDT
   - Pros: Pure C#, no native deps
   - Cons: Less mature, smaller ecosystem, no Tiptap integration

2. **YDotNet** (Yrs bindings): Rust bindings via P/Invoke
   - Pros: Battle-tested (Yjs ecosystem), Tiptap native support, fast (Rust)
   - Cons: Native dependency, P/Invoke overhead

### Decision
**YDotNet with Yrs (Rust Yjs port) bindings.**

**Rationale**:
- Tiptap editor has native Yjs collaboration extension (y-prosemirror)
- Yjs wire format well-documented, interoperable
- Yrs performance superior to C# implementations
- Active maintenance, production-proven (Notion, Linear, etc.)

**Implementation**:
- `IDocumentCrdt` port defines interface
- `YDotNetAdapter` implements using Yrs bindings
- Thread-safe: one YDoc instance per page, lock on apply/encode

**Consequences**:
- Native library deployment: include Yrs .so/.dll in Docker image
- P/Invoke overhead: ~10µs per call (acceptable for update frequency)
- Future: if Yrs bindings unmaintained, implement IDocumentCrdt with Automerge

---

## ADR-005: Presigned URLs for File Upload/Download (vs Proxy)

**Date**: 2025-01-15
**Status**: Accepted

### Context
File upload/download strategies:

1. **Proxy through backend**: Client → API → Storage
   - Pros: Centralized auth, rate limiting, virus scanning
   - Cons: Backend bandwidth bottleneck, buffering overhead

2. **Presigned URLs**: Direct client ↔ storage with temporary signed URLs
   - Pros: No backend bottleneck, CDN-friendly, scalable
   - Cons: Auth at generation time only, expiry management

### Decision
**Presigned URLs for both upload (POST) and download (GET).**

**Upload Flow**:
1. Client calls `POST /files:presign {mime, size, filename}`
2. Backend validates auth, generates presigned POST URL (15min expiry)
3. Client uploads directly to MinIO using presigned URL
4. Client calls `POST /blocks/{blockId}/file {fileId}` to attach

**Download Flow**:
1. Client calls `GET /files/{fileId}/download-url`
2. Backend validates view permission, generates presigned GET URL (1h expiry)
3. Client downloads directly from presigned URL

**Rationale**:
- Offloads bandwidth from backend (critical for large files, videos)
- MinIO enforces policy (MIME type, size) via presigned POST
- Expiry limits window for leaked URLs
- S3-compatible (portable to AWS S3, Cloudflare R2, Azure Blob)

**Consequences**:
- File access controlled at URL generation time (not download time)
- Leaked URLs valid until expiry (mitigation: short expiry, revoke share links)
- Cannot stream or transform files in backend (e.g., thumbnail generation)
  - Future: Add thumbnail worker that fetches from MinIO, generates, stores separately

---

## ADR-006: Test-Driven Development with Testcontainers

**Date**: 2025-01-15
**Status**: Accepted

### Context
Testing strategy for infrastructure components (DB, Redis, MinIO):

1. **Mocks**: In-memory fake implementations
   - Pros: Fast, no external deps
   - Cons: Behavior divergence from real services

2. **Shared test DB**: Single DB instance for all tests
   - Pros: Real DB behavior
   - Cons: Test pollution, slow cleanup, race conditions

3. **Testcontainers**: Docker containers per test suite
   - Pros: Isolated, real services, parallel execution
   - Cons: Docker dependency, slower startup

### Decision
**Testcontainers for integration tests, in-memory for unit tests.**

**Test Pyramid**:
- **Unit Tests**: Domain logic, use-case handlers with mocked ports (fast, no I/O)
- **Integration Tests**: Repositories, SignalR hubs with Testcontainers (Postgres, Redis, MinIO)
- **E2E Tests**: Full stack with Docker Compose (optional, expensive)

**Testcontainers Configuration**:
```csharp
[Collection("Database")]
public class OrgRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

**Rationale**:
- Integration tests verify EF Core mappings, SQL query correctness
- Testcontainers provide true Postgres (not in-memory SQLite with divergent behavior)
- Parallel execution: each test class gets isolated container
- CI-friendly: GitHub Actions supports Docker

**Consequences**:
- Test execution slower (~5s container startup per suite)
- Docker required on dev machines and CI
- Faster than manual DB setup/teardown
- Future: Testcontainers snapshot caching for faster reruns

---

## ADR-007: Bounded Contexts with Shared Kernel (User, Org IDs)

**Date**: 2025-01-15
**Status**: Accepted

### Context
Bounded context isolation in DDD:

1. **Strict boundaries**: No shared types, duplicate User in each context
   - Pros: Complete independence, evolvability
   - Cons: Duplication, sync complexity, eventual consistency burden

2. **Shared kernel**: Common types (User, Org IDs) shared across contexts
   - Pros: Simplified refs, immediate consistency
   - Cons: Coupling, harder to extract contexts to microservices

### Decision
**Shared kernel for User and Org identity, separate contexts for domain logic.**

**Shared Kernel** (`Backend.Domain.Shared`):
- `UserId` (Guid VO)
- `OrgId` (Guid VO)
- Common value objects: `Email`, `DateTimeRange`

**Bounded Contexts**:
- **IdentityOrg**: Owns User, Org aggregates
- **Authorization**: References User/Org by ID, owns ACL logic
- **Documents**: References User/Org by ID, owns Page/Block
- **Files**: References User/Org by ID, owns File/presigned URL

**Rationale**:
- User and Org are stable, unlikely to diverge across contexts
- Cross-context queries simpler (no eventual consistency for basic refs)
- Acceptable coupling for monolithic deployment
- Future: If extracting to microservices, convert to async events (e.g., UserCreated)

**Consequences**:
- Contexts coupled via User/Org IDs (cannot deploy independently without API gateway)
- Schema shares users, orgs tables (foreign keys across context tables)
- Acceptable trade-off for monolithic teaching reference

---

## Summary Table

| ADR | Decision | Reason |
|-----|----------|--------|
| 001 | Block rows + CRDT blob | Enable fine-grained queries and potential block-level ACLs |
| 002 | Append-only + compaction | Idempotency, history, conflict resolution |
| 003 | Redis backplane | Horizontal scaling for SignalR |
| 004 | YDotNet (Yrs bindings) | Tiptap compatibility, performance, maturity |
| 005 | Presigned URLs | Offload bandwidth, scalability |
| 006 | Testcontainers | Real services in tests, isolation |
| 007 | Shared kernel (User/Org) | Simplify refs, acceptable coupling for monolith |

---

## Future Considerations

### ADR-008: Offline Sync with Conflict Resolution (Deferred)
**Status**: Not Implemented in MVP

If implementing offline editing:
- Client stores YDoc state in IndexedDB
- On reconnect, merge local updates with server state
- Yjs CRDT handles automatic conflict resolution
- Consideration: Auth token expiry, stale permissions

### ADR-009: Event Sourcing for Audit Log (Deferred)
**Status**: Not Implemented in MVP

For audit/compliance:
- Store domain events (PageCreated, BlockMoved, etc.) in event_log table
- Replay events to reconstruct state
- Consideration: Schema evolution, event versioning, storage cost

### ADR-010: Elasticsearch for Full-Text Search (Deferred)
**Status**: Not Implemented in MVP

For advanced search:
- Index block content from CRDT state in Elasticsearch
- Postgres full-text search sufficient for MVP
- Consideration: Sync lag, consistency, infrastructure cost
