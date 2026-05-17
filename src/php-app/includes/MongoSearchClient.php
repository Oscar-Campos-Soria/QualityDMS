<?php
class MongoSearchClient {
    private $searchUrl;
    private $apiKey;

    public function __construct() {
        $base = defined('FASTAPI_URL') ? FASTAPI_URL : (getenv('FASTAPI_URL') ?: 'http://127.0.0.1:8000');
        $this->searchUrl = rtrim($base, '/') . '/indexer/search';
        $this->apiKey    = defined('FASTAPI_API_KEY') ? FASTAPI_API_KEY : (getenv('FASTAPI_API_KEY') ?: '');
    }

    public function searchDocumentIds(string $query): array {
        $url = $this->searchUrl . '?q=' . urlencode($query) . '&limit=500';

        $ch = curl_init($url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, 3);
        curl_setopt($ch, CURLOPT_HTTPHEADER, [
            'X-API-Key: ' . $this->apiKey,
        ]);
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        if ($response === false || $httpCode !== 200) {
            throw new RuntimeException("MongoDB search unavailable (HTTP $httpCode)");
        }

        $data = json_decode($response, true);
        return array_map('intval', $data['ids'] ?? []);
    }
}
