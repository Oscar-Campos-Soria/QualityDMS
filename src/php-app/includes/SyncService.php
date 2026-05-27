<?php
class SyncService {
    private $fastApiUrl = "http://127.0.0.1:8000/indexer/sync";

    public function ejecutarSincronizacion($row) {
        // Preparamos el JSON exactamente como lo espera Python
        $data = [
            "postgres_id" => (int)$row['id'],
            "code" => $row['codigo'],
            "title" => $row['titulo'],
            "category_name" => $row['nombre_categoria'],
            "department_name" => $row['nombre_departamento'],
            "tags" => explode(",", $row['tags']), 
            "description" => $row['descripcion'],
            "extra_info" => [
                "file_path" => $row['ruta_archivo'],
                "file_size" => $row['tamano'],
                "uploaded_by" => $row['usuario_id']
            ]
        ];

        $payload = json_encode($data);

        $ch = curl_init($this->fastApiUrl);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, $payload);
        curl_setopt($ch, CURLOPT_HTTPHEADER, array('Content-Type:application/json'));
        
        $response = curl_exec($ch);
        curl_close($ch);
        
        return $response;
    }
}