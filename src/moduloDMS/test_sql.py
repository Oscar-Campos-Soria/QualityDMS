"""
Script de diagnostico — correr directamente:
    python test_sql.py
No necesita FastAPI ni MongoDB activos.
"""
import pyodbc

SQL_CONN_STR = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "SERVER=localhost\\SQLEXPRESS01;"
    "DATABASE=QualityDMS;"
    "Trusted_Connection=yes;"
    "TrustServerCertificate=yes;"
)

def test_connection():
    print("\n[1] Probando conexion a SQL Server...")
    try:
        conn = pyodbc.connect(SQL_CONN_STR, timeout=10)
        print("    OK - Conexion exitosa")
        return conn
    except Exception as e:
        print(f"    ERROR: {e}")
        print("\n  Posibles causas:")
        print("  - SQLEXPRESS01 no esta corriendo (revisar Services)")
        print("  - ODBC Driver 17 no instalado (verificar con: odbcad32)")
        print("  - Usuario Windows sin permisos en QualityDMS")
        return None

def test_counts(conn):
    cursor = conn.cursor()

    print("\n[2] COUNT sin filtros (total bruto en Documents):")
    cursor.execute("SELECT COUNT(*) FROM Documents")
    total_docs = cursor.fetchone()[0]
    print(f"    Documents total: {total_docs}")

    print("\n[3] COUNT con filtros del sync (CurrentStatus=3 AND IsCurrent=1):")
    cursor.execute("""
        SELECT COUNT(*)
        FROM Documents d
        INNER JOIN DocumentVersions dv ON d.DocumentId = dv.DocumentId
        WHERE d.CurrentStatus = 3 AND dv.IsCurrent = 1
    """)
    filtered = cursor.fetchone()[0]
    print(f"    Registros con filtros activos: {filtered}")

    if filtered == 0:
        print("\n[4] Diagnostico adicional - distribucion de CurrentStatus:")
        cursor.execute("SELECT CurrentStatus, COUNT(*) as qty FROM Documents GROUP BY CurrentStatus ORDER BY qty DESC")
        for row in cursor.fetchall():
            print(f"    Status {row[0]}: {row[1]} documentos")

        print("\n[5] Diagnostico adicional - IsCurrent en DocumentVersions:")
        cursor.execute("SELECT IsCurrent, COUNT(*) as qty FROM DocumentVersions GROUP BY IsCurrent")
        for row in cursor.fetchall():
            print(f"    IsCurrent={row[0]}: {row[1]} versiones")

    print("\n[6] Verificando JOINs — tablas relacionadas existen:")
    for table in ["Documents", "DocumentCategories", "Departments", "DocumentVersions"]:
        try:
            cursor.execute(f"SELECT TOP 1 1 FROM {table}")
            cursor.fetchone()
            print(f"    {table}: OK")
        except Exception as e:
            print(f"    {table}: ERROR - {e}")

    print("\n[7] Muestra de 3 filas con el query completo del sync:")
    try:
        cursor.execute("""
            SELECT TOP 3
                d.DocumentId, d.Code, d.Title,
                c.Name as Category, dep.Name as Department,
                dv.VersionNumber, dv.FilePath
            FROM Documents d
            INNER JOIN DocumentCategories c ON d.CategoryId = c.CategoryId
            INNER JOIN Departments dep ON d.DepartmentId = dep.DepartmentId
            INNER JOIN DocumentVersions dv ON d.DocumentId = dv.DocumentId
            WHERE d.CurrentStatus = 3 AND dv.IsCurrent = 1
        """)
        rows = cursor.fetchall()
        if rows:
            for r in rows:
                print(f"    ID={r.DocumentId} | Code={r.Code} | Title={r.Title[:40]}")
        else:
            print("    Sin resultados con filtros activos.")
    except Exception as e:
        print(f"    ERROR en query completo: {e}")

if __name__ == "__main__":
    conn = test_connection()
    if conn:
        test_counts(conn)
        conn.close()
    print("\nDiagnostico completo.\n")
