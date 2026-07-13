# Technical Specifications: Automatic CV Parsing (HR-5)

## 1. System Architecture
- **Trigger**: In-process fire-and-forget via `Task.Run` with `IServiceScopeFactory` in `RecruitmentService.CreateCandidateAsync`. Optional QStash webhook as alternative entry point.
- **PDF Extraction**: `UglyToad.PdfPig` (pure .NET, no native dependencies).
- **DOCX Extraction**: `DocumentFormat.OpenXml` (official Microsoft library).
- **AI Service Layer**: Groq API (Llama 3 70B) via `IHttpClientFactory` named `"GroqApi"`.
- **Database**: PostgreSQL with EF Core 8.
- **Storage**: Local filesystem or MinIO/S3 (configurable via `Storage:Provider`).
- **Frontend**: Next.js 16 split-screen review UI with `react-pdf`.

## 2. Database Schema

### Table: `candidates` (existing, extended)
| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID | PK |
| `name` | VARCHAR(200) | Overwritten by Groq first_name + last_name |
| `email` | VARCHAR(255) | Overwritten by Groq |
| `phone` | VARCHAR(30) | Overwritten by Groq |
| `location` | VARCHAR(200) | |
| `summary` | VARCHAR(2000) | Overwritten by Groq summary |
| `raw_text` | TEXT | Raw extracted PDF/DOCX text (added in HR-5) |
| `status` | VARCHAR(20) | DRAFT → PARSED / PARSE_FAILED → ACTIVE / REJECTED |
| `file_url` | VARCHAR(1000) | Download URL for CV file |
| `file_hash` | VARCHAR(64) | SHA-256 of uploaded file |
| `tenant_id` | UUID | Tenant isolation |
| *(other fields from HR-4)* | | |

### Table: `candidate_education` (existing)
| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID | PK |
| `candidate_id` | UUID | FK → candidates |
| `institution` | VARCHAR(200) | |
| `degree` | VARCHAR(100) | |
| `field_of_study` | VARCHAR(200) | |
| `start_date` | TIMESTAMP | |
| `end_date` | TIMESTAMP | |
| `gpa` | DECIMAL(3,2) | |

### Table: `candidate_experience` (existing)
| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID | PK |
| `candidate_id` | UUID | FK → candidates |
| `company` | VARCHAR(200) | |
| `role` | VARCHAR(100) | |
| `start_date` | TIMESTAMP | |
| `end_date` | TIMESTAMP | |
| `is_current` | BOOLEAN | |
| `description` | VARCHAR(2000) | |
| `location` | VARCHAR(200) | |

### Table: `candidate_skills` (existing)
| Column | Type | Notes |
|--------|------|-------|
| `id` | UUID | PK |
| `candidate_id` | UUID | FK → candidates |
| `skill_name` | VARCHAR(100) | |
| `skill_category` | VARCHAR(100) | |
| `proficiency_level` | VARCHAR(50) | |
| `years_experience` | INTEGER | |

## 3. Backend Service Architecture

### 3.1 PdfTextExtractor
- **File**: `Modules/HR/Application/PdfTextExtractor.cs`
- **Method**: `string ExtractText(byte[] pdfBytes)` — uses PdfPig to iterate pages and concatenate text.
- **Static Helper**: `bool IsScannedDocument(string text)` — returns true if text length < 50 characters.

### 3.2 DocxTextExtractor
- **File**: `Modules/HR/Application/DocxTextExtractor.cs`
- **Method**: `string ExtractText(byte[] docxBytes)` — uses DocumentFormat.OpenXml to extract `<Text>` elements from the document body.

### 3.3 GroqApiService
- **File**: `Modules/HR/Application/GroqApiService.cs`
- **Constructor**: Takes `IHttpClientFactory` (creates named client `"GroqApi"`) and `IConfiguration` (reads `Groq:ApiKey` and `Groq:Model`).
- **Method**: `Task<JsonElement> ParseCvTextAsync(string rawText, CancellationToken)`
  - PII redaction: regex replaces email/phone/address before sending.
  - Token truncation: approximates 4 chars per token, truncates to 4000 tokens.
  - Retry: exponential backoff up to 3 retries for 429/5xx errors.
  - Prompt: system + user message with strict JSON schema, `response_format: { type: "json_object" }`.
  - Response validation: parses JSON, falls back to stripping code fences.

### 3.4 CvParsingService
- **File**: `Modules/HR/Application/CvParsingService.cs`
- **Method**: `Task ParseCandidateAsync(Guid candidateId, Guid userId, Guid tenantId, ...)`
  - Loads candidate with Education/Experience/Skills navigation properties.
  - Reads file bytes from storage via `IFileStorageService.ReadFileAsync`.
  - Selects PdfTextExtractor or DocxTextExtractor based on `FileType`.
  - Checks for scanned document (< 50 chars) → sets PARSE_FAILED immediately.
  - Calls GroqApiService → validates response has `firstName` property.
  - Saves parsed data to Education/Experience/Skills tables.
  - Sets status to PARSED on success, PARSE_FAILED on any failure.
  - Logs audit events: `CV_PARSED` / `CV_PARSE_FAILED`.

## 4. Groq API Integration (Prompt Engineering)
- **Model**: `llama3-70b-8192` (configured via `Groq:Model`).
- **Auth**: Bearer token from `Groq:ApiKey` (set via `.env` or env var `Groq__ApiKey`).
- **Endpoint**: `POST https://api.groq.com/openai/v1/chat/completions`
- **Temperature**: 0.1
- **Response Format**: `{ type: "json_object" }`
- **Schema**:
```json
{
  "firstName": "...",
  "lastName": "...",
  "email": "...",
  "phone": "...",
  "summary": "...",
  "experience": [{ "company": "", "role": "", "startDate": "", "endDate": "", "isCurrent": false, "description": "", "location": "" }],
  "education": [{ "institution": "", "degree": "", "fieldOfStudy": "", "startDate": "", "endDate": "", "gpa": null }],
  "skills": [{ "skillName": "", "skillCategory": "", "proficiencyLevel": "", "yearsExperience": null }]
}
```

## 5. API Endpoints

### GET `/api/v1/hr/recruitment/candidates`
- Existing from HR-4. Supports `?status=PARSED` for review queue filtering.

### GET `/api/v1/hr/recruitment/candidates/{id}`
- Existing from HR-4. Returns full detail with education/experience/skills sub-entities.

### POST `/api/v1/hr/recruitment/parse-webhook`
- QStash target endpoint (optional — in-process trigger is default).
- Validates `Upstash-Signature` header via HMAC-SHA256 (QStashSignatureFilter).
- Body: `{ candidateId, tenantId, userId }`.
- Returns `{ status: "parsing_initiated" }`.

### PUT `/api/v1/hr/recruitment/candidates/{id}/approve`
- Changes status from PARSED → ACTIVE.
- Requires `HR:RecruitmentManage` permission.
- Returns `{ id, status, message }`.

### PUT `/api/v1/hr/recruitment/candidates/{id}/reject`
- Changes status from PARSED → REJECTED.
- Requires `HR:RecruitmentManage` permission.
- Returns `{ id, status, message }`.

### DELETE `/api/v1/hr/recruitment/candidates/{id}`
- Deletes candidate record and associated file from storage.
- Requires `HR:RecruitmentManage` permission.
- Returns `{ deleted: true }`.

## 6. Frontend — Split-Screen Review Page

### Route
- **Path**: `/hr/recruitment/{id}/review`
- **Shell**: `app/hr/recruitment/[id]/review/page.tsx`

### Components
| Component | File | Purpose |
|-----------|------|---------|
| `PdfViewerPane` | `components/hr/PdfViewerPane.tsx` | PDF rendering via react-pdf with page navigation, loading/error states |
| `CandidateReviewForm` | `components/hr/CandidateReviewForm.tsx` | Editable form: Personal Info, Education (add/delete), Experience (add/delete), Skills (tag input) |
| `SkillsInput` | `components/hr/SkillsInput.tsx` | Tokenized chip input with Enter to add, X to remove |
| `CandidateReviewTopBar` | `components/hr/CandidateReviewTopBar.tsx` | Header with name, status badge, Approve/Reject buttons with loading states |

### Layout
- Desktop: 50/50 flex split. Left = PDF viewer (collapsible). Right = editable form.
- Mobile (< 768px): PDF behind "View Original" modal button. Form takes full width.
- Each section in form displays `[Extracted]` tag with muted italic styling.

### Integrations
- Candidate table shows "Review" link for PARSED candidates.
- Candidate detail page shows "Review & Approve" card in sidebar.
- Approve/Reject buttons call PUT endpoints via TanStack Query mutations.
- Mutations invalidate `["candidates"]` and `["candidate", id]` queries on success.

## 7. Parsing Pipeline Flow

```
Upload CV (POST /candidates)
        │
        ▼
RecruitmentService.CreateCandidateAsync
  - Saves candidate as DRAFT
  - Raises CandidateUploaded event
  - Fires Task.Run (fire-and-forget with IServiceScopeFactory)
        │
        ▼
CvParsingService.ParseCandidateAsync
  1. Load candidate + navigation properties
  2. ReadFileAsync from storage
  3. Extract text (PdfPig / DocumentFormat.OpenXml)
  4. Save raw_text to candidate
  5. Check length < 50 chars? → PARSE_FAILED, stop
  6. PII redact → Truncate to 4000 tokens
  7. Call Groq API (retry up to 3x on 429/5xx)
  8. Validate response
  9. Replace Education/Experience/Skills in DB
  10. Set status = PARSED
  11. Audit log CV_PARSED
        │
        ▼
Recruiter reviews at /hr/recruitment/{id}/review
        │
        ├─ Approve → PUT /approve → status ACTIVE
        └─ Reject  → PUT /reject  → status REJECTED
```

## 8. Error Handling

| Scenario | Behavior |
|----------|----------|
| Scanned PDF (< 50 chars text) | Skips Groq, sets PARSE_FAILED immediately |
| Groq 429 rate limit | Retry with exponential backoff (2^attempt * random seconds). Max 3 retries. |
| Groq 5xx server error | Same retry logic |
| Groq returns invalid JSON | Strip code fences, retry parse. If still invalid → PARSE_FAILED. |
| Groq returns valid JSON missing firstName | Treated as invalid → PARSE_FAILED |
| File not found in storage | Exception caught → PARSE_FAILED |
| Any unhandled exception | Logged via ILogger → PARSE_FAILED |

## 9. Security Considerations

- **QStash webhook**: HMAC-SHA256 signature verified via `QStashSignatureFilter`.
- **Groq API key**: Loaded from `Groq__ApiKey` env var, never stored in DB.
- **PII redaction**: Email, phone, and address patterns are redacted before text leaves the server.
- **Permissions**: All recruitment endpoints require `HR:RecruitmentManage` policy.
- **Tenant isolation**: All queries filter by `TenantId`.

## 10. Performance Considerations

- **Fire-and-forget**: Parsing runs on background thread via `IServiceScopeFactory`, does not block HTTP response.
- **Token truncation**: Text capped at ~4000 tokens (16000 chars) before Groq call.
- **Retry backoff**: Exponential backoff prevents hammering Groq during rate limits.
- **DbContext scope**: Separate DI scope for background task avoids `ObjectDisposedException`.

## 11. Testing

| Layer | Tests | Location |
|-------|-------|----------|
| Unit (PII, truncation, scanned doc) | 14 | `tests/unit/hr/hr-5-automatic-cv-parsing.Test/TextExtractorTests.cs` + `GroqApiServiceTests.cs` |
| Unit (approve, reject, delete) | 10 | `tests/unit/hr/hr-5-automatic-cv-parsing.Test/RecruitmentServiceApprovalTests.cs` |
| Integration (full parsing pipeline) | TBD | Backend integration tests with mock Groq |
| E2E (upload → parse → review → approve) | TBD | Playwright E2E tests |
