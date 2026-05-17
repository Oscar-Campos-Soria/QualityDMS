<?php
/**
 * --- SYNC MAIN: Orquestador de Sincronización Global ---
 * Este archivo coordina la descarga de categorías, departamentos y documentos.
 */

// Deshabilitar compresión y buffering para streaming en navegador
@apache_setenv('no-gzip', 1);
@ini_set('zlib.output_compression', 0);
@ini_set('implicit_flush', 1);
while (ob_get_level()) ob_end_clean();
ob_implicit_flush(true);
set_time_limit(0);

header('Content-Type: text/plain; charset=utf-8');
header('X-Accel-Buffering: no'); // para nginx si aplica
header('Cache-Control: no-cache');

// Padding inicial — navegadores esperan ~1KB antes de renderizar streaming
echo str_pad('', 1024) . "\n";

echo "====================================================\n";
echo "   SISTEMA DE SINCRONIZACIÓN PUBLIC DMS             \n";
echo "   Iniciado: " . date('d-m-Y H:i:s') . "            \n";
echo "====================================================\n\n";
flush();

// 2. Definición de rutas
$archivo_log_tiempo = __DIR__ . '/ultimo_sync.txt';

try {
    // 3. Ejecución de Módulos (El orden es vital por llaves foráneas)
    
    echo "[1/3] Sincronizando Categorías...\n"; flush();
    require_once __DIR__ . '/sync_categories.php';
    echo "✓ Categorías completadas.\n\n"; flush();

    echo "[2/3] Sincronizando Departamentos...\n"; flush();
    require_once __DIR__ . '/sync_departments.php';
    echo "✓ Departamentos completados.\n\n"; flush();

    echo "[3/3] Sincronizando Documentos...\n"; flush();
    require_once __DIR__ . '/sync_docs.php';
    echo "✓ Documentos actualizados.\n\n"; flush();

    // 4. Actualizar marca de tiempo de éxito
    // Esto lo lee el index.php para mostrar "Último Sync: HH:mm"
    file_put_contents($archivo_log_tiempo, time());

    echo "====================================================\n";
    echo "   SINCRONIZACIÓN FINALIZADA CON ÉXITO              \n";
    echo "   Terminado: " . date('H:i:s') . "                 \n";
    echo "====================================================\n";

} catch (Exception $e) {
    echo "\n❌ ERROR DURANTE LA SINCRONIZACIÓN:\n";
    echo $e->getMessage() . "\n";
    
    // Registrar error en log físico
    error_log("[" . date('Y-m-d H:i:s') . "] Global Sync Error: " . $e->getMessage() . PHP_EOL, 3, __DIR__ . "/error_log.txt");
}