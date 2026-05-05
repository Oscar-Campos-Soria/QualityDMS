<?php
/**
 * --- SYNC MAIN: Orquestador de Sincronización Global ---
 * Este archivo coordina la descarga de categorías, departamentos y documentos.
 */

// 1. Configuración de cabeceras para visualización en navegador
header('Content-Type: text/plain; charset=utf-8');
echo "====================================================\n";
echo "   SISTEMA DE SINCRONIZACIÓN PUBLIC DMS             \n";
echo "   Iniciado: " . date('d-m-Y H:i:s') . "            \n";
echo "====================================================\n\n";

// 2. Definición de rutas
$archivo_log_tiempo = __DIR__ . '/ultimo_sync.txt';

try {
    // 3. Ejecución de Módulos (El orden es vital por llaves foráneas)
    
    echo "[1/3] Sincronizando Categorías...\n";
    require_once __DIR__ . '/sync_categories.php';
    echo "✓ Categorías completadas.\n\n";

    echo "[2/3] Sincronizando Departamentos...\n";
    require_once __DIR__ . '/sync_departments.php';
    echo "✓ Departamentos completados.\n\n";

    echo "[3/3] Sincronizando Documentos y Descarga de Archivos...\n";
    // Este script contiene la lógica de file_get_contents que añadimos
    require_once __DIR__ . '/sync_docs.php';
    echo "✓ Documentos y archivos físicos actualizados.\n\n";

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