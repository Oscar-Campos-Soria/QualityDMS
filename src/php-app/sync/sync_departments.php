<?php
// sync/sync_departments.php
require_once __DIR__ . '/../config/sqlserver.php';
require_once __DIR__ . '/../config/db.php';

try {
    echo "--- Sincronizando Departamentos ---\n";

    $stmtSQL = $connSQL->prepare("SELECT DepartmentId, Code, Name FROM Departments");
    $stmtSQL->execute();
    $departments = $stmtSQL->fetchAll(PDO::FETCH_ASSOC);

    $queryPG = "INSERT INTO departments (id, code, name) 
                VALUES (:id, :code, :name)
                ON CONFLICT (id) DO UPDATE SET 
                code = EXCLUDED.code, name = EXCLUDED.name";
    
    $stmtPG = $pdo->prepare($queryPG);
    $pdo->beginTransaction();

    foreach ($departments as $dep) {
        $stmtPG->execute([
            ':id'   => $dep['DepartmentId'],
            ':code' => $dep['Code'],
            ':name' => $dep['Name']
        ]);
    }

    $pdo->commit();
    echo "Éxito: " . count($departments) . " departamentos sincronizados.\n";

} catch (Exception $e) {
    if (isset($pdo) && $pdo->inTransaction()) $pdo->rollBack();
    echo "Error: " . $e->getMessage() . "\n";
}