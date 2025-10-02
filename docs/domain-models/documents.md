# Documents Bounded Context - Domain Model

## Overview
Manages collaborative documents with block-based structure and CRDT-based real-time editing.

## Aggregates

### Page (Aggregate Root)
**Purpose**: Represents a document page with collaborative editing support.

**Properties**:
- `Id` (Guid): Unique identifier
- `OrgId` (Guid): Parent organization
- `Title` (string): Page title
- `CreatedBy` (Guid): User who created the page
- `CreatedAt` (DateTimeOffset): Creation timestamp
- `Blocks` (Collection<Block>): Page blocks (metadata only)

**Invariants**:
- Title cannot be empty or whitespace
- Must belong to an existing org
- Blocks must form valid tree (no cycles, valid parent references)
- Block sort_keys must be unique within same parent
- CreatedBy must reference existing user

**Business Rules**:
- Pages are owned by organizations
- Access controlled via Authorization context ACLs
- CRDT state stored separately in doc_states table
- Blocks collection in Page represents metadata; actual content in CRDT

**Domain Events**:
- `PageCreated`: When page is created
- `PageTitleChanged`: When title is updated
- `BlockAdded`: When block metadata is added
- `BlockMoved`: When block position changes
- `BlockDeleted`: When block is removed

---

### Block (Entity, child of Page)
**Purpose**: Represents a block of content within a page (paragraph, heading, todo, file).

**Properties**:
- `Id` (Guid): Unique identifier
- `PageId` (Guid): Parent page
- `ParentBlockId` (Guid?): Parent block for nesting (null for root blocks)
- `SortKey` (SortKey VO): Fractional index for ordering
- `Type` (BlockType VO): Block type (paragraph, heading, todo, file)
- `Json` (JsonDocument): Block-specific attributes (heading level, todo checked, etc.)

**Invariants**:
- Must belong to a page
- ParentBlockId, if set, must reference block on same page
- Cannot create cycles in parent chain
- SortKey must be unique among siblings (same parent)
- Type must be valid
- Json schema must match block type requirements

**Business Rules**:
- Blocks form a tree structure (parent-child relationships)
- Siblings ordered by SortKey (fractional indexing for O(1) moves)
- Block content synchronized via CRDT; Block entity stores metadata only
- File blocks reference files via file_blocks table

---

## Value Objects

### SortKey
**Purpose**: Enables efficient reordering without renumbering siblings.

**Properties**:
- `Value` (decimal): Fractional index (precision 18,9)

**Behavior**:
- `Between(SortKey? before, SortKey? after) -> SortKey`
  - Generates new sort key between two existing keys
  - Handles edge cases: null before (prepend), null after (append)
  - Uses fractional arithmetic to avoid collisions

**Validation**:
- Must be positive
- Precision: 18 digits total, 9 after decimal

**Equality**: By value

**Example**:
- Block A: sort_key = 1.000000000
- Block B: sort_key = 2.000000000
- Insert between A and B: sort_key = 1.500000000
- Insert before A: sort_key = 0.500000000

---

### BlockType
**Purpose**: Defines supported block types.

**Values**:
- `Paragraph`: Standard text block
- `Heading`: Heading (level 1-3 in json.level)
- `Todo`: Checklist item (checked state in json.checked)
- `File`: File attachment (references file via file_blocks)

**Equality**: By value

**JSON Schemas**:
- Paragraph: `{ }`
- Heading: `{ "level": 1-3 }`
- Todo: `{ "checked": true/false }`
- File: `{ }` (file reference stored in file_blocks table)

---

## CRDT Integration

### IDocumentCrdt (Port)
**Purpose**: Abstraction for CRDT operations (implemented by YDotNet adapter).

**Operations**:
- `ApplyUpdate(ReadOnlySpan<byte> update)`: Apply remote update to local doc
- `GetStateVector() -> byte[]`: Get current document state vector
- `EncodeUpdateSince(ReadOnlySpan<byte> stateVector) -> byte[]`: Generate delta since state vector

**Implementation**: YDotNet adapter (Yrs bindings for .NET)

---

### DocState (Entity, separate table)
**Purpose**: Append-only log of CRDT updates with snapshot support.

**Properties**:
- `PageId` (Guid): Associated page
- `Seq` (long): Sequence number (auto-increment per page)
- `CrdtBlob` (byte[]): Encoded CRDT update
- `CreatedAt` (DateTimeOffset): Update timestamp
- `IsSnapshot` (bool): True if this is a compacted snapshot

**Invariants**:
- (PageId, Seq) must be unique
- Seq must be sequential per page
- IsSnapshot = true implies this is a full state, not a delta

**Business Rules**:
- Updates appended atomically with seq increment
- Deduplication: if same (PageId, Seq) exists, ignore (idempotency)
- Snapshot compaction runs periodically to combine deltas
- Snapshots marked with IsSnapshot = true; old deltas pruned after snapshot

---

## Domain Services

### BlockTreeService
**Purpose**: Maintains block tree validity.

**Operations**:
- `MoveBlock(Block block, Guid? newParentId, SortKey newSortKey)`
  - Validates no cycles created
  - Updates parent and sort key
  - Triggers BlockMoved event

- `DeleteBlock(Block block)`
  - Recursively deletes children
  - Removes file_blocks associations
  - Triggers BlockDeleted event

- `InsertBlock(Block block, Guid? parentId, Guid? beforeId, Guid? afterId) -> SortKey`
  - Calculates sort key based on before/after siblings
  - Validates parent exists
  - Returns generated sort key

---

### CrdtSyncService
**Purpose**: Coordinates CRDT updates between clients and persistence.

**Operations**:
- `ApplyUpdate(Guid pageId, byte[] update, byte[]? stateVector) -> long`
  - Appends update to doc_states with next seq
  - Applies update to in-memory CRDT instance (if loaded)
  - Returns assigned seq for idempotency
  - Broadcasts update to SignalR group

- `GetPageState(Guid pageId) -> (byte[] snapshot, byte[] stateVector)`
  - Loads latest snapshot or rebuilds from updates
  - Returns snapshot and state vector for client sync

- `CompactUpdates(Guid pageId)`
  - Rebuilds full CRDT state from updates
  - Saves new snapshot
  - Prunes old delta entries (keeps last N days or N updates)

---

## Integration Points

**Outbound**:
- `IPageRepository`: Persistence for Page and Blocks
- `IDocStateRepository`: Persistence for CRDT updates
- `IDocumentCrdt`: CRDT operations (YDotNet adapter)
- `IAuthorizationService`: Check edit permissions before updates
- SignalR `IDocClient`: Broadcast updates to connected clients

**Inbound**:
- REST API for page/block metadata CRUD
- SignalR hub for CRDT updates
- Snapshot worker (HostedService) for compaction

---

## Real-time Collaboration Flow

1. **Client connects**: Opens WebSocket to SignalR hub, joins page group
2. **Client sends update**: `hub.ApplyCrdtUpdate(pageId, updateB64, svB64)`
3. **Server**:
   - Validates user has edit permission via AuthorizationService
   - Calls `CrdtSyncService.ApplyUpdate(pageId, update, stateVector)`
   - Appends to doc_states with seq
   - Broadcasts to group: `clients.CrdtUpdate(pageId, updateB64)`
4. **Other clients receive**: Apply update to local Yjs doc
5. **Snapshot worker**: Periodically compacts updates into snapshots

---

## Testing Strategy

### Domain Tests
- SortKey.Between() fractional arithmetic
- BlockTreeService cycle detection
- Block tree invariants (valid parent chain)

### Use Case Tests
- CreatePage: initial state
- ApplyUpdate: append-only log, seq assignment
- MoveBlock: sort key recalculation
- Snapshot compaction: rebuild from deltas

### Integration Tests
- EF Core mapping for Page, Block, DocState
- Concurrent CRDT updates (race conditions)
- SignalR hub message flow (Testcontainers + SignalR test client)
- Snapshot rebuild correctness (compare state after compaction)

### Property Tests
- SortKey generation: always between bounds, no collisions
- CRDT convergence: same updates in different orders yield same state
