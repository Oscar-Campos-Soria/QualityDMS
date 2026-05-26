import asyncio
import logging
from database import get_sql_connection, collection
from models import PublicDMSMetadata
from datetime import datetime, timezone

logger = logging.getLogger(__name__)

SQL_QUERY = """
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


def _fetch_sql_records():
    conn = get_sql_connection()
    try:
        cursor = conn.cursor()
        logger.info("Ejecutando query en SQL Server...")
        cursor.execute(SQL_QUERY)
        rows = cursor.fetchall()
        logger.info(f"SQL Server devolvio {len(rows)} registros")
        return rows
    finally:
        conn.close()


async def run_bulk_sync_from_sql():
    logger.info("=== Bulk Sync iniciado ===")
    try:
        rows = await asyncio.to_thread(_fetch_sql_records)

        if not rows:
            logger.warning("SQL Server devolvio 0 registros.")
            return 0

        total = len(rows)
        count = 0

        for row in rows:
            # Ruta relativa normalizada (solo forward slashes)
            relative_path = row.FilePath.replace("\\", "/").lstrip("/") if row.FilePath else ""

            doc_data = PublicDMSMetadata(
                postgres_id     = str(row.DocumentId),
                code            = row.Code,
                title           = row.Title,
                category_name   = row.Category,
                department_name = row.Department,
                version         = str(row.VersionNumber),
                file_url        = relative_path,
                is_active       = bool(row.IsActive),
            )

            payload = doc_data.model_dump()
            payload["sync_date"] = datetime.now(timezone.utc).isoformat()

            await collection.update_one(
                {"postgres_id": doc_data.postgres_id},
                {"$set": payload},
                upsert=True,
            )
            count += 1

            if count % 500 == 0:
                logger.info(f"Progreso: {count}/{total} registros indexados")

        logger.info(f"=== Bulk Sync completo: {count}/{total} ===")
        return count

    except Exception as e:
        logger.error(f"Bulk Sync fallo: {e}", exc_info=True)
        raise
