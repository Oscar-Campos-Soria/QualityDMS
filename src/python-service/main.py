import asyncio
import logging
import os

from fastapi import FastAPI, BackgroundTasks, HTTPException
from pymongo import TEXT
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import JSONResponse

from bulk_sync import run_bulk_sync_from_sql, run_incremental_sync
from database import collection
from routes.indexer import router as indexer_router

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="DMS Search Engine",
    description="Motor de búsqueda para documentos aprobados",
    version="2.0.0",
)

# ── API Key middleware ────────────────────────────────────────────────────────
_API_KEY    = os.getenv("FASTAPI_API_KEY", "")
_OPEN_PATHS = {"/", "/docs", "/openapi.json", "/redoc", "/health"}


class APIKeyMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request, call_next):
        if not _API_KEY or request.url.path in _OPEN_PATHS:
            return await call_next(request)
        if request.headers.get("X-API-Key", "") != _API_KEY:
            return JSONResponse({"detail": "Invalid or missing API key"}, status_code=401)
        return await call_next(request)


app.add_middleware(APIKeyMiddleware)
app.include_router(indexer_router)

SYNC_INTERVAL_SECONDS = int(os.getenv("SYNC_INTERVAL_SECONDS", "30"))


async def _auto_sync_loop():
    """Poller autónomo: lee SQL Server cada SYNC_INTERVAL_SECONDS e indexa en MongoDB."""
    logger.info(f"Auto-sync iniciado — intervalo: {SYNC_INTERVAL_SECONDS}s")
    await asyncio.sleep(10)  # dar tiempo al startup para terminar
    while True:
        try:
            count = await run_incremental_sync()
            if count:
                logger.info(f"Auto-sync: {count} doc(s) indexados")
        except Exception as e:
            logger.error(f"Auto-sync error: {e}", exc_info=True)
        await asyncio.sleep(SYNC_INTERVAL_SECONDS)


@app.on_event("startup")
async def on_startup():
    # Índice de texto en MongoDB
    try:
        logger.info("Verificando índices en MongoDB...")
        existing = await collection.index_information()
        for name, details in existing.items():
            is_text = any("_fts" in str(v) for v in details.get("key", []))
            if is_text and name != "dms_master_index":
                logger.info(f"Eliminando índice conflictivo: {name}")
                await collection.drop_index(name)
        await collection.create_index(
            [
                ("title",           TEXT),
                ("code",            TEXT),
                ("category_name",   TEXT),
                ("department_name", TEXT),
            ],
            name="dms_master_index",
            default_language="spanish",
        )
        logger.info("Índice 'dms_master_index' listo.")
    except Exception as e:
        logger.error(f"Error configurando índices: {e}")

    # Lanzar poller autónomo como background task
    asyncio.create_task(_auto_sync_loop())


@app.get("/", tags=["General"])
@app.get("/health", tags=["General"])
async def health_check():
    return {"status": "online", "engine": "FastAPI + MongoDB Text Search"}


@app.post("/sync/start", tags=["Sync"])
async def start_bulk_sync(background_tasks: BackgroundTasks):
    """Fuerza re-indexación completa de todos los docs aprobados."""
    try:
        background_tasks.add_task(run_bulk_sync_from_sql)
        return {
            "message": "Bulk sync iniciado en segundo plano",
            "source":  "SQL Server (QualityDMS)",
            "target":  "MongoDB (file_tags)",
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
