<?php
// sync/sync_categories.php
require_once __DIR__ . '/../config/sqlserver.php';
require_once __DIR__ . '/../config/db.php';

try {
    echo "--- Sincronizando Categorías ---\n";

    // Extraer de SQL Server
    $stmtSQL = $connSQL->prepare("SELECT CategoryId, Code, Name FROM DocumentCategories");
    $stmtSQL->execute();
    $categories = $stmtSQL->fetchAll(PDO::FETCH_ASSOC);

    // Upsert en PostgreSQL
    $queryPG = "INSERT INTO categories (id, code, name) 
                VALUES (:id, :code, :name)
                ON CONFLICT (id) DO UPDATE SET 
                code = EXCLUDED.code, name = EXCLUDED.name";
    
    $stmtPG = $pdo->prepare($queryPG);
    $pdo->beginTransaction();

    foreach ($categories as $cat) {
        $stmtPG->execute([
            ':id'   => $cat['CategoryId'],
            ':code' => $cat['Code'],
            ':name' => $cat['Name']
        ]);
    }

    $pdo->commit();
    echo "Éxito: " . count($categories) . " categorías sincronizadas.\n";

} catch (Exception $e) {
    if (isset($pdo) && $pdo->inTransaction()) $pdo->rollBack();
    echo "Error: " . $e->getMessage() . "\n";
}