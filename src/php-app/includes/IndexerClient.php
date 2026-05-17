<?php
namespace App\Services;

class IndexerClient {
    private $apiUrl;

    public function __construct() {
        $base = defined('FASTAPI_URL') ? FASTAPI_URL : 'http://127.0.0.1:8000';
        $this->apiUrl = rtrim($base, '/') . '/indexer/upsert';
    }

    public function enviarAMongo(array $documento): bool {
        $payload = json_encode([
            'postgres_id'     => (string)$documento['id'],
            'code'            => $documento['code'],
            'title'           => $documento['title'],
            'category_name'   => $documento['category_name'],
            'department_name' => $documento['department_name'],
            'version'         => $documento['version']  ?? '1.0',
            'is_active'       => (bool)($documento['is_active'] ?? true),
            'file_url'        => $documento['file_url'] ?? '',
        ]);

        $ch = curl_init($this->apiUrl);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_POST,           true);
        curl_setopt($ch, CURLOPT_POSTFIELDS,     $payload);
        curl_setopt($ch, CURLOPT_TIMEOUT,        5);
        $apiKey = defined('FASTAPI_API_KEY') ? FASTAPI_API_KEY : (getenv('FASTAPI_API_KEY') ?: '');
        curl_setopt($ch, CURLOPT_HTTPHEADER, [
            'Content-Type: application/json',
            'Content-Length: ' . strlen($payload),
            'X-API-Key: ' . $apiKey,
        ]);

        curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        return $httpCode === 200;
    }
}
