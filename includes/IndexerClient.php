<?php
namespace App\Services;

class IndexerClient {
    // La URL de tu microservicio de FastAPI
    private $apiUrl = "http://127.0.0.1:8000/indexer/sync";

    public function enviarAMongo($documento) {
        // Preparamos el JSON con la estructura exacta que definimos en Python
        $payload = json_encode([
            "postgres_id" => (string)$documento['id'], // UUID de tu imagen
            "code" => $documento['code'],
            "title" => $documento['title'],
            "category_name" => $documento['category_name'],
            "department_name" => $documento['department_name'],
            "version" => $documento['version'] ?? "1.0",
            "is_active" => (bool)($documento['is_active'] ?? true),
            "extra_info" => [
                "file_url" => $documento['file_url'] ?? "",
                "sync_origin" => "App_Realtime"
            ]
        ]);

        $ch = curl_init($this->apiUrl);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, $payload);
        curl_setopt($ch, CURLOPT_HTTPHEADER, [
            'Content-Type: application/json',
            'Content-Length: ' . strlen($payload)
        ]);

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        return ($httpCode === 200);
    }
}