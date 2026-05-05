<?php
// config/sqlserver.php

// URL base donde el servidor de origen sirve los archivos PDF.
// Local:  'http://localhost/QualityDMS/uploads/'  (o el puerto/ruta de tu app)
// Docker: usar variable de entorno FILES_BASE_URL
define('FILES_BASE_URL', getenv('FILES_BASE_URL') ?: 'http://localhost/QualityDMS/uploads/');

$serverName = "localhost\\SQLEXPRESS01";
$database   = "QualityDMS";

try {
    /**
     * DSN para SQL Server. 
     * Al usar "Integrated Security", omitimos UID y PWD.
     * TrustServerCertificate=true es necesario según tu cadena de conexión.
     */
    $dsn = "sqlsrv:Server=$serverName;Database=$database;TrustServerCertificate=true";
    
    $connSQL = new PDO($dsn);
    
    // Configuración de errores para desarrollo
    $connSQL->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
    $connSQL->setAttribute(PDO::ATTR_DEFAULT_FETCH_MODE, PDO::FETCH_ASSOC);

} catch (PDOException $e) {
    // Si falla, detenemos el script para no intentar sincronizar sin fuente
    die("Error conectando a SQL Server: " . $e->getMessage());
}