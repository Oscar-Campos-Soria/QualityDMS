<?php
// Configuración de la base de datos
$host = 'localhost';
$db   = 'PublicDMS';
$user = 'postgres';
$pass = '1234';
$port = '5432';

try {
    $dsn = "pgsql:host=$host;port=$port;dbname=$db";
    $options = [
        PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
        // Es recomendable forzar UTF-8 para evitar problemas con tildes o eñes
        PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8" 
    ];
    
    $pdo = new PDO($dsn, $user, $pass, $options);

    /**
     * AJUSTE DE ESQUEMA:
     * Establecemos el "search_path" para que PHP encuentre las tablas en 'publicdms' 
     * sin necesidad de escribir el prefijo en cada consulta SQL.
     */
    $pdo->exec("SET search_path TO publicdms, public");

} catch (PDOException $e) {
    echo "Error de conexión: " . $e->getMessage();
    exit;
}