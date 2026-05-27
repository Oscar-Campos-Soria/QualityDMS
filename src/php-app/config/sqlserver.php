<?php
$sqlHost = getenv('SQLSERVER_HOST')     ?: 'localhost\\SQLEXPRESS01';
$sqlDb   = getenv('SQLSERVER_DB')       ?: 'QualityDMS';
$sqlUser = getenv('SQLSERVER_USER')     ?: null;
$sqlPass = getenv('SQLSERVER_PASSWORD') ?: null;

try {
    if ($sqlUser) {
        // Docker / SQL Auth
        $dsn     = "sqlsrv:Server=$sqlHost;Database=$sqlDb;TrustServerCertificate=true";
        $connSQL = new PDO($dsn, $sqlUser, $sqlPass);
    } else {
        // Windows — Integrated Security
        $dsn     = "sqlsrv:Server=$sqlHost;Database=$sqlDb;TrustServerCertificate=true";
        $connSQL = new PDO($dsn);
    }
    $connSQL->setAttribute(PDO::ATTR_ERRMODE,            PDO::ERRMODE_EXCEPTION);
    $connSQL->setAttribute(PDO::ATTR_DEFAULT_FETCH_MODE, PDO::FETCH_ASSOC);
} catch (PDOException $e) {
    die("Error conectando a SQL Server: " . $e->getMessage());
}
