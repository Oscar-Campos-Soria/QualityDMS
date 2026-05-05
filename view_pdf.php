<?php
// Carpeta donde realmente guardas los archivos
$storage_path = __DIR__ . '/storage/pdfs/'; 

if (isset($_GET['file'])) {
    $file = basename($_GET['file']); // Por seguridad, solo el nombre del archivo
    $full_path = $storage_path . $file;

    if (file_exists($full_path)) {
        header('Content-Type: application/pdf');
        header('Content-Disposition: inline; filename="' . $file . '"');
        readfile($full_path);
        exit;
    } else {
        echo "Error: El archivo físico no existe en el servidor local.";
    }
}
?>