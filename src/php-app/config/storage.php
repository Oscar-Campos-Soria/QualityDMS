<?php
// Storage root compartido. Configurar en XAMPP vía httpd.conf:
//   SetEnv DMS_STORAGE_PATH "D:\DMSUploads"
// o vía variable de entorno del sistema.
define('DMS_STORAGE_ROOT', rtrim(
    getenv('DMS_STORAGE_PATH') ?: 'C:\\Users\\eroer\\source\\repos\\CalidadSYS\\CalidadSYS\\uploads',
    '\\/'
));

define('FASTAPI_URL', rtrim(
    getenv('FASTAPI_URL') ?: 'http://127.0.0.1:8000',
    '/'
));
