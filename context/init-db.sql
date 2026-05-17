CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ========================
-- USERS
-- ========================
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username TEXT NOT NULL,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    global_role TEXT NOT NULL, -- user | admin(hidden)
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- ========================
-- WORKSPACES
-- ========================
CREATE TABLE workspaces (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name TEXT NOT NULL,
    owner_user_id UUID NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (owner_user_id)
        REFERENCES users(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_workspace_owner ON workspaces(owner_user_id);

-- ========================
-- WORKSPACE USERS
-- ========================
CREATE TABLE workspace_users (
    user_id UUID NOT NULL,
    workspace_id UUID NOT NULL,
    role TEXT NOT NULL, -- owner | member
    joined_at TIMESTAMP DEFAULT NOW(),

    PRIMARY KEY (user_id, workspace_id),

    FOREIGN KEY (user_id)
        REFERENCES users(id)
        ON DELETE CASCADE,

    FOREIGN KEY (workspace_id)
        REFERENCES workspaces(id)
        ON DELETE CASCADE
);

-- ========================
-- FILES (logical container)
-- ========================
CREATE TABLE files (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workspace_id UUID NOT NULL,
    title TEXT NOT NULL,
    folder_path TEXT NOT NULL, -- mỗi file = 1 folder
    created_by UUID,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (workspace_id)
        REFERENCES workspaces(id)
        ON DELETE CASCADE,

    FOREIGN KEY (created_by)
        REFERENCES users(id)
        ON DELETE SET NULL
);

CREATE INDEX idx_file_workspace ON files(workspace_id);

-- ========================
-- VERSIONS (actual storage)
-- ========================
CREATE TABLE versions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    file_id UUID NOT NULL,
    version_number INT NOT NULL,
    storage_path TEXT NOT NULL,

    is_full BOOLEAN NOT NULL,

    base_version_id UUID,

    file_size BIGINT,
    checksum TEXT,

    created_by UUID,
    created_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (file_id)
        REFERENCES files(id)
        ON DELETE CASCADE,

    FOREIGN KEY (base_version_id)
        REFERENCES versions(id)
        ON DELETE SET NULL,

    FOREIGN KEY (created_by)
        REFERENCES users(id)
        ON DELETE SET NULL,

    UNIQUE (file_id, version_number)
);

CREATE INDEX idx_version_file ON versions(file_id);

-- ========================
-- COMMENTS
-- ========================
CREATE TABLE comments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    file_id UUID,
    version_id UUID,

    user_id UUID,
    content TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (file_id)
        REFERENCES files(id)
        ON DELETE CASCADE,

    FOREIGN KEY (version_id)
        REFERENCES versions(id)
        ON DELETE CASCADE,

    FOREIGN KEY (user_id)
        REFERENCES users(id)
        ON DELETE SET NULL,

    -- đảm bảo có target
    CONSTRAINT chk_comment_target
    CHECK (
        file_id IS NOT NULL OR version_id IS NOT NULL
    )
);

CREATE INDEX idx_comment_file ON comments(file_id);

-- ========================
-- FEEDBACK
-- ========================
CREATE TABLE feedbacks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID,
    content TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (user_id)
        REFERENCES users(id)
        ON DELETE SET NULL
);

-- ========================
-- ACTIVITY LOG
-- ========================
CREATE TYPE activity_action AS ENUM (
    'UPLOAD_FILE',
    'CREATE_VERSION',
    'COMMENT',
    'JOIN_WORKSPACE',
    'LEAVE_WORKSPACE'
);

CREATE TABLE activity_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID,
    workspace_id UUID,

    action activity_action NOT NULL,

    entity_type TEXT,
    entity_id UUID,

    created_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (user_id)
        REFERENCES users(id)
        ON DELETE SET NULL,

    FOREIGN KEY (workspace_id)
        REFERENCES workspaces(id)
        ON DELETE SET NULL
);

CREATE INDEX idx_log_workspace ON activity_logs(workspace_id);

-- ========================
-- PASSWORD-RESET-REQs
-- ========================

CREATE TABLE password_reset_requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    token TEXT NOT NULL,
    expired_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (user_id)
        REFERENCES users(id)
        ON DELETE CASCADE
);





-- không biết nhưng có thể sẽ xoá cái file locking này

ALTER TABLE files
ADD COLUMN locked_by UUID,
ADD COLUMN locked_at TIMESTAMP;

ALTER TABLE files
ADD CONSTRAINT fk_files_locked_user
FOREIGN KEY (locked_by) REFERENCES users(id);

ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'CREATE_WORKSPACE';                                                                                                                                  │
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'UPDATE_WORKSPACE';                                                                                                                                  │
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'DELETE_WORKSPACE';    

ALTER TABLE versions
ADD COLUMN expired_at TIMESTAMP NULL;

CREATE INDEX idx_versions_expired_not_null 
ON versions(expired_at)
WHERE expired_at IS NOT NULL;

CREATE INDEX idx_versions_base_version
ON versions(base_version_id);

-- Phase 5: Soft Delete & Trash Management
-- Thêm cột deleted_at vào bảng files để hỗ trợ thùng rác

ALTER TABLE files 
ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE NULL;

-- Index để tối ưu việc quét file trong thùng rác và lọc file active
CREATE INDEX idx_files_deleted_at ON files(deleted_at) WHERE deleted_at IS NOT NULL;
CREATE INDEX idx_files_not_deleted ON files(workspace_id) WHERE deleted_at IS NULL;


ALTER TABLE comments 
ADD COLUMN updated_at TIMESTAMP DEFAULT NOW();

-- Hỗ trợ Soft Delete cho bình luận
ALTER TABLE comments
ADD COLUMN deleted_at TIMESTAMP NULL;

-- Tối ưu truy vấn tìm comment theo version
CREATE INDEX idx_comment_version ON comments(version_id);

-- Tối ưu truy vấn bỏ qua các bình luận đã bị xóa mềm
CREATE INDEX idx_comments_deleted_at ON comments(deleted_at) WHERE deleted_at IS NOT NULL;

-- ==========================================
-- Phase 9: Mời & Tham gia / Rời Workspace (UC8)
-- ==========================================
ALTER TABLE workspaces
ADD COLUMN invite_code TEXT UNIQUE,
ADD COLUMN invite_enabled BOOLEAN DEFAULT TRUE;

CREATE TABLE workspace_invitations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    workspace_id UUID NOT NULL,
    email TEXT NOT NULL,
    token TEXT NOT NULL,
    expired_at TIMESTAMP NOT NULL,
    used_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (workspace_id)
        REFERENCES workspaces(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_invite_token ON workspace_invitations(token);

-- ==========================================
-- Phase 10: Feedback (UC6)
-- ==========================================
ALTER TABLE feedbacks
ADD COLUMN status TEXT DEFAULT 'open';

-- ==========================================
-- Phase 12: Tìm kiếm File (UC*.1)
-- ==========================================
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX idx_files_title_trgm
ON files USING gin (title gin_trgm_ops);

-- ==========================================
-- Phase 13: Activity Logs (UC*.2)
-- ==========================================
ALTER TABLE activity_logs
ADD COLUMN entity_name TEXT;

ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'DELETE_FILE';
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'RESTORE_VERSION';
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'UPDATE_PROFILE';
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'CHANGE_PASSWORD';

-- ==========================================
-- Global Fix (BẮT BUỘC)
-- ==========================================
-- Soft Delete Consistency (files might already have it from Phase 5, using a safe approach)
ALTER TABLE files ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP;
ALTER TABLE versions ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP;

-- Comment Constraint Fix
ALTER TABLE comments DROP CONSTRAINT chk_comment_target;

ALTER TABLE comments ADD CONSTRAINT chk_comment_target
CHECK (
    (file_id IS NOT NULL AND version_id IS NULL)
    OR
    (file_id IS NULL AND version_id IS NOT NULL)
);

-- Phase 12: File Search Optimization

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;

ALTER TABLE files 
ADD COLUMN IF NOT EXISTS search_vector TSVECTOR;

CREATE INDEX IF NOT EXISTS idx_files_title_trgm 
ON files USING gin (title gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_files_content_fts 
ON files USING gin (search_vector);

-- Optional Trigger (fallback title search)
CREATE OR REPLACE FUNCTION update_search_vector()
RETURNS trigger AS $$
BEGIN
  NEW.search_vector :=
    to_tsvector('simple', unaccent(COALESCE(NEW.title, '') || ' '));
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_search_vector ON files;

CREATE TRIGGER trg_update_search_vector
BEFORE INSERT OR UPDATE OF title ON files
FOR EACH ROW
EXECUTE FUNCTION update_search_vector();

-- Phase 13 & 14 & Global Fixes Final Update

-- 1. Phase 13: Activity Logs Improvements
ALTER TABLE activity_logs
ADD COLUMN IF NOT EXISTS entity_name TEXT;

-- Mở rộng Enum
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'DELETE_FILE';
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'RESTORE_VERSION';
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'UPDATE_PROFILE';
ALTER TYPE activity_action ADD VALUE IF NOT EXISTS 'CHANGE_PASSWORD';

-- 2. Phase 14: Reset Password
CREATE TABLE IF NOT EXISTS password_reset_requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    token TEXT NOT NULL,
    expired_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),

    FOREIGN KEY (user_id)
        REFERENCES users(id)
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_password_reset_token ON password_reset_requests(token);

-- 3. Global Fix: Comment Constraint Bug (XOR Logic)
ALTER TABLE comments DROP CONSTRAINT IF EXISTS chk_comment_target;

ALTER TABLE comments
ADD CONSTRAINT chk_comment_target
CHECK (
    (file_id IS NOT NULL AND version_id IS NULL)
    OR
    (file_id IS NULL AND version_id IS NOT NULL)
);

-- ==========================================
-- Phase 15: Admin Features
-- ==========================================

-- Bổ sung tính năng Khóa tài khoản
ALTER TABLE users
ADD COLUMN IF NOT EXISTS is_locked BOOLEAN DEFAULT FALSE;

-- Index hỗ trợ query nhanh danh sách user theo trạng thái khóa (nếu cần)
CREATE INDEX IF NOT EXISTS idx_users_is_locked ON users(is_locked) WHERE is_locked = TRUE;


ALTER TABLE users 
ADD COLUMN IF NOT EXISTS preferences JSONB DEFAULT '{}'::jsonb;

-- ==========================================
-- Refinement: User Profile Extension
-- ==========================================
ALTER TABLE users
ADD COLUMN IF NOT EXISTS bio TEXT,
ADD COLUMN IF NOT EXISTS role TEXT,
ADD COLUMN IF NOT EXISTS team TEXT,
ADD COLUMN IF NOT EXISTS division TEXT;


