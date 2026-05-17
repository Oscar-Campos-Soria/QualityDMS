<?php
class MongoSearchClient {
    private $searchUrl = "http://127.0.0.1:8000/indexer/search";

    /**
     * Queries FastAPI → MongoDB $text search.
     * Returns doc IDs ordered by relevance score (best match first).
     * Throws RuntimeException if FastAPI is unreachable or returns error.
     */
    public function searchDocumentIds(string $query): array {
        $url = $this->searchUrl . '?q=' . urlencode($query) . '&limit=500';

        $ch = curl_init($url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, 3);
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
