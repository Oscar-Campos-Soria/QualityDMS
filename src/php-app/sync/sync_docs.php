<?php
// sync/sync_docs.php

ini_set('memory_limit', '256M');
set_time_limit(0);

require_once __DIR__ . '/../config/sqlserver.php';
require_once __DIR__ . '/../config/db.php';

try {

    // --- MODO INCREMENTAL ---
    // Lee último sync exitoso. Resta 5 min de buffer para no perder registros
    // en el borde. Si no hay timestamp previo → sync completo.
    $logFile    = __DIR__ . '/ultimo_sync.txt';
    $lastSyncTs = file_exists($logFile) ? max(0, (int)file_get_contents($logFile) - 300) : 0;

    if ($lastSyncTs > 0) {
        $sinceDate       = gmdate('Y-m-d H:i:s', $lastSyncTs);
        $changedFilter   = "AND (d.UpdatedAt >= '$sinceDate'
                                 OR d.CreatedAt >= '$sinceDate'
                                 OR dv.CreatedAt >= '$sinceDate')";
        $modeLabel       = "INCREMENTAL desde $sinceDate";
    } else {
        $changedFilter   = "";
        $modeLabel       = "COMPLETO";
    }

    echo "--- Sincronización [$modeLabel]: " . date('Y-m-d H:i:s') . " ---\n";

    $batchSize      = 500;
    $offset         = 0;
    $totalProcessed = 0;

    $queryPG = "
        INSERT INTO publicdms.documents (
            id, code, title, category_id, category_name,
            department_id, department_name, version,
            file_url, is_active, effective_date, expiration_date, last_sync
        ) VALUES (
            :id, :code, :title, :cat_id, :cat_name,
            :dep_id, :dep_name, :version,
            :url, :is_active, :eff, :exp, NOW()
        )
        ON CONFLICT (id) DO UPDATE SET
            code            = EXCLUDED.code,
            title           = EXCLUDED.title,
            category_name   = EXCLUDED.category_name,
            department_name = EXCLUDED.department_name,
            version         = EXCLUDED.version,
            file_url        = EXCLUDED.file_url,
            is_active       = EXCLUDED.is_active,
            last_sync       = NOW();
    ";
    $stmtPG = $pdo->prepare($queryPG);

    while (true) {
        $querySQL = "
            SELECT
                d.DocumentId     AS doc_id,
                d.Code           AS doc_code,
                d.Title          AS doc_title,
                d.CategoryId     AS cat_id,
                c.Name           AS cat_name,
                d.DepartmentId   AS dep_id,
                dep.Name         AS dep_name,
                d.IsActive       AS doc_is_active,
                dv.VersionNumber AS doc_version,
                dv.FilePath      AS doc_url,
                d.EffectiveDate,
                d.ExpirationDate
            FROM Documents d
            INNER JOIN DocumentCategories c   ON d.CategoryId   = c.CategoryId
            INNER JOIN Departments dep        ON d.DepartmentId = dep.DepartmentId
            INNER JOIN DocumentVersions dv    ON dv.DocumentId  = d.DocumentId
            WHERE d.CurrentStatus = 3
              AND dv.IsCurrent = 1
              $changedFilter
            ORDER BY d.DocumentId
            OFFSET $offset ROWS FETCH NEXT $batchSize ROWS ONLY;
        ";

        $stmtSQL = $connSQL->prepare($querySQL);
        $stmtSQL->execute();
        $batch = $stmtSQL->fetchAll(PDO::FETCH_ASSOC);

        if (empty($batch)) break;

        $pdo->beginTransaction();

        foreach ($batch as $doc) {
            $rutaRelativa = ltrim(str_replace('\\', '/', (string)$doc['doc_url']), '/');
            $isActive     = (bool)$doc['doc_is_active'];

            $stmtPG->execute([
                ':id'        => $doc['doc_id'],
                ':code'      => $doc['doc_code'],
                ':title'     => $doc['doc_title'],
                ':cat_id'    => $doc['cat_id'],
                ':cat_name'  => $doc['cat_name'],
                ':dep_id'    => $doc['dep_id'],
                ':dep_name'  => $doc['dep_name'],
                ':version'   => (string)$doc['doc_version'],
                ':url'       => $rutaRelativa,
                ':is_active' => $isActive ? 'true' : 'false',
                ':eff'       => $doc['EffectiveDate'],
                ':exp'       => $doc['ExpirationDate'],
            ]);

            $totalProcessed++;
        }

        $pdo->commit();
        echo "   > Procesados: $totalProcessed\n"; flush();

        $offset += $batchSize;
    }

    // Log en PostgreSQL solo si hubo cambios
    if ($totalProcessed > 0) {
        $pdo->prepare("INSERT INTO publicdms.sync_log (entity, action, processed_at)
                        VALUES ('documents', 'SYNC_COMPLETED', NOW())")->execute();
    }

    echo "\n====================================================\n";
    echo "   MODO         : $modeLabel\n";
    echo "   Procesados   : $totalProcessed\n";
    echo "====================================================\n";

} catch (Exception $e) {
    if (isset($pdo) && $pdo->inTransaction()) {
        $pdo->rollBack();
    }
    echo "\n❌ ERROR: " . $e->getMessage() . "\n";
    error_log("[" . date('Y-m-d H:i:s') . "] Sync Error: " . $e->getMessage() . PHP_EOL, 3, __DIR__ . "/sync_errors.log");
}
