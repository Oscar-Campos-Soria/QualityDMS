from fastapi import APIRouter, HTTPException, Query
from datetime import datetime, timezone
from models import PublicDMSMetadata
from database import collection

router = APIRouter(prefix="/indexer", tags=["Indexer"])


@router.post("/upsert")
async def upsert_document(metadata: PublicDMSMetadata):
    try:
        doc_data = metadata.model_dump()
        doc_data["sync_date"] = datetime.now(timezone.utc).isoformat()

        result = await collection.update_one(
            {"postgres_id": metadata.postgres_id},
            {"$set": doc_data},
            upsert=True,
        )

        return {
            "status":      "success",
            "postgres_id": metadata.postgres_id,
            "action":      "updated" if result.matched_count else "indexed",
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/search")
async def search_documents(
    q:     str = Query(..., min_length=2, description="Término de búsqueda"),
    limit: int = Query(default=50, le=500),
):
    try:
        cursor = collection.find(
            {"$text": {"$search": q}},
            {"score": {"$meta": "textScore"}, "postgres_id": 1, "_id": 0},
        ).sort([("score", {"$meta": "textScore"})]).limit(limit)

        results = await cursor.to_list(length=limit)
        ids = [int(r["postgres_id"]) for r in results if r.get("postgres_id")]

        return {"ids": ids}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
