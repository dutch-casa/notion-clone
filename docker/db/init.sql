-- Database Initialization Script
-- Collaborative Notion-like Teaching Reference
-- PostgreSQL 16

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================================================
-- IDENTITY & ORGANIZATION CONTEXT
-- =============================================================================

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT users_email_not_empty CHECK (email <> ''),
    CONSTRAINT users_name_not_empty CHECK (name <> '')
);

CREATE INDEX idx_users_email ON users(email);

CREATE TABLE orgs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    owner_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT orgs_name_not_empty CHECK (name <> '')
);

CREATE INDEX idx_orgs_owner ON orgs(owner_id);

CREATE TABLE members (
    org_id UUID NOT NULL REFERENCES orgs(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL CHECK (role IN ('owner', 'admin', 'member')),
    joined_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (org_id, user_id)
);

CREATE INDEX idx_members_user ON members(user_id);
CREATE INDEX idx_members_org_role ON members(org_id, role);

-- =============================================================================
-- AUTHORIZATION CONTEXT
-- =============================================================================

CREATE TABLE resources (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    type VARCHAR(50) NOT NULL CHECK (type IN ('org', 'page', 'file')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_resources_type ON resources(type);

CREATE TABLE acls (
    resource_id UUID NOT NULL REFERENCES resources(id) ON DELETE CASCADE,
    subject_type VARCHAR(50) NOT NULL CHECK (subject_type IN ('user', 'org', 'role')),
    subject_id VARCHAR(255) NOT NULL,
    capability VARCHAR(50) NOT NULL CHECK (capability IN ('view', 'comment', 'edit', 'admin')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (resource_id, subject_type, subject_id, capability)
);

CREATE INDEX idx_acls_subject ON acls(subject_type, subject_id);
CREATE INDEX idx_acls_resource_capability ON acls(resource_id, capability);

CREATE TABLE share_links (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    resource_id UUID NOT NULL REFERENCES resources(id) ON DELETE CASCADE,
    capability VARCHAR(50) NOT NULL CHECK (capability IN ('view', 'comment', 'edit', 'admin')),
    token_hash VARCHAR(64) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NULL,
    created_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT share_links_expires_in_future CHECK (expires_at IS NULL OR expires_at > created_at)
);

CREATE INDEX idx_share_links_token_hash ON share_links(token_hash);
CREATE INDEX idx_share_links_resource ON share_links(resource_id);
CREATE INDEX idx_share_links_expires ON share_links(expires_at) WHERE expires_at IS NOT NULL;

-- =============================================================================
-- DOCUMENTS CONTEXT
-- =============================================================================

CREATE TABLE pages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    org_id UUID NOT NULL REFERENCES orgs(id) ON DELETE CASCADE,
    title VARCHAR(500) NOT NULL,
    created_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT pages_title_not_empty CHECK (title <> '')
);

CREATE INDEX idx_pages_org ON pages(org_id);
CREATE INDEX idx_pages_created_by ON pages(created_by);
CREATE INDEX idx_pages_org_created ON pages(org_id, created_at DESC);

CREATE TABLE blocks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    page_id UUID NOT NULL REFERENCES pages(id) ON DELETE CASCADE,
    parent_block_id UUID NULL REFERENCES blocks(id) ON DELETE CASCADE,
    sort_key NUMERIC(18,9) NOT NULL,
    type VARCHAR(50) NOT NULL CHECK (type IN ('paragraph', 'heading', 'todo', 'file')),
    json JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT blocks_sort_key_positive CHECK (sort_key > 0)
);

-- Critical indexes for block queries
CREATE INDEX idx_blocks_page ON blocks(page_id);
CREATE INDEX idx_blocks_page_sort ON blocks(page_id, sort_key);
CREATE INDEX idx_blocks_parent ON blocks(parent_block_id) WHERE parent_block_id IS NOT NULL;
CREATE INDEX idx_blocks_type ON blocks(type);

-- Prevent cycles: A block cannot be its own ancestor (enforced in application layer)
-- Unique sort_key among siblings (same parent)
CREATE UNIQUE INDEX idx_blocks_page_parent_sort
    ON blocks(page_id, COALESCE(parent_block_id, '00000000-0000-0000-0000-000000000000'::uuid), sort_key);

CREATE TABLE doc_states (
    page_id UUID NOT NULL REFERENCES pages(id) ON DELETE CASCADE,
    seq BIGINT NOT NULL,
    crdt_blob BYTEA NOT NULL,
    is_snapshot BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (page_id, seq),

    CONSTRAINT doc_states_seq_positive CHECK (seq > 0)
);

CREATE INDEX idx_doc_states_page_seq ON doc_states(page_id, seq DESC);
CREATE INDEX idx_doc_states_page_snapshot ON doc_states(page_id, is_snapshot) WHERE is_snapshot = TRUE;

-- =============================================================================
-- FILES CONTEXT
-- =============================================================================

CREATE TABLE files (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    org_id UUID NOT NULL REFERENCES orgs(id) ON DELETE CASCADE,
    owner_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    key VARCHAR(1024) NOT NULL UNIQUE,
    mime VARCHAR(255) NOT NULL,
    size BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT files_key_not_empty CHECK (key <> ''),
    CONSTRAINT files_mime_not_empty CHECK (mime <> ''),
    CONSTRAINT files_size_positive CHECK (size > 0)
);

CREATE INDEX idx_files_org ON files(org_id);
CREATE INDEX idx_files_owner ON files(owner_id);
CREATE INDEX idx_files_org_created ON files(org_id, created_at DESC);
CREATE INDEX idx_files_key ON files(key);

CREATE TABLE file_blocks (
    block_id UUID NOT NULL REFERENCES blocks(id) ON DELETE CASCADE,
    file_id UUID NOT NULL REFERENCES files(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (block_id, file_id)
);

CREATE INDEX idx_file_blocks_file ON file_blocks(file_id);

-- =============================================================================
-- SEED DATA FOR DEVELOPMENT
-- =============================================================================

-- Insert test users (passwords are "password123" hashed with bcrypt)
-- Note: In real app, use ASP.NET Core Identity for password hashing
INSERT INTO users (id, email, name, password_hash) VALUES
    ('11111111-1111-1111-1111-111111111111', 'alice@example.com', 'Alice Admin', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36YE0p3XfG9YFJD8.5LZl8O'),
    ('22222222-2222-2222-2222-222222222222', 'bob@example.com', 'Bob Builder', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36YE0p3XfG9YFJD8.5LZl8O'),
    ('33333333-3333-3333-3333-333333333333', 'charlie@example.com', 'Charlie Collaborator', '$2a$11$EixZaYVK1fsbw1ZfbX3OXePaWxn96p36YE0p3XfG9YFJD8.5LZl8O')
ON CONFLICT (id) DO NOTHING;

-- Insert teaching org
INSERT INTO orgs (id, name, owner_id) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Teaching Organization', '11111111-1111-1111-1111-111111111111')
ON CONFLICT (id) DO NOTHING;

-- Insert members
INSERT INTO members (org_id, user_id, role) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'owner'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '22222222-2222-2222-2222-222222222222', 'admin'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '33333333-3333-3333-3333-333333333333', 'member')
ON CONFLICT (org_id, user_id) DO NOTHING;

-- Insert resources for org
INSERT INTO resources (id, type) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'org')
ON CONFLICT (id) DO NOTHING;

-- Insert ACLs for org (owner gets admin)
INSERT INTO acls (resource_id, subject_type, subject_id, capability) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'user', '11111111-1111-1111-1111-111111111111', 'admin'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'user', '22222222-2222-2222-2222-222222222222', 'edit'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'user', '33333333-3333-3333-3333-333333333333', 'view')
ON CONFLICT (resource_id, subject_type, subject_id, capability) DO NOTHING;

-- Insert sample pages
INSERT INTO pages (id, org_id, title, created_by) VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Welcome to the Teaching Reference', '11111111-1111-1111-1111-111111111111'),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Course Syllabus', '11111111-1111-1111-1111-111111111111')
ON CONFLICT (id) DO NOTHING;

-- Insert resources for pages
INSERT INTO resources (id, type) VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'page'),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'page')
ON CONFLICT (id) DO NOTHING;

-- Insert ACLs for pages
INSERT INTO acls (resource_id, subject_type, subject_id, capability) VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'user', '11111111-1111-1111-1111-111111111111', 'admin'),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'org', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'edit'),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'user', '11111111-1111-1111-1111-111111111111', 'admin'),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'org', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'view')
ON CONFLICT (resource_id, subject_type, subject_id, capability) DO NOTHING;

-- Insert sample blocks for Welcome page
INSERT INTO blocks (id, page_id, parent_block_id, sort_key, type, json) VALUES
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NULL, 1.000000000, 'heading', '{"level": 1}'),
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NULL, 2.000000000, 'paragraph', '{}'),
    ('ffffffff-ffff-ffff-ffff-ffffffffffff', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', NULL, 3.000000000, 'todo', '{"checked": false}')
ON CONFLICT (id) DO NOTHING;

-- Insert initial CRDT snapshots for pages (empty YDoc state)
-- Note: In real app, this will be generated by YDotNet on page creation
INSERT INTO doc_states (page_id, seq, crdt_blob, is_snapshot) VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 1, E'\\x00', TRUE),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 1, E'\\x00', TRUE)
ON CONFLICT (page_id, seq) DO NOTHING;

-- =============================================================================
-- UTILITY FUNCTIONS
-- =============================================================================

-- Function to generate fractional sort key between two values
CREATE OR REPLACE FUNCTION generate_sort_key(before_key NUMERIC, after_key NUMERIC)
RETURNS NUMERIC AS $$
BEGIN
    IF before_key IS NULL AND after_key IS NULL THEN
        RETURN 1.000000000;
    ELSIF before_key IS NULL THEN
        RETURN after_key / 2.0;
    ELSIF after_key IS NULL THEN
        RETURN before_key + 1.000000000;
    ELSE
        RETURN (before_key + after_key) / 2.0;
    END IF;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Function to check for block cycles (used in application layer for validation)
CREATE OR REPLACE FUNCTION has_block_cycle(check_block_id UUID, new_parent_id UUID)
RETURNS BOOLEAN AS $$
DECLARE
    current_parent UUID;
    max_depth INT := 100;
    depth INT := 0;
BEGIN
    IF new_parent_id IS NULL THEN
        RETURN FALSE;
    END IF;

    IF check_block_id = new_parent_id THEN
        RETURN TRUE;
    END IF;

    current_parent := new_parent_id;

    WHILE current_parent IS NOT NULL AND depth < max_depth LOOP
        IF current_parent = check_block_id THEN
            RETURN TRUE;
        END IF;

        SELECT parent_block_id INTO current_parent
        FROM blocks
        WHERE id = current_parent;

        depth := depth + 1;
    END LOOP;

    RETURN FALSE;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- COMMENTS FOR TEACHING
-- =============================================================================

COMMENT ON TABLE users IS 'User accounts (IdentityOrg context)';
COMMENT ON TABLE orgs IS 'Organizations owning documents and files (IdentityOrg context)';
COMMENT ON TABLE members IS 'Organization memberships with roles (IdentityOrg context)';
COMMENT ON TABLE resources IS 'Generic resources for ACL system (Authorization context)';
COMMENT ON TABLE acls IS 'Access Control Lists: subject + capability on resource (Authorization context)';
COMMENT ON TABLE share_links IS 'Token-based share links with expiry (Authorization context)';
COMMENT ON TABLE pages IS 'Document pages (Documents context)';
COMMENT ON TABLE blocks IS 'Block metadata with fractional sort keys (Documents context)';
COMMENT ON TABLE doc_states IS 'Append-only CRDT update log with snapshots (Documents context)';
COMMENT ON TABLE files IS 'File metadata for S3-stored objects (Files context)';
COMMENT ON TABLE file_blocks IS 'Many-to-many junction for file attachments (Files context)';

COMMENT ON COLUMN blocks.sort_key IS 'Fractional index for O(1) reordering: NUMERIC(18,9) allows 999999999.999999999 positions';
COMMENT ON COLUMN blocks.json IS 'Block-specific attributes: {"level":1-3} for heading, {"checked":true/false} for todo';
COMMENT ON COLUMN doc_states.is_snapshot IS 'TRUE if this row is a compacted snapshot, FALSE if delta';
COMMENT ON COLUMN doc_states.seq IS 'Monotonic sequence per page for idempotency and ordering';
COMMENT ON COLUMN share_links.token_hash IS 'SHA256 hash of cleartext token (cleartext never stored)';
COMMENT ON COLUMN files.key IS 'S3 object key: {orgId}/{fileId}/{filename}';

-- =============================================================================
-- STATISTICS FOR QUERY OPTIMIZATION
-- =============================================================================

-- Analyze tables for optimal query planning
ANALYZE users;
ANALYZE orgs;
ANALYZE members;
ANALYZE resources;
ANALYZE acls;
ANALYZE share_links;
ANALYZE pages;
ANALYZE blocks;
ANALYZE doc_states;
ANALYZE files;
ANALYZE file_blocks;

-- =============================================================================
-- END OF INITIALIZATION
-- =============================================================================

-- Display summary
DO $$
BEGIN
    RAISE NOTICE 'Database initialized successfully!';
    RAISE NOTICE 'Sample users: alice@example.com, bob@example.com, charlie@example.com (password: password123)';
    RAISE NOTICE 'Sample org: Teaching Organization';
    RAISE NOTICE 'Sample pages: Welcome to the Teaching Reference, Course Syllabus';
END $$;
