-- PostgreSQL — PublicDMS schema
CREATE SCHEMA IF NOT EXISTS publicdms;

CREATE TABLE IF NOT EXISTS publicdms.categories (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS publicdms.departments (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS publicdms.documents (
    id              INTEGER PRIMARY KEY,
    code            VARCHAR(50)  NOT NULL,
    title           VARCHAR(500) NOT NULL,
    category_id     INTEGER REFERENCES publicdms.categories(id),
    category_name   VARCHAR(150),
    department_id   INTEGER REFERENCES publicdms.departments(id),
    department_name VARCHAR(150),
    version         VARCHAR(50),
    file_url        TEXT,
    local_file_name VARCHAR(255),
    is_active       BOOLEAN DEFAULT TRUE,
    effective_date  TIMESTAMP,
    expiration_date TIMESTAMP,
    last_sync       TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS publicdms.sync_log (
    id           SERIAL PRIMARY KEY,
    entity       VARCHAR(100),
    action       VARCHAR(100),
    processed_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_docs_active    ON publicdms.documents(is_active);
CREATE INDEX IF NOT EXISTS idx_docs_category  ON publicdms.documents(category_id);
CREATE INDEX IF NOT EXISTS idx_docs_dept      ON publicdms.documents(department_id);
CREATE INDEX IF NOT EXISTS idx_docs_last_sync ON publicdms.documents(last_sync);
