# Technical Specifications: CV Upload (HR-4)

## 1. System Architecture
- **Frontend**: Next.js 16 Client Components (react-dropzone, XHR upload with progress).
- **Backend**: .NET 8 Minimal API (Minimal API endpoints, EF Core, Npgsql).
- **Storage**: MinIO (dev) / S3-compatible (production) via presigned URLs, or Local File Storage for Docker-less dev.
- **Database**: PostgreSQL 16 via EF Core.

---

## 2. Database Schema

### Table: `candidates`

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `id` | UUID | PK | |
| `name` | VARCHAR(200) | NOT NULL | Candidate full name |
| `email` | VARCHAR(255) | NOT NULL | |
| `phone` | VARCHAR(30) | | |
| `location` | VARCHAR(200) | | |
| `linkedin_url` | VARCHAR(500) | | |
| `github_url` | VARCHAR(500) | | |
| `portfolio_url` | VARCHAR(500) | | |
| `summary` | VARCHAR(2000) | | |
| `total_experience_months` | INT | | |
| `expected_salary_min` | DECIMAL(18,2) | | |
| `expected_salary_max` | DECIMAL(18,2) | | |
| `notice_period_days` | INT | | |
| `status` | VARCHAR(20) | NOT NULL, DEFAULT 'DRAFT' | DRAFT, PARSED, PARSE_FAILED, ACTIVE, INTERVIEW, HIRED, REJECTED, ARCHIVED |
| `file_url` | VARCHAR(1000) | | S3 object key or local path |
| `file_hash` | VARCHAR(64) | UNIQUE(tenant_id, file_hash) | SHA-256 |
| `original_filename` | VARCHAR(500) | | |
| `file_type` | VARCHAR(50) | | pdf, docx |
| `file_size_bytes` | BIGINT | | |
| `uploaded_by` | UUID | NOT NULL | Reference to users table |
| `tenant_id` | UUID | NOT NULL | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | |

### Table: `candidate_education`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID | PK |
| `candidate_id` | UUID | FK → candidates (CASCADE) |
| `institution` | VARCHAR(200) | |
| `degree` | VARCHAR(100) | |
| `field_of_study` | VARCHAR(200) | |
| `start_date` | DATE | |
| `end_date` | DATE | |
| `gpa` | DECIMAL(3,2) | |

### Table: `candidate_experience`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID | PK |
| `candidate_id` | UUID | FK → candidates (CASCADE) |
| `company` | VARCHAR(200) | |
| `role` | VARCHAR(100) | |
| `start_date` | DATE | |
| `end_date` | DATE | |
| `is_current` | BOOLEAN | |
| `description` | VARCHAR(2000) | |
| `location` | VARCHAR(200) | |

### Table: `candidate_skills`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID | PK |
| `candidate_id` | UUID | FK → candidates (CASCADE) |
| `skill_name` | VARCHAR(100) | |
| `skill_category` | VARCHAR(100) | |
| `proficiency_level` | VARCHAR(50) | |
| `years_experience` | INT | |

### Table: `candidate_documents`

| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID | PK |
| `candidate_id` | UUID | FK → candidates (CASCADE) |
| `file_name` | VARCHAR(500) | |
| `file_type` | VARCHAR(50) | |
| `file_url` | VARCHAR(1000) | |
| `file_size_bytes` | BIGINT | |
| `is_primary` | BOOLEAN | |
| `uploaded_at` | TIMESTAMP | DEFAULT NOW() |

---

## 3. API Endpoints

### POST `/api/v1/hr/recruitment/upload-url`

- **Description**: Validates file metadata and returns a presigned URL for direct-to-storage upload.
- **Request Body**:
  ```json
  { "fileName": "cv.pdf", "fileType": "pdf", "fileSize": 100000, "fileHash": "abc123..." }
  ```
- **Validation**:
  1. `fileType` must be `pdf` or `docx` → 400 INVALID_FILE_TYPE
  2. `fileSize` ≤ 5MB → 413 FILE_TOO_LARGE
  3. `fileHash` must not exist for this tenant → 409 DUPLICATE_FILE
- **Response**:
  ```json
  { "presignedUrl": "https://...", "objectKey": "tenant/hash/cv.pdf", "fileHash": "abc123..." }
  ```
- **Authorization**: `HR:RecruitmentManage`

### POST `/api/v1/hr/recruitment/candidates`

- **Description**: Creates a candidate record after successful file upload.
- **Request Body**:
  ```json
  { "name": "John Doe", "email": "john@test.com", "fileUrl": "...", "fileHash": "...", ... }
  ```
- **Actions**:
  1. Validates email uniqueness per tenant → 400 DUPLICATE_EMAIL
  2. Creates candidate with status `DRAFT`
  3. Raises `CandidateUploaded` domain event
  4. On `DbUpdateException` (duplicate hash race): deletes orphaned S3 object → re-throws
- **Response**: `201 Created` with `{ "id": "uuid", ... }`
- **Authorization**: `HR:RecruitmentManage`

### GET `/api/v1/hr/recruitment/candidates`

- **Description**: Paginated candidate list with search and status filter.
- **Query Params**: `?search=john&status=DRAFT&page=1&pageSize=20`
- **Response**:
  ```json
  { "items": [...], "total": 50, "page": 1, "pageSize": 20 }
  ```
- **Authorization**: `HR:RecruitmentManage`

### GET `/api/v1/hr/recruitment/candidates/{id}`

- **Description**: Returns full candidate detail including education, experience, skills, documents.
- **Authorization**: `HR:RecruitmentManage`

---

## 4. Storage Provider

Configurable via `Storage:Provider` in `appsettings.json`:

| Provider | Description | File Path |
|----------|-------------|-----------|
| `Local` (default) | Stores files on disk under `uploads/` | `backend/FluxGrid.Api/uploads/` |
| `S3` | Uses MinIO/S3-compatible storage | Requires MinIO Docker service |

**Switching**: Change `"Provider": "Local"` to `"Provider": "S3"` and fill `Endpoint/AccessKey/SecretKey/BucketName`.

---

## 5. Domain Events

- **Raised**: `CandidateUploaded` — consumed by CV Parsing engine (HR-5) to trigger background extraction.
- **Event Payload**:
  ```csharp
  record CandidateUploaded(Guid CandidateId, string CandidateName, string Email,
    string FileName, string FileHash, long FileSizeBytes, Guid UploadedBy, Guid TenantId);
  ```

---

## 6. Permissions (RBAC)

- `HR:RecruitmentManage` — required to access all recruitment endpoints (upload, list, detail).
- Added to `Permissions.cs` and registered in `Program.cs` authorization policies.

---

## 7. File Upload Flow

```
User selects file(s) in DropzoneArea
  → Client-side validation (type: PDF/DOCX, size: ≤5MB)
  → SHA-256 hash computed via Web Crypto API
  → POST /api/v1/hr/recruitment/upload-url
  → Backend validates + checks duplicate hash
  → Returns presigned URL (5 min TTL)
  → XHR PUT directly to presigned URL (with progress tracking)
  → POST /api/v1/hr/recruitment/candidates
  → Candidate created as DRAFT + CandidateUploaded event raised
  → Candidate list refreshes
```

---

## 8. Security Considerations

- **File type enforcement**: Server validates `fileType` before presigned URL generation
- **Hash deduplication**: `UNIQUE(tenant_id, file_hash)` constraint prevents duplicate storage
- **Orphan cleanup**: Failed DB insert triggers S3 delete of uploaded file
- **Direct-to-storage**: Large files bypass backend (no server buffering)
- **Local mode**: Files stored on server disk, served through backend API (no public access)

---

## 9. Error Handling

| Scenario | HTTP Status | Handling |
|----------|-------------|----------|
| Invalid file type | 400 | Frontend shows error badge |
| File too large | 400 | Frontend shows error badge |
| Duplicate file hash | 409 | Frontend shows "already uploaded" message |
| Duplicate email | 400 | Frontend shows error |
| Upload fails (network) | — | Frontend shows retry option |
| DB insert fails after S3 upload | 500 | Backend deletes orphaned S3 file |

---

## 10. Dependencies

### Backend (NuGet)
- `Minio` 6.0.4 — S3-compatible storage SDK
- Existing: EF Core, Npgsql, JWT, etc.

### Frontend (npm)
- `react-dropzone` ^14.3.8 — drag-and-drop file upload
- Existing: `@tanstack/react-query`, `lucide-react`, `class-variance-authority`, etc.
