import os
from urllib.parse import quote_plus

import pyodbc
from dotenv import load_dotenv
from motor.motor_asyncio import AsyncIOMotorClient

load_dotenv()

# --- SQL Server (Fuente de Verdad) ---
SQL_SERVER   = os.getenv("SQL_SERVER",   "localhost\\SQLEXPRESS01")
SQL_DATABASE = os.getenv("SQL_DATABASE", "QualityDMS")
SQL_USER     = os.getenv("SQL_USER")
SQL_PASSWORD = os.getenv("SQL_PASSWORD")

if SQL_USER and SQL_PASSWORD:
    SQL_CONN_STR = (
        f"DRIVER={{ODBC Driver 17 for SQL Server}};"
        f"SERVER={SQL_SERVER};"
        f"DATABASE={SQL_DATABASE};"
        f"UID={SQL_USER};"
        f"PWD={SQL_PASSWORD};"
        f"TrustServerCertificate=yes;"
    )
else:
    SQL_CONN_STR = (
        f"DRIVER={{ODBC Driver 17 for SQL Server}};"
        f"SERVER={SQL_SERVER};"
        f"DATABASE={SQL_DATABASE};"
        f"Trusted_Connection=yes;"
        f"TrustServerCertificate=yes;"
    )


def get_sql_connection():
    return pyodbc.connect(SQL_CONN_STR)


# --- MongoDB (Motor de Búsqueda) ---
_raw_mongo_url = os.getenv("MONGO_URL", "mongodb://localhost:27017/")

def _escape_mongo_url(url: str) -> str:
    """URL-encode credentials in mongodb:// URI (RFC 3986)."""
    if "://" not in url or "@" not in url:
        return url
    scheme, rest = url.split("://", 1)
    userinfo, hostpart = rest.rsplit("@", 1)
    if ":" in userinfo:
        user, passwd = userinfo.split(":", 1)
        userinfo = f"{quote_plus(user)}:{quote_plus(passwd)}"
    return f"{scheme}://{userinfo}@{hostpart}"

MONGO_URL    = _escape_mongo_url(_raw_mongo_url)
mongo_client = AsyncIOMotorClient(MONGO_URL)
db         = mongo_client["dms_metadata"]
collection = db["file_tags"]


def get_collection(name: str):
    return db[name]
