# Development Notes

## Storage Provider

Storage provider dikontrol via `Storage:Provider` di `appsettings.json` (atau env var `Storage__Provider`).

### Mode Local (default, tanpa Docker)

```json
"Storage": {
  "Provider": "Local"
}
```

- File disimpan di `backend/FluxGrid.Api/uploads/`
- Download file via endpoint backend (`/api/v1/hr/storage/...`)
- Tidak perlu MinIO / Docker
- Cocok untuk development lokal

### Mode S3 (MinIO / production)

```json
"Storage": {
  "Provider": "S3",
  "Endpoint": "localhost:9000",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "BucketName": "fluxgrid-cvs",
  "UseSsl": false
}
```

- Wajib jalanin MinIO dulu:
  ```powershell
  docker compose up -d minio
  ```
- Akses console MinIO: http://localhost:9001 (login: `minioadmin` / `minioadmin`)
- Bucket `fluxgrid-cvs` dibuat otomatis oleh aplikasi
- File diupload langsung ke MinIO via presigned URL (tidak lewat backend)
