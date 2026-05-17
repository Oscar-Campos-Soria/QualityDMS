# Sistema Integral de Gestión Documental (DMS)

Sistema multi-stack para gestión, aprobación y consulta pública de documentos normativos.

## Arquitectura

```
┌─────────────────────┐     ┌──────────────────────┐     ┌─────────────────────┐
│   CalidadSYS        │     │   moduloDMS           │     │   PublicDMS         │
│   ASP.NET Core 10   │────▶│   FastAPI + MongoDB   │◀────│   PHP 8 + PostgreSQL│
│   + SQL Server 2022 │     │   Indexación          │     │   Consulta Pública  │
└─────────────────────┘     └──────────────────────┘     └─────────────────────┘
         │                                                         ▲
         └─────────────────── uploads_data (volumen) ─────────────┘
```

| Servicio       | Tecnología              | Puerto | Rol                                      |
|----------------|-------------------------|--------|------------------------------------------|
| CalidadSYS     | ASP.NET Core 10 MVC     | 5000   | Gestión de documentos, workflows, uploads |
| moduloDMS      | FastAPI + Python 3.12   | 8000   | Indexación en MongoDB, búsqueda full-text |
| PublicDMS      | PHP 8.3 + Apache        | 80     | Portal de consulta pública               |
| SQL Server     | SQL Server 2022         | 1433   | Base de datos maestra                    |
| PostgreSQL     | PostgreSQL 16           | 5432   | Metadatos sincronizados (PublicDMS)      |
| MongoDB        | MongoDB 7               | 27017  | Índice full-text de documentos           |

## Estructura del repositorio

```
/
├── src/
│   ├── dotnet-core/        ← Código fuente CalidadSYS (ASP.NET Core)
│   ├── php-app/            ← Este repositorio ES el módulo PHP
│   └── python-service/     ← Código fuente moduloDMS (FastAPI)
├── db/
│   ├── postgres/           ← Schema PostgreSQL (auto-ejecutado en primer arranque)
│   ├── mongo/              ← Init MongoDB (auto-ejecutado en primer arranque)
│   └── sqlserver/          ← Instrucciones para exportar schema SQL Server
├── docker/
│   ├── php-app/            ← Dockerfile + config Apache + crontab
│   ├── python-service/     ← Dockerfile FastAPI
│   └── dotnet-core/        ← Dockerfile .NET 10 multi-stage
├── docker-compose.yml
├── .env.example
└── README.md
```

> **Nota:** Este repositorio (PublicDMS) actúa como raíz del monorepo.
> Los módulos CalidadSYS y moduloDMS deben copiarse en `src/dotnet-core/` y `src/python-service/` respectivamente antes de ejecutar `docker compose up`.

## Requisitos

- Docker Desktop 4.x o superior
- Git
- 8 GB RAM disponibles para los contenedores

## Configuración inicial

### 1. Clonar y preparar el monorepo

```bash
git clone <URL_DEL_REPO> PublicDMS
cd PublicDMS

# Copiar fuentes de CalidadSYS
cp -r /ruta/a/CalidadSYS/* src/dotnet-core/

# Copiar fuentes de moduloDMS
cp -r /ruta/a/moduloDMS/* src/python-service/
```

### 2. Crear el archivo .env

```bash
cp .env.example .env
# Editar .env con tus contraseñas
```

Variables requeridas:

| Variable            | Descripción                  | Ejemplo               |
|---------------------|------------------------------|-----------------------|
| `MSSQL_SA_PASSWORD` | Contraseña SA de SQL Server  | `YourStrong@Passw0rd` |
| `MSSQL_DB`          | Nombre de la base de datos   | `QualityDMS`          |
| `POSTGRES_DB`       | Nombre de BD PostgreSQL      | `PublicDMS`           |
| `POSTGRES_USER`     | Usuario PostgreSQL           | `postgres`            |
| `POSTGRES_PASSWORD` | Contraseña PostgreSQL        | `postgres123`         |

### 3. Levantar los servicios

```bash
docker compose up -d
```

El primer arranque descarga imágenes base (~5–10 min). Los health checks garantizan el orden correcto de inicio.

### 4. Verificar que todo está correcto

```bash
# Ver estado de todos los contenedores
docker compose ps

# Ver logs en tiempo real
docker compose logs -f

# Verificar PHP (portal público)
curl http://localhost/

# Verificar FastAPI (indexador)
curl http://localhost:8000/health

# Verificar CalidadSYS
curl http://localhost:5000/
```

### 5. Sincronización inicial

Entrar al portal PHP en `http://localhost/` y hacer clic en **Sincronizar**. La sincronización incremental automática corre cada 5 minutos vía cron.

## Flujo de datos

```
CalidadSYS (upload PDF)
    ↓ guarda en uploads_data (volumen Docker compartido)
    ↓ registra en SQL Server (DocumentVersions.FilePath)
    
PublicDMS (sync_docs.php, cada 5 min)
    ↓ lee SQL Server → sincroniza metadatos a PostgreSQL
    ↓ llama FastAPI /indexer/upsert → MongoDB full-text index
    
Usuario final → PublicDMS
    ↓ búsqueda full-text → MongoDB → ids → PostgreSQL
    ↓ descarga PDF → view_pdf.php → uploads_data (solo lectura)
```

## Desarrollo local (sin Docker)

### PHP (PublicDMS)
Requiere XAMPP con PHP 8.3, extensiones `pdo_pgsql` y `pdo_sqlsrv`.
Ajustar `config/storage.php` con la ruta local a los uploads de CalidadSYS.

### FastAPI (moduloDMS)
```bash
cd src/python-service
python -m venv .venv
.venv\Scripts\activate      # Windows
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

### CalidadSYS
Abrir `src/dotnet-core/CalidadSYS.sln` en Visual Studio 2022 y ejecutar con F5.

## Detener los servicios

```bash
docker compose down           # detiene, conserva volúmenes (datos)
docker compose down -v        # detiene y BORRA volúmenes (datos perdidos)
```
