from pydantic import BaseModel, Field
from typing import Optional, Dict, Any
from datetime import datetime


class DocumentMetadata(BaseModel):
    doc_id: int
    code: str
    title: str
    category_name: str
    department_name: str
    version: str
    file_url: Optional[str] = None
    is_active: bool = True
    indexed_at: datetime = Field(default_factory=datetime.utcnow)
    extra_info: Dict[str, Any] = Field(default_factory=dict)


class PublicDMSMetadata(BaseModel):
    postgres_id: str
    code: str
    title: str
    category_name: str
    department_name: str
    version: str = "1.0"
    is_active: bool = True
    file_url: str = ""
