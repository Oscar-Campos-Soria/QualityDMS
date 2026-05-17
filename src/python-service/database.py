import os
import pyodbc
from motor.motor_asyncio import AsyncIOMotorClient
from dotenv import load_dotenv

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
MONGO_URL  = os.getenv("MONGO_URL", "mongodb://localhost:27017/")
mongo_client = AsyncIOMotorClient(MONGO_URL)
db         = mongo_client["dms_metadata"]
collection = db["file_tags"]


def get_collection(name: str):
    return db[name]
