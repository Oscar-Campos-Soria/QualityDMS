<?php
require_once __DIR__ . '/config/storage.php';

if (!isset($_GET['file']) || trim($_GET['file']) === '') {
    http_response_code(400);
    exit('Parámetro file requerido.');
}

// Normalizar: solo forward slashes, sin slash inicial
$relativePath = ltrim(str_replace('\\', '/', $_GET['file']), '/');

// Bloquear path traversal antes de realpath
if (strpos($relativePath, '..') !== false) {
    http_response_code(403);
    exit('Ruta no permitida.');
}

$fullPath = DMS_STORAGE_ROOT . DIRECTORY_SEPARATOR . str_replace('/', DIRECTORY_SEPARATOR, $relativePath);

$realStorage = realpath(DMS_STORAGE_ROOT);
$realFile    = realpath($fullPath);

if ($realStorage === false) {
    http_response_code(500);
    exit('Storage no configurado. Verificar DMS_STORAGE_PATH.');
}

if ($realFile === false || strpos($realFile, $realStorage . DIRECTORY_SEPARATOR) !== 0) {
    http_response_code(403);
    exit('Acceso denegado.');
}

if (!file_exists($realFile)) {
    http_response_code(404);
    exit('Archivo no encontrado en storage.');
}

$download = isset($_GET['download']) && $_GET['download'] === '1';
$filename = basename($realFile);

header('Content-Type: application/pdf');
header('Content-Length: ' . filesize($realFile));
header('Content-Disposition: ' . ($download ? 'attachment' : 'inline') . '; filename="' . rawurlencode($filename) . '"');
header('X-Content-Type-Options: nosniff');
readfile($realFile);
exit;
