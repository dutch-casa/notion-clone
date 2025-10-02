# IdentityOrg Bounded Context - Domain Model

## Overview
Manages user identities, organization structure, and membership relationships.

## Aggregates

### User (Aggregate Root)
**Purpose**: Represents a user account in the system.

**Properties**:
- `Id` (Guid): Unique identifier
- `Email` (Email VO): User's email address (unique)
- `Name` (string): Display name
- `PasswordHash` (string): Hashed password
- `CreatedAt` (DateTimeOffset): Account creation timestamp

**Invariants**:
- Email must be unique across all users
- Email must be valid format
- Name cannot be empty or whitespace
- Password must meet minimum complexity requirements (handled by Identity)

**Domain Events**:
- `UserRegistered`: When a new user signs up
- `UserLoggedIn`: When user authenticates

---

### Org (Aggregate Root)
**Purpose**: Represents an organization that owns documents and files.

**Properties**:
- `Id` (Guid): Unique identifier
- `Name` (string): Organization name
- `OwnerId` (Guid): Reference to User who owns this org
- `CreatedAt` (DateTimeOffset): Creation timestamp
- `Members` (Collection<Member>): Organization members

**Invariants**:
- Name cannot be empty or whitespace
- Owner must always be a member with role "owner"
- Must have exactly one owner
- Cannot delete owner without transferring ownership first
- Member uniqueness: (OrgId, UserId) must be unique

**Business Rules**:
- When Org is created, owner is automatically added as member with role "owner"
- Owner can invite new members
- Owner and admin can change member roles (except cannot demote the owner)
- Only owner can delete the org

**Domain Events**:
- `OrgCreated`: When organization is established
- `MemberInvited`: When a member is added
- `MemberRoleChanged`: When member role is updated
- `MemberRemoved`: When member leaves or is removed

---

### Member (Entity, child of Org)
**Purpose**: Represents a user's membership in an organization.

**Properties**:
- `OrgId` (Guid): Parent organization
- `UserId` (Guid): Member's user ID
- `Role` (OrgRole VO): Member's role (owner, admin, member)
- `JoinedAt` (DateTimeOffset): When user joined

**Invariants**:
- Must reference existing Org and User
- Role must be valid (owner, admin, member)
- Composite key: (OrgId, UserId) must be unique

---

## Value Objects

### Email
**Purpose**: Ensures email validity and normalization.

**Properties**:
- `Value` (string): Normalized email address

**Validation**:
- Must match email format (using MailAddress parsing)
- Normalized to lowercase
- Leading/trailing whitespace trimmed

**Equality**: By value

---

### OrgRole
**Purpose**: Represents member role with associated permissions.

**Values**:
- `Owner`: Full control, can delete org, manage all members
- `Admin`: Can invite members, manage pages, but cannot delete org
- `Member`: Can view and edit pages based on ACLs

**Validation**:
- Must be one of the three valid values

**Equality**: By value

---

## Domain Services

### OrgMembershipService
**Purpose**: Coordinates complex membership operations.

**Operations**:
- `TransferOwnership(Org org, Guid newOwnerId)`: Changes org owner
  - Validates new owner is already a member
  - Demotes old owner to admin
  - Promotes new owner to owner role

- `RemoveMember(Org org, Guid memberId)`: Removes member safely
  - Prevents removing the owner (must transfer first)
  - Validates member exists

---

## Integration Points

**Outbound**:
- `IUserRepository`: Persistence for User aggregate
- `IOrgRepository`: Persistence for Org aggregate
- `IAuthorizationService`: Creates resources and ACLs when org is created

**Inbound**:
- Authentication endpoints use User for login
- All other contexts reference User and Org by ID

---

## Testing Strategy

### Domain Tests
- Email VO validation (valid formats, normalization)
- OrgRole enum validation
- Org invariants: owner must be member, unique members
- Member uniqueness enforcement

### Use Case Tests
- CreateOrg: owner auto-added
- InviteMember: role assignment, duplicate prevention
- TransferOwnership: role transitions
- RemoveMember: owner protection

### Integration Tests
- EF Core mapping for User and Org
- Repository save/load with members collection
- Concurrent member additions (race conditions)
