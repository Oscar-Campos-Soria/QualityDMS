<?php
header('Content-Type: application/json');

$logFile = __DIR__ . '/ultimo_sync.txt';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $phpBin = PHP_BINARY;
    $script = __DIR__ . '/sync_main.php';

    if (stristr(PHP_OS, 'WIN')) {
        pclose(popen("start /B \"\" \"$phpBin\" \"$script\"", "r"));
    } else {
        shell_exec("\"$phpBin\" \"$script\" > /dev/null 2>&1 &");
    }
}

echo json_encode([
    'status'    => 'ok',
    'last_sync' => file_exists($logFile) ? (int)file_get_contents($logFile) : 0
]);
