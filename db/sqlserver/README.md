# SQL Server Schema

SQL Server (QualityDMS) is managed by CalidadSYS via Entity Framework Core migrations.

To export the current schema from a running instance:

```bash
# Con sqlcmd dentro del contenedor
docker exec -it dms_sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "SCRIPT DATABASE QualityDMS" \
  -o /tmp/schema.sql
```

Or use SQL Server Management Studio (SSMS) → Right-click DB → Tasks → Generate Scripts.
