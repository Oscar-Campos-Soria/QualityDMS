import asyncio
import logging
from datetime import datetime, timezone
from typing import Optional

from database import get_sql_connection, collection, get_collection
from models import PublicDMSMetadata

logger = logging.getLogger(__name__)

_state_col = get_collection("sync_state")
_STATE_ID  = "fastapi_auto_sync"

_SQL_BASE = """
    SELECT
        d.DocumentId, d.Code, d.Title,
        d.IsActive,
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
        payload = PublicDMSMetadata(
            postgres_id     = str(row.DocumentId),
            code            = row.Code,
            title           = row.Title,
            category_name   = row.Category,
            department_name = row.Department,
            version         = str(row.VersionNumber),
            file_url        = path,
            is_active       = bool(row.IsActive),
        ).model_dump()
        payload["sync_date"] = now
        await collection.update_one(
            {"postgres_id": payload["postgres_id"]},
            {"$set": payload},
            upsert=True,
        )
        count += 1
        if count % 500 == 0:
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
