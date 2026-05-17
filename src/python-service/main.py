from fastapi import FastAPI, BackgroundTasks, HTTPException
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import JSONResponse
from database import collection
from bulk_sync import run_bulk_sync_from_sql
from routes.indexer import router as indexer_router
from pymongo import TEXT
import logging
import os

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="DMS Search Engine",
    description="Motor de búsqueda para documentos aprobados",
    version="1.3.0",
)

# ── API Key middleware ────────────────────────────────────────────────────────
_API_KEY = os.getenv("FASTAPI_API_KEY", "")
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


@app.on_event("startup")
async def create_search_indexes():
    try:
        logger.info("Verificando índices en MongoDB...")
        existing_indexes = await collection.index_information()

        for name, details in existing_indexes.items():
            is_text_index = any("_fts" in str(val) for val in details.get("key", []))
            if is_text_index and name != "dms_master_index":
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
        logger.error(f"Error al configurar índices: {e}")


@app.get("/", tags=["General"])
@app.get("/health", tags=["General"])
async def health_check():
    return {"status": "online", "engine": "FastAPI + MongoDB Text Search"}


@app.post("/sync/start", tags=["Sync"])
async def start_sync(background_tasks: BackgroundTasks):
    try:
        background_tasks.add_task(run_bulk_sync_from_sql)
        return {
            "message": "Sincronización iniciada en segundo plano",
            "source":  "SQL Server (QualityDMS)",
            "target":  "MongoDB (file_tags)",
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
