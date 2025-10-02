# Authorization Bounded Context - Domain Model

## Overview
Manages access control through ACLs (Access Control Lists) and share links with capabilities.

## Aggregates

### Resource (Aggregate Root)
**Purpose**: Represents any entity that can have permissions (Org, Page, File).

**Properties**:
- `Id` (Guid): Unique identifier
- `Type` (ResourceType VO): Type of resource (org, page, file)
- `Acls` (Collection<Acl>): Access control entries

**Invariants**:
- Type must be valid
- Resource must exist before ACLs can be created
- Cannot have duplicate ACLs for same (SubjectType, SubjectId, Capability)

**Domain Events**:
- `ResourceCreated`: When resource is registered
- `AccessGranted`: When ACL is added
- `AccessRevoked`: When ACL is removed

---

### Acl (Entity, child of Resource)
**Purpose**: Grants a specific capability to a subject on a resource.

**Properties**:
- `ResourceId` (Guid): Target resource
- `SubjectType` (SubjectType VO): Type of subject (user, org, role)
- `SubjectId` (string): Subject identifier (userId, orgId, or role name)
- `Capability` (Capability VO): Granted permission (view, comment, edit, admin)

**Invariants**:
- SubjectType must be valid
- Capability must be valid
- Composite key: (ResourceId, SubjectType, SubjectId, Capability) must be unique

**Business Rules**:
- Admin capability implies edit, comment, and view
- Edit capability implies comment and view
- Comment capability implies view
- Capabilities are hierarchical: admin > edit > comment > view

---

### ShareLink (Aggregate Root)
**Purpose**: Provides temporary token-based access to a resource.

**Properties**:
- `Id` (Guid): Unique identifier
- `ResourceId` (Guid): Target resource
- `Capability` (Capability VO): Granted permission level
- `TokenHash` (string): Hashed share token
- `Token` (string): Cleartext token (only available at creation)
- `ExpiresAt` (DateTimeOffset?): Optional expiration
- `CreatedAt` (DateTimeOffset): Creation timestamp
- `CreatedBy` (Guid): User who created the link

**Invariants**:
- Token must be cryptographically random (min 32 bytes)
- TokenHash must be unique
- ExpiresAt, if set, must be in the future
- Resource must exist

**Business Rules**:
- Token is only returned once at creation
- Token is hashed before storage (SHA256)
- Expired links cannot be used for access
- Share links can be revoked (deleted)

**Domain Events**:
- `ShareLinkCreated`: When link is generated
- `ShareLinkRevoked`: When link is deleted
- `ShareLinkAccessed`: When link is used (audit)

---

## Value Objects

### ResourceType
**Purpose**: Defines types of resources that can have permissions.

**Values**:
- `Org`: Organization
- `Page`: Document page
- `File`: Uploaded file

**Equality**: By value

---

### SubjectType
**Purpose**: Defines who can be granted access.

**Values**:
- `User`: Individual user by ID
- `Org`: All members of an organization
- `Role`: Members with specific role (e.g., "admin")

**Equality**: By value

---

### Capability
**Purpose**: Defines permission levels.

**Values**:
- `View`: Read-only access
- `Comment`: Can add comments (not implemented in MVP, but reserved)
- `Edit`: Can modify content
- `Admin`: Full control including permission management

**Hierarchy**:
- Admin > Edit > Comment > View
- Higher capabilities include all lower capabilities

**Equality**: By value

---

## Domain Services

### AuthorizationService
**Purpose**: Centralized authorization checking.

**Operations**:
- `Can(Guid userId, ResourceId resourceId, Capability capability) -> bool`
  - Checks if user has required capability on resource
  - Evaluates direct user ACLs
  - Evaluates org membership ACLs
  - Evaluates role-based ACLs
  - Applies capability hierarchy (admin implies edit, etc.)

- `GrantAccess(ResourceId resourceId, SubjectType subjectType, string subjectId, Capability capability)`
  - Creates ACL entry
  - Deduplicates if ACL already exists

- `RevokeAccess(ResourceId resourceId, SubjectType subjectType, string subjectId, Capability capability)`
  - Removes ACL entry

---

### ShareLinkService
**Purpose**: Manages share link lifecycle.

**Operations**:
- `CreateShareLink(ResourceId resourceId, Capability capability, DateTimeOffset? expiresAt, Guid createdBy) -> ShareLink`
  - Generates cryptographically random token
  - Hashes token for storage
  - Returns link with cleartext token (only time it's available)

- `ValidateShareToken(string token) -> (ResourceId, Capability)?`
  - Hashes incoming token
  - Looks up by TokenHash
  - Checks expiration
  - Returns resource and capability if valid, null if invalid/expired

---

## Integration Points

**Outbound**:
- `IResourceRepository`: Persistence for Resource and ACLs
- `IShareLinkRepository`: Persistence for ShareLinks
- `IOrgRepository`: Query org memberships for ACL evaluation
- `IUserRepository`: Query user existence for ACL validation

**Inbound**:
- All API endpoints call `AuthorizationService.Can()` before operations
- Share link endpoints use `ShareLinkService`

---

## Testing Strategy

### Domain Tests
- Capability hierarchy: admin includes edit, edit includes view
- ACL uniqueness enforcement
- ShareLink token generation (randomness, length)
- ShareLink expiration logic

### Use Case Tests
- AuthorizationService.Can() with various ACL combinations
- User ACL vs org ACL vs role ACL priority
- ShareLink validation: expired, revoked, invalid token

### Integration Tests
- ACL persistence and querying
- ShareLink token hashing and lookup
- Concurrent ACL modifications
- Performance of authorization checks with many ACLs
