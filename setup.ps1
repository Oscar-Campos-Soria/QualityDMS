# ============================================================
#  DMS Setup — genera .env y levanta todos los contenedores
#  Uso: .\setup.ps1
# ============================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  Sistema de Gestion Documental - Setup Inicial  " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# ── Verificar Docker ─────────────────────────────────────────
try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Docker no responde" }
} catch {
    Write-Host "[ERROR] Docker no esta corriendo. Inicia Docker Desktop primero." -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Docker detectado." -ForegroundColor Green

# ── Verificar docker compose ──────────────────────────────────
$null = docker compose version 2>&1
if ($LASTEXITCODE -eq 0) {
    $composeCmd = "docker compose"
} else {
    $null = docker-compose version 2>&1
    if ($LASTEXITCODE -eq 0) {
        $composeCmd = "docker-compose"
    } else {
        Write-Host "[ERROR] docker-compose no encontrado." -ForegroundColor Red
        exit 1
    }
}

# ── Flag: necesita generar .env? ─────────────────────────────
$generarEnv = $true

if (Test-Path ".env") {
    Write-Host ""
    Write-Host "[AVISO] Ya existe un archivo .env." -ForegroundColor Yellow
    $respuesta = Read-Host "  Sobreescribir? (s/N)"
    if ($respuesta -notmatch "^[sS]$") {
        Write-Host "  Usando .env existente." -ForegroundColor Cyan
        $generarEnv = $false
    }
}

# ── Pedir contrasena maestra ──────────────────────────────────
if ($generarEnv) {
    Write-Host ""
    Write-Host "Ingresa una contrasena para TODOS los servicios de base de datos." -ForegroundColor Yellow
    Write-Host "  Requisito: minimo 6 caracteres." -ForegroundColor Gray
    Write-Host ""

    $p1 = ""
    do {
        $pass1 = Read-Host "  Contrasena" -AsSecureString
        $pass2 = Read-Host "  Confirmar contrasena" -AsSecureString

        $p1 = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
                  [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pass1))
        $p2 = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
                  [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pass2))

        if ($p1 -ne $p2) {
            Write-Host "  [!] Las contrasenas no coinciden. Intenta de nuevo." -ForegroundColor Red
            $p1 = ""
            continue
        }
        if ($p1.Length -lt 6) {
            Write-Host "  [!] Minimo 6 caracteres." -ForegroundColor Red
            $p1 = ""
            continue
        }
    } while ([string]::IsNullOrEmpty($p1))

    $MASTER_PASS = $p1

    # SQL Server requiere simbolo especial — agrega sufijo si no tiene
    $MSSQL_PASS = if ($MASTER_PASS -match '[^a-zA-Z0-9]') { $MASTER_PASS } else { $MASTER_PASS + "@Dm5" }

    # Generar API key aleatoria (64 hex chars)
    $apiKeyBytes = New-Object byte[] 32
    [Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($apiKeyBytes)
    $API_KEY = ($apiKeyBytes | ForEach-Object { $_.ToString("x2") }) -join ""

    # Escribir .env
    $envContent = @"
# Generado por setup.ps1 — $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

# ── SQL Server ──────────────────────────────
MSSQL_SA_PASSWORD=$MSSQL_PASS
MSSQL_DB=QualityDMS

# ── PostgreSQL ───────────────────────────────
POSTGRES_DB=PublicDMS
POSTGRES_USER=postgres
POSTGRES_PASSWORD=$MASTER_PASS

# ── MongoDB ──────────────────────────────────
MONGO_USER=mongoadmin
MONGO_PASSWORD=$MASTER_PASS

# ── FastAPI ───────────────────────────────────
FASTAPI_API_KEY=$API_KEY
FASTAPI_URL=http://fastapi:8000

# ── Sync ──────────────────────────────────────
SYNC_INTERVAL_SECONDS=30
"@

    Set-Content -Path ".env" -Value $envContent -Encoding UTF8
    Write-Host ""
    Write-Host "[OK] Archivo .env generado." -ForegroundColor Green
}

# ── Levantar contenedores ─────────────────────────────────────
Write-Host ""
Write-Host "Construyendo e iniciando contenedores..." -ForegroundColor Cyan
Write-Host "(Esto puede tardar varios minutos la primera vez)" -ForegroundColor Gray
Write-Host ""

Invoke-Expression "$composeCmd up -d --build"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Fallo al levantar contenedores. Revisa los logs:" -ForegroundColor Red
    Write-Host "  $composeCmd logs" -ForegroundColor Gray
    exit 1
}

# ── Esperar SQL Server ────────────────────────────────────────
Write-Host ""
Write-Host "Esperando que SQL Server este listo (puede tardar ~60s)..." -ForegroundColor Yellow

$attempts    = 0
$maxAttempts = 20
do {
    Start-Sleep -Seconds 5
    $attempts++
    $status = docker inspect --format="{{.State.Health.Status}}" dms_sqlserver 2>$null
    Write-Host "  [$attempts/$maxAttempts] SQL Server: $status" -ForegroundColor Gray
} while ($status -ne "healthy" -and $attempts -lt $maxAttempts)

if ($status -ne "healthy") {
    Write-Host "[AVISO] SQL Server tarda mas de lo esperado. Verifica: docker logs dms_sqlserver" -ForegroundColor Yellow
} else {
    Write-Host "[OK] SQL Server listo." -ForegroundColor Green
}

# ── URLs finales ──────────────────────────────────────────────
Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host "  Sistema levantado correctamente!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Portal PublicDMS (HTTP) : http://localhost" -ForegroundColor White
Write-Host "  Portal PublicDMS (HTTPS): https://localhost:8443" -ForegroundColor White
Write-Host "  CalidadSYS              : http://localhost:5000" -ForegroundColor White
Write-Host "  CalidadSYS Swagger      : http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  FastAPI Docs            : http://localhost:8001/docs" -ForegroundColor White
Write-Host ""
Write-Host "  Ver logs  : $composeCmd logs -f" -ForegroundColor Gray
Write-Host "  Detener   : $composeCmd down" -ForegroundColor Gray
Write-Host ""
