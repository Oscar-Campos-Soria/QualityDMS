<?php
// sync/sync_docs.php

// 1. Configuración de límites y rutas
ini_set('memory_limit', '1024M'); // Aumentado para manejar buffers grandes
set_time_limit(0);               // Sin límite de tiempo para procesos masivos
$storage_dir = __DIR__ . '/../storage/pdfs/';

if (!is_dir($storage_dir)) {
    mkdir($storage_dir, 0777, true);
}

require_once __DIR__ . '/../includes/IndexerClient.php';
use App\Services\IndexerClient;

require_once __DIR__ . '/../config/sqlserver.php'; 
require_once __DIR__ . '/../config/db.php';        

try {
    $indexer = new IndexerClient();
    echo "--- Iniciando Sincronización Masiva: " . date('Y-m-d H:i:s') . " ---\n";

    // Configuraciones de Paginación
    $batchSize = 1000;
    $offset = 0;
    $totalProcessed = 0;
    $totalDownloads = 0;
    $totalIndexed = 0;

    // 2. Preparar el INSERT/UPSERT en PostgreSQL (Fuera del bucle para eficiencia)
    $queryPG = "
        INSERT INTO publicdms.documents (
            id, code, title, category_id, category_name, 
            department_id, department_name, version, 
            file_url, local_file_name, effective_date, expiration_date, last_sync
        ) VALUES (
            :id, :code, :title, :cat_id, :cat_name, 
            :dep_id, :dep_name, :version, 
            :url, :local_name, :eff, :exp, NOW()
        )
        ON CONFLICT (id) DO UPDATE SET
            code = EXCLUDED.code,
            title = EXCLUDED.title,
            category_name = EXCLUDED.category_name,
            department_name = EXCLUDED.department_name,
            version = EXCLUDED.version,
            file_url = EXCLUDED.file_url,
            local_file_name = EXCLUDED.local_file_name,
            last_sync = NOW();
    ";
    $stmtPG = $pdo->prepare($queryPG);

    // Bucle principal de paginación
    while (true) {
        /**
         * 3. EXTRAER: Bloque de SQL Server usando OFFSET/FETCH
         */
        $querySQL = "
            SELECT 
                d.DocumentId     AS doc_id, 
                d.Code           AS doc_code,
                d.Title          AS doc_title,
                d.CategoryId     AS cat_id,
                c.Name           AS cat_name,
                d.DepartmentId   AS dep_id,
                dep.Name         AS dep_name,
                dv.VersionNumber AS doc_version,
                dv.FilePath      AS doc_url,
                d.EffectiveDate,
                d.ExpirationDate
            FROM Documents d
            INNER JOIN DocumentCategories c ON d.CategoryId = c.CategoryId
            INNER JOIN Departments dep ON d.DepartmentId = dep.DepartmentId
            INNER JOIN DocumentVersions dv ON dv.DocumentId = d.DocumentId
            WHERE d.CurrentStatus = 3 AND dv.IsCurrent = 1
            ORDER BY d.DocumentId
            OFFSET $offset ROWS FETCH NEXT $batchSize ROWS ONLY;
        ";

        $stmtSQL = $connSQL->prepare($querySQL);
        $stmtSQL->execute();
        $batch = $stmtSQL->fetchAll(PDO::FETCH_ASSOC);

        // Si no hay más registros, terminamos el bucle
        if (empty($batch)) break;

        $pdo->beginTransaction();

        foreach ($batch as $doc) {
            // --- LÓGICA DE ARCHIVO FÍSICO ---
            $rutaRelativa = $doc['doc_url'];
            $nombreArchivoLocal = null;

            if (!empty($rutaRelativa)) {
                $rutaRelativa = str_replace('/', DIRECTORY_SEPARATOR, str_replace('\\', '/', $rutaRelativa));
                // Ruta de origen según tu estructura de carpetas
                $rutaOrigen = 'C:\\Users\\eroer\\source\\repos\\CalidadSYS\\CalidadSYS\\uploads\\' . $rutaRelativa;
                $basename = basename($rutaRelativa);
                $rutaFisicaLocal = $storage_dir . $basename;

                if (file_exists($rutaFisicaLocal)) {
                    $nombreArchivoLocal = $basename;
                } elseif (file_exists($rutaOrigen)) {
                    if (copy($rutaOrigen, $rutaFisicaLocal)) {
                        $nombreArchivoLocal = $basename;
                        $totalDownloads++;
                    }
                }
            }

            // --- CARGAR EN POSTGRESQL ---
            $stmtPG->execute([
                ':id'         => $doc['doc_id'],
                ':code'       => $doc['doc_code'],
                ':title'      => $doc['doc_title'],
                ':cat_id'     => $doc['cat_id'],
                ':cat_name'   => $doc['cat_name'],
                ':dep_id'     => $doc['dep_id'],
                ':dep_name'   => $doc['dep_name'],
                ':version'    => (string)$doc['doc_version'], // Gracias al ALTER TABLE esto ya no fallará
                ':url'        => $doc['doc_url'],
                ':local_name' => $nombreArchivoLocal,
                ':eff'        => $doc['EffectiveDate'],
                ':exp'        => $doc['ExpirationDate']
            ]);

            // --- INDEXAR EN MONGODB (Solo si el registro es válido) ---
            // Enviamos incluso si el archivo físico no existe para poblar el buscador en pruebas
            $dataSync = [
                'id'              => $doc['doc_id'],
                'code'            => $doc['doc_code'],
                'title'           => $doc['doc_title'],
                'category_name'   => $doc['cat_name'],
                'department_name' => $doc['dep_name'],
                'version'         => (string)$doc['doc_version'],
                'is_active'       => true,
                'file_url'        => $nombreArchivoLocal ?? 'pending_file.pdf'
            ];

            if ($indexer->enviarAMongo($dataSync)) {
                $totalIndexed++;
            }

            $totalProcessed++;
        }

        $pdo->commit();
        echo "   > Bloque completado: $totalProcessed registros procesados...\n";
        
        $offset += $batchSize;
    }

    // Registrar log final en Postgres
    $stmtLog = $pdo->prepare("INSERT INTO publicdms.sync_log (entity, action, processed_at) VALUES ('documents', 'MASS_SYNC_COMPLETED', NOW())");
    $stmtLog->execute();

    echo "\n====================================================\n";
    echo "   SINCRONIZACIÓN MASIVA FINALIZADA\n";
    echo "   Total Registros: $totalProcessed\n";
    echo "   Archivos Físicos: $totalDownloads\n";
    echo "   Indexados en Mongo: $totalIndexed\n";
    echo "====================================================\n";

} catch (Exception $e) {
    if (isset($pdo) && $pdo->inTransaction()) {
        $pdo->rollBack();
    }
    echo "\n❌ ERROR CRÍTICO: " . $e->getMessage() . "\n";
    error_log("[" . date('Y-m-d H:i:s') . "] Mass Sync Error: " . $e->getMessage() . PHP_EOL, 3, "sync_errors.log");
}