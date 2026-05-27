<?php
$host = getenv('POSTGRES_HOST')     ?: 'localhost';
$db   = getenv('POSTGRES_DB')       ?: 'PublicDMS';
$user = getenv('POSTGRES_USER')     ?: 'postgres';
$pass = getenv('POSTGRES_PASSWORD') ?: '1234';
$port = getenv('POSTGRES_PORT')     ?: '5432';

try {
    $dsn = "pgsql:host=$host;port=$port;dbname=$db";
    $pdo = new PDO($dsn, $user, $pass, [
        PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
    ]);
    $pdo->exec("SET search_path TO publicdms, public");
} catch (PDOException $e) {
    http_response_code(500);
    echo json_encode(["error" => "DB connection failed"]);
    exit;
}
