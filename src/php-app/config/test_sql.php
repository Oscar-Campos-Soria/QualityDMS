<?php
// Configuración para SQL Server 2022 local
$serverName = "localhost\\SQLEXPRESS01"; 
$database   = "QualityDMS";

try {
    // Importante: TrustServerCertificate=1 es vital para conexiones locales en SQL 2022
    $dsn = "sqlsrv:Server=$serverName;Database=$database;Encrypt=yes;TrustServerCertificate=1";
    
    $conn = new PDO($dsn);
    $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

    echo "<h1>✅ ¡Conectado con éxito!</h1>";
    echo "PHP ya puede leer datos de tu SQL Server 2022.";
    
} catch (PDOException $e) {
    echo "<h1>❌ Error de conexión</h1>";
    echo "Detalle: " . $e->getMessage();
}