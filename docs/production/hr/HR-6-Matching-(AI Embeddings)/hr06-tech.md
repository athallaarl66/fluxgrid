# Technical Specifications: Job Matching (HR-6)

## 1. System Architecture

- **Backend API**: .NET 8 Minimal API (existing FluxGrid.Api project)
- **ORM**: Entity Framework Core 8 with Npgsql + pgvector
- **AI Service Layer**: Groq API `/v1/chat/completions` with `llama3-70b-8192` prompted to output 1536-dim vector (fallback: OpenAI `text-embedding-3-small`)
- **Database**: PostgreSQL (Neon) with `pgvector` extension
- **Embedding Index**: HNSW (Hierarchical Navigable Small World) on `embedding` columns
- **Frontend**: Next.js 16 (existing), tab-based UI under `/hr/recruitment/jobs`

### Module Structure

All new code lives under `backend/FluxGrid.Api/Modules/HR/`:

```
Modules/HR/
├── API/
│   ├── RecruitmentEndpoints.cs   ← modified: added job endpoints
│   └── JobPostingDtos.cs          ← new: request/response DTOs
├── Application/
│   ├── EmbeddingService.cs        ← new: Groq/OpenAI embedding generation
│   ├── JobPostingService.cs       ← new: CRUD + publish/close/matches
│   └── RecruitmentService.cs      ← modified: approve triggers embedding
├── Domain/
│   ├── Entities/
│   │   ├── JobPosting.cs          ← new entity
│   │   └── Candidate.cs           ← modified: added Embedding, EmbeddingStatus
│   └── Enums/
│       └── JobPostingStatus.cs    ← new: DRAFT, PUBLISHED, CLOSED
```

## 2. Database Schema

### Table: `job_postings`

| Column Name | Type | Constraints | Description |
|---|---|---|---|
| `id` | UUID | PRIMARY KEY, default `gen_random_uuid()` | |
| `title` | VARCHAR(200) | NOT NULL | e.g., "Senior React Developer" |
| `description` | TEXT | NOT NULL | Full JD text |
| `requirements` | TEXT | NULLABLE | Additional requirements |
| `required_skills` | TEXT[] | NOT NULL, default `{}` | Array of skill names |
| `min_experience_years` | INTEGER | NULLABLE | |
| `max_experience_years` | INTEGER | NULLABLE | |
| `location` | VARCHAR(200) | NULLABLE | |
| `salary_min` | NUMERIC(12,2) | NULLABLE | |
| `salary_max` | NUMERIC(12,2) | NULLABLE | |
| `status` | VARCHAR(20) | NOT NULL, default `'DRAFT'` | DRAFT, PUBLISHED, CLOSED |
| `embedding` | VECTOR(1536) | NULLABLE | Generated on publish |
| `tenant_id` | UUID | NOT NULL, FK → tenants | |
| `created_at` | TIMESTAMPTZ | NOT NULL, default `now()` | |
| `updated_at` | TIMESTAMPTZ | NOT NULL, default `now()` | |

### Table: `candidates` (modification)

| Column Name | Type | Constraints | Description |
|---|---|---|---|
| `embedding` | VECTOR(1536) | NULLABLE | Generated on approve |
| `embedding_status` | VARCHAR(20) | NULLABLE | `NULL` = success, `'PENDING'` = retry queued |

### Index

```sql
CREATE INDEX idx_job_postings_embedding ON job_postings USING hnsw (embedding vector_cosine_ops);
CREATE INDEX idx_candidates_embedding ON candidates USING hnsw (embedding vector_cosine_ops);
```

## 3. EF Core Entity: `JobPosting`

```csharp
public class JobPosting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Requirements { get; set; }
    public string[] RequiredSkills { get; set; } = [];
    public int? MinExperienceYears { get; set; }
    public int? MaxExperienceYears { get; set; }
    public string? Location { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Status { get; set; } = JobPostingStatus.Draft;
    public float[]? Embedding { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

## 4. Embedding Service

### Strategy

Uses Groq `/v1/chat/completions` with `llama3-70b-8192` prompted to return a JSON array of 1536 floats. If Groq fails (rate-limit, server error, invalid output format), falls back to OpenAI `text-embedding-3-small`.

### Text Composition

**Candidate text** (PII stripped): concatenates skills, experience descriptions, education fields only. Name, email, phone, URLs are excluded.

```
Skills: React, TypeScript, .NET.
Experience: 2020-Present Senior Frontend at Google. Built internal dashboard.
Education: BSc in Computer Science from UI.
```

**Job text**: concatenates title, description, requirements, required skills.

```
Title: Senior React Developer.
Description: Build and maintain frontend applications.
Requirements: 5+ years of experience.
Required Skills: React, TypeScript, Next.js.
```

### Key Methods

| Method | Description |
|---|---|
| `GenerateEmbeddingAsync(string text)` | Groq → fallback → OpenAI. Retries 3x with exponential backoff. |
| `GenerateMatchReasoningAsync(profile, jobDesc)` | 2-sentence AI explanation of candidate-job fit. |
| `ComposeCandidateText(Candidate)` | Static. Builds embedding text from skills/experience/education. |
| `ComposeJobText(JobPosting)` | Static. Builds embedding text from title/desc/requirements/skills. |

## 5. API Endpoints

All under `/api/v1/hr/recruitment/jobs`.

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/jobs` | `HR:JobRead` | List job postings (paginated, filterable by status/search) |
| `POST` | `/jobs` | `HR:JobManage` | Create new job posting (status = DRAFT) |
| `GET` | `/jobs/{id}` | `HR:JobRead` | Get job by ID |
| `PUT` | `/jobs/{id}` | `HR:JobManage` | Update job fields |
| `DELETE` | `/jobs/{id}` | `HR:JobManage` | Delete job (DRAFT only) |
| `POST` | `/jobs/{id}/publish` | `HR:JobManage` | Generate embedding, set status → PUBLISHED |
| `POST` | `/jobs/{id}/close` | `HR:JobManage` | Set status → CLOSED |
| `GET` | `/jobs/{id}/matches` | `HR:JobRead` | Ranked candidates via cosine similarity |
| `POST` | `/jobs/{id}/matches/{candidateId}/reasoning` | `HR:JobRead` | AI reasoning for a specific match |

### DTOs

```csharp
sealed record CreateJobRequest(string Title, string Description, string? Requirements, ...);
sealed record UpdateJobRequest(string? Title, string? Description, ...);
sealed record JobResponse(Guid Id, string Title, string Description, ..., string Status, ...);
sealed record PublishJobResponse(Guid Id, string Status, string Message);
sealed record JobMatchItem(Guid CandidateId, string CandidateName, double MatchScore, ...);
sealed record JobMatchResponse(Guid JobId, string JobTitle, List<JobMatchItem> Matches);
sealed record MatchReasoningResponse(Guid CandidateId, string CandidateName, double MatchScore, string Reasoning);
```

## 6. Job Matching Query

Matching is performed in-memory using cosine similarity (C# `Math`), not raw SQL `<=>`:

```
score = dot(a, b) / (sqrt(sum(a²)) * sqrt(sum(b²)))
```

### Query Flow

1. Validate job exists and is PUBLISHED
2. If job has no embedding → return empty matches
3. Load active candidates with embeddings for the same tenant
4. Compute cosine similarity for each candidate
5. Filter by `min_score` (optional), sort descending, apply `limit` (default 20, max 100)
6. Return `JobMatchResponse` with ranked items

### Tenant Isolation

Enforced at DB query level: `WHERE tenant_id = @TenantId` for both job lookup and candidate retrieval.

## 7. Permissions (RBAC)

| Permission | Description |
|---|---|
| `HR:JobRead` | View job postings and candidate matches |
| `HR:JobManage` | Create, update, delete, publish, close jobs |

- Follows existing `Admin` super admin bypass pattern (Admin = all permissions).
- Tenant-scoped: users can only access jobs belonging to their tenant.

## 8. Embedding Trigger Points

| Event | Action |
|---|---|
| `POST /jobs/{id}/publish` | Compose job text → generate embedding → save → set PUBLISHED |
| `ApproveCandidateAsync` (PARSED → ACTIVE) | Compose candidate text → generate embedding → save |

### Failure Handling

- **Job publish**: embedding failure → stays DRAFT, returns error message. No retry.
- **Candidate approve**: embedding failure → status ACTIVE, `embedding_status = 'PENDING'`, background retry queued (3 attempts with 5s/25s/125s delays).

## 9. Audit Logging

All job mutations log via `AuditService.LogAsync`:

| Action | `resource_type` | Details Logged |
|---|---|---|
| CREATE | `job_postings` | null → new job object |
| UPDATE | `job_postings` | old values → new values |
| DELETE | `job_postings` | deleted job object → null |
| PUBLISH | `job_postings` | previousStatus=DRAFT → newStatus=PUBLISHED |
| CLOSE | `job_postings` | previousStatus=PUBLISHED → newStatus=CLOSED |
| APPROVE | `candidates` | previousStatus=PARSED → newStatus=ACTIVE + embeddingGenerated |

## 10. Performance Considerations

- HNSW index on `embedding` columns (faster query, higher memory usage than IVFFlat)
- Candidate match query loads all active candidates into memory for cosine computation (acceptable for ~10K candidates, revisit if dataset grows)
- `limit` caps at 100 to prevent excessive response size
- Embedding generation only on publish/approve, not on draft saves or edits

## 11. Security Considerations

- PII (name, email, phone, LinkedIn/GitHub URLs) excluded from embedding text — enforced by `ComposeCandidateText` which only uses skills, experience, education
- Tenant isolation in all queries via `WHERE tenant_id = @TenantId`
- All job endpoints protected by `RequirePermission` filter
- Input validation: title required (max 255 chars), salary must be non-negative, experience years 0–50

## 12. Error Handling

| Scenario | Behaviour |
|---|---|
| Job not found | 404 via `InvalidOperationException` |
| Wrong status for operation | 400 with descriptive message |
| Embedding API down (publish) | Stays DRAFT, returns message "Failed to generate AI search index" |
| Embedding API down (approve) | ACTIVE with PENDING status, background retry |
| PII accidentally in text | Prevented by design; `ComposeCandidateText` never includes PII fields |
| Tenant mismatch | Empty result / 404 (tenant_id filtered in query) |
| Invalid input fields | Validation error via `ValidationFilter` |

## 13. Unit Tests

43 xUnit tests covering:

- Embedding text composition (PII exclusion, null handling, field combinations)
- Job CRUD with tenant isolation and status guards
- Publish/close workflow (embedding generation, failure handling)
- Cosine similarity matching (ranking, min_score, limit, status guards)
- Match reasoning API
- Candidate approve with embedding generation and retry queue

Test project: `tests/unit/hr/hr-6-matching-ai-embeddings.Test/`
