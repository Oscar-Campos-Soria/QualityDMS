import logging
import mimetypes
import os
from pathlib import Path

logger = logging.getLogger(__name__)

STORAGE_ROOT = os.getenv("DMS_STORAGE_PATH", "/app/uploads")
MAX_CONTENT_CHARS = 500_000

_MIME_MAP = {
    ".pdf":  "application/pdf",
    ".docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    ".txt":  "text/plain",
    ".xlsx": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
}


def resolve_path(file_url: str) -> str:
    return os.path.join(STORAGE_ROOT, file_url.lstrip("/").replace("\\", "/"))


def get_file_info(file_url: str) -> dict:
    if not file_url:
        return {}
    full_path = resolve_path(file_url)
    p = Path(full_path)
    ext = p.suffix.lower()
    size = 0
    try:
        size = p.stat().st_size
    except OSError:
        pass
    return {
        "file_name": p.name,
        "extension": ext,
        "mime_type": _MIME_MAP.get(ext) or mimetypes.guess_type(str(p))[0] or "application/octet-stream",
        "size":      size,
        "path":      file_url,
    }


def extract_text(file_url: str) -> tuple[str, str | None]:
    """Returns (text, error_or_None). Never raises."""
    if not file_url:
        return "", "no file_url provided"
    full_path = resolve_path(file_url)
    if not os.path.isfile(full_path):
        return "", f"file not found: {full_path}"
    ext = Path(full_path).suffix.lower()
    try:
        if ext == ".pdf":
            return _pdf(full_path), None
        if ext == ".docx":
            return _docx(full_path), None
        if ext == ".txt":
            return _txt(full_path), None
        if ext == ".xlsx":
            return _xlsx(full_path), None
        return "", f"unsupported extension: {ext}"
    except Exception as exc:
        logger.warning("Text extraction failed [%s]: %s", file_url, exc)
        return "", str(exc)


def _pdf(path: str) -> str:
    import fitz  # PyMuPDF
    with fitz.open(path) as doc:
        return "\n".join(page.get_text() for page in doc)


def _docx(path: str) -> str:
    from docx import Document
    doc = Document(path)
    return "\n".join(p.text for p in doc.paragraphs if p.text.strip())


def _txt(path: str) -> str:
    with open(path, "r", encoding="utf-8", errors="ignore") as f:
        return f.read()


def _xlsx(path: str) -> str:
    from openpyxl import load_workbook
    wb = load_workbook(path, read_only=True, data_only=True)
    parts = []
    for ws in wb.worksheets:
        for row in ws.iter_rows(values_only=True):
            line = " ".join(str(c) for c in row if c is not None)
            if line.strip():
                parts.append(line)
    return "\n".join(parts)
