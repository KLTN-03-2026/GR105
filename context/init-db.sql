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