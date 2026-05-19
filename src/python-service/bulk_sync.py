import asyncio
import logging
from datetime import datetime, timezone
from typing import Optional

from database import get_sql_connection, collection, get_collection
from extractor import MAX_CONTENT_CHARS, extract_text, get_file_info
from models import PublicDMSMetadata

logger = logging.getLogger(__name__)

_state_col = get_collection("sync_state")
_STATE_ID  = "fastapi_auto_sync"

_SQL_BASE = """
    SELECT
        d.DocumentId, d.Code, d.Title,
        d.IsActive, d.CreatedAt, d.UpdatedAt,
        c.Name   AS Category,
        dep.Name AS Department,
        dv.VersionNumber, dv.FilePath
    FROM Documents d
    INNER JOIN DocumentCategories c   ON d.CategoryId   = c.CategoryId
    INNER JOIN Departments dep        ON d.DepartmentId = dep.DepartmentId
    INNER JOIN DocumentVersions dv    ON d.DocumentId   = dv.DocumentId
    WHERE d.CurrentStatus = 3 AND dv.IsCurrent = 1
"""

_SQL_INCREMENTAL = _SQL_BASE + """
    AND (d.UpdatedAt >= ? OR d.CreatedAt >= ? OR dv.CreatedAt >= ?)
"""


def _fetch_records(since: Optional[datetime] = None):
    conn = get_sql_connection()
    try:
        cursor = conn.cursor()
        if since:
            s = since.strftime("%Y-%m-%d %H:%M:%S")
            logger.info(f"Incremental sync desde {s}")
            cursor.execute(_SQL_INCREMENTAL, s, s, s)
        else:
            logger.info("Bulk sync completo")
            cursor.execute(_SQL_BASE)
        return cursor.fetchall()
    finally:
        conn.close()


async def _get_last_sync() -> Optional[datetime]:
    doc = await _state_col.find_one({"_id": _STATE_ID})
    return doc.get("last_sync") if doc else None


async def _set_last_sync(ts: datetime):
    await _state_col.update_one(
        {"_id": _STATE_ID},
        {"$set": {"last_sync": ts}},
        upsert=True,
    )


async def _index_rows(rows) -> int:
    count = 0
    now   = datetime.now(timezone.utc).isoformat()
    for row in rows:
        path = row.FilePath.replace("\\", "/").lstrip("/") if row.FilePath else ""

        file_info = get_file_info(path)
        content, extraction_error = await asyncio.to_thread(extract_text, path)
        if extraction_error:
            logger.warning("Extraction [%s]: %s", path, extraction_error)

        created_at = row.CreatedAt.isoformat() if getattr(row, "CreatedAt", None) else None
        updated_at = row.UpdatedAt.isoformat() if getattr(row, "UpdatedAt", None) else None

        payload = {
            # backward-compat keys (search & PHP still use these)
            "postgres_id":      str(row.DocumentId),
            "code":             row.Code,
            "title":            row.Title,
            "category_name":    row.Category,
            "department_name":  row.Department,
            "version":          str(row.VersionNumber),
            "file_url":         path,
            "is_active":        bool(row.IsActive),
            # document identity
            "document_id":      str(row.DocumentId),
            "created_at":       created_at,
            "updated_at":       updated_at,
            "uploaded_by":      "",
            # structured metadata
            "metadata": {
                "department":   row.Department,
                "tags":         [],
                "version":      str(row.VersionNumber),
            },
            # file info (file_name, extension, mime_type, size, path)
            **file_info,
            # extracted content
            "content":                  content[:MAX_CONTENT_CHARS],
            "content_extracted":        extraction_error is None,
            "content_extraction_error": extraction_error,
            "sync_date":                now,
        }

        await collection.update_one(
            {"postgres_id": payload["postgres_id"]},
            {"$set": payload},
            upsert=True,
        )
        count += 1
        if count % 100 == 0:
            logger.info(f"Indexados: {count}/{len(rows)}")
    return count


async def run_incremental_sync() -> int:
    """Lee solo docs nuevos/modificados desde último sync exitoso."""
    since    = await _get_last_sync()
    started  = datetime.now(timezone.utc)
    rows     = await asyncio.to_thread(_fetch_records, since)
    if not rows:
        return 0
    count = await _index_rows(rows)
    await _set_last_sync(started)
    logger.info(f"Incremental sync: {count} docs indexados")
    return count


def _fetch_by_id(document_id: int):
    conn = get_sql_connection()
    try:
        cursor = conn.cursor()
        cursor.execute(_SQL_BASE + " AND d.DocumentId = ?", document_id)
        return cursor.fetchall()
    finally:
        conn.close()


async def sync_single_document(document_id: int) -> bool:
    """Index one document immediately — called by webhook on approval."""
    try:
        rows = await asyncio.to_thread(_fetch_by_id, document_id)
        if not rows:
            logger.warning(f"Document {document_id} not found or not approved in SQL Server")
            return False
        await _index_rows(rows)
        logger.info(f"Webhook sync: document {document_id} indexed")
        return True
    except Exception as e:
        logger.error(f"Webhook sync failed for document {document_id}: {e}", exc_info=True)
        return False


async def run_bulk_sync_from_sql() -> int:
    """Re-indexa TODOS los docs aprobados. Útil para recovery."""
    logger.info("=== Bulk Sync iniciado ===")
    try:
        rows = await asyncio.to_thread(_fetch_records, None)
        if not rows:
            logger.warning("SQL Server devolvio 0 registros.")
            return 0
        count = await _index_rows(rows)
        await _set_last_sync(datetime.now(timezone.utc))
        logger.info(f"=== Bulk Sync completo: {count} ===")
        return count
    except Exception as e:
        logger.error(f"Bulk Sync fallo: {e}", exc_info=True)
        raise
