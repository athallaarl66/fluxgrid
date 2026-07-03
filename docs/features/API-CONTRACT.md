# API Contract & Security Specification

## Document Information
- **Document Version**: 2.0
- **Created Date**: 2026-06-29
- **Last Updated**: 2026-07-03
- **Author**: AI Engineer
- **Project**: FluxGrid ERP
- **Scope**: Complete ERP System (All Modules)

---

## 1. API Contract Specification

### 1.1 Standard Request Headers
All API requests must include:
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
X-Request-ID: {uuid}  // Optional, for tracing
```

### 1.2 Standard Response Headers
All API responses include:
```
Content-Type: application/json
X-Request-ID: {uuid}  // Echoed from request if provided
X-RateLimit-Remaining: {count}
X-RateLimit-Reset: {timestamp}
```

### 1.3 Base URL
The API is served from the **.NET Backend Service** deployed on Koyeb as a Docker container. The Next.js Frontend communicates with this backend via REST.

```
Production: https://fluxgrid-api.koyeb.app/api/v1
Staging: https://fluxgrid-api-staging.koyeb.app/api/v1
Development: http://localhost:5000/api/v1

Frontend (Next.js) URL:
Production: https://fluxgrid.koyeb.app
Development: http://localhost:3000

Module prefixes:
- WMS: /wms/
- Finance: /finance/
- HR: /hr/
- TaskProject: /task/

Full path example: https://fluxgrid-api.koyeb.app/api/v1/wms/stock-ledger
```

---

## 2. API Endpoints

### 2.1 WMS Endpoints

#### POST /wms/purchase-receipts
**Purpose:** Create purchase receipt for inbound goods

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "po_number": "PO-2026-001",
  "supplier_id": "uuid",
  "items": [
    {
      "item_id": "uuid",
      "quantity": 100,
      "unit_cost": 50000
    }
  ]
}
```

**Required Permission:** WMS:Write

---

#### POST /wms/pick-lists
**Purpose:** Generate pick list for outbound order

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "order_id": "uuid",
  "priority": "normal"
}
```

**Required Permission:** WMS:Write

---

#### GET /wms/stock-ledger
**Purpose:** Get stock ledger with movements

**Query Parameters:**
```
item_id: uuid (optional)
from_date: date (optional)
to_date: date (optional)
page: integer (optional, default: 1)
page_size: integer (optional, default: 20)
```

**Required Permission:** WMS:Read

---

#### GET /wms/inventory
**Purpose:** Get current inventory levels

**Query Parameters:**
```
location_id: uuid (optional)
sku: string (optional)
page: integer (optional, default: 1)
page_size: integer (optional, default: 20)
```

**Required Permission:** WMS:Read

---

### 2.2 Finance Endpoints

#### POST /finance/journal-entries
**Purpose:** Create journal entry

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "entry_date": "2026-06-29",
  "description": "Purchase of inventory",
  "lines": [
    {
      "account_id": "uuid",
      "debit": 5000000,
      "credit": 0
    },
    {
      "account_id": "uuid",
      "debit": 0,
      "credit": 5000000
    }
  ]
}
```

**Required Permission:** Finance:Write

---

#### GET /finance/chart-of-accounts
**Purpose:** Get chart of accounts hierarchy

**Query Parameters:**
```
parent_id: uuid (optional)
type: string (optional, enum: asset|liability|equity|revenue|expense)
```

**Required Permission:** Finance:Read

---

#### POST /finance/periods/{id}/close
**Purpose:** Close accounting period

**Path Parameters:**
```
id: uuid (required)
```

**Required Permission:** Finance:Admin

---

#### GET /finance/reports/trial-balance
**Purpose:** Generate Trial Balance report

**Query Parameters:**
```
period_id: uuid (required)
```

**Required Permission:** Finance:Read

---

#### GET /finance/reports/pl
**Purpose:** Generate Profit & Loss statement

**Query Parameters:**
```
period_id: uuid (required)
```

**Required Permission:** Finance:Read

---

#### GET /finance/reports/balance-sheet
**Purpose:** Generate Balance Sheet

**Query Parameters:**
```
period_id: uuid (required)
```

**Required Permission:** Finance:Read

---

### 2.3 HR Endpoints

#### GET /hr/employees
**Purpose:** List employees

**Query Parameters:**
```
department_id: uuid (optional)
status: string (optional, enum: active|inactive|terminated)
search: string (optional)
page: integer (optional, default: 1)
page_size: integer (optional, default: 20)
```

**Required Permission:** HR:Read

---

#### POST /hr/attendance/clock-in
**Purpose:** Clock in for attendance (PWA with GPS Geofencing & Face Recognition)

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "latitude": -6.2088,
  "longitude": 106.8456,
  "selfie_image": "data:image/jpeg;base64,...",
  "device_info": {
    "user_agent": "Mozilla/5.0...",
    "is_pwa": true
  }
}
```

**Notes:**
- `employee_id` is extracted from the JWT token (server-side), NOT from the client payload.
- `timestamp` is generated server-side via `DateTime.UtcNow` to prevent time spoofing.
- `latitude` / `longitude` are validated against the company's Geofence (configurable radius, e.g., 200 meters).
- `selfie_image` is processed via Face Recognition to match against the employee's enrolled photo.
- If the request comes from offline PWA sync, a `synced_at` field is added by the Service Worker.

**Required Permission:** HR:Write

---

#### POST /hr/attendance/clock-out
**Purpose:** Clock out for attendance (PWA with GPS Geofencing & Face Recognition)

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "latitude": -6.2088,
  "longitude": 106.8456,
  "selfie_image": "data:image/jpeg;base64,..."
}
```

**Notes:**
- Same validation rules as clock-in (GPS + Face Recognition).

**Required Permission:** HR:Write

---

#### POST /hr/payroll/process
**Purpose:** Process payroll for period

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "period_id": "uuid"
}
```

**Required Permission:** HR:PayrollProcess

---

#### POST /hr/recruitment/candidates/upload
**Purpose:** Upload CV file for AI processing

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: multipart/form-data
```

**Request Body (multipart/form-data):**
```
file: [binary file data]
file_name: string (required)
file_type: string (required, enum: pdf|docx|txt)
```

**Required Permission:** HR:CVWrite

---

#### GET /hr/recruitment/candidates
**Purpose:** List candidates

**Query Parameters:**
```
status: string (optional, enum: active|interview|hired|rejected|archived)
job_id: uuid (optional)
search: string (optional)
page: integer (optional, default: 1)
page_size: integer (optional, default: 20)
```

**Required Permission:** HR:CVRead

---

#### POST /hr/recruitment/jobs
**Purpose:** Create job posting

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "title": "Senior Software Engineer",
  "description": "Job description...",
  "required_skills": ["C#", ".NET", "SQL"],
  "min_experience_years": 5,
  "location": "Jakarta",
  "salary_min": 15000000,
  "salary_max": 25000000
}
```

**Required Permission:** HR:CandidateManage

---

### 2.4 TaskProject Endpoints

#### GET /task/projects
**Purpose:** List projects

**Query Parameters:**
```
status: string (optional, enum: active|completed|on-hold)
team_id: uuid (optional)
page: integer (optional, default: 1)
page_size: integer (optional, default: 20)
```

**Required Permission:** Task:Read

---

#### POST /task/projects/{id}/tasks
**Purpose:** Create task in project

**Path Parameters:**
```
id: uuid (required) - Project ID
```

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "title": "Implement feature X",
  "description": "Task description...",
  "assignee_id": "uuid",
  "priority": "high",
  "due_date": "2026-07-15"
}
```

**Required Permission:** Task:Write

---

#### PUT /task/tasks/{id}/status
**Purpose:** Update task status

**Path Parameters:**
```
id: uuid (required) - Task ID
```

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "status": "in-progress"
}
```

**Required Permission:** Task:Write

---

#### POST /task/tasks/{id}/time-logs
**Purpose:** Log time for task

**Path Parameters:**
```
id: uuid (required) - Task ID
```

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "employee_id": "uuid",
  "hours": 4.5,
  "date": "2026-06-29",
  "description": "Work completed"
}
```

**Required Permission:** Task:Write

---

#### GET /task/projects/{id}/kanban
**Purpose:** Get kanban board for project

**Path Parameters:**
```
id: uuid (required) - Project ID
```

**Required Permission:** Task:Read

---

### 2.5 Shared Endpoints

#### GET /auth/me
**Purpose:** Get current user info

**Request Headers:**
```
Authorization: Bearer {jwt_token}
```

**Required Permission:** Any authenticated user

---

#### POST /auth/logout
**Purpose:** Logout user

**Request Headers:**
```
Authorization: Bearer {jwt_token}
```

**Required Permission:** Any authenticated user

---

#### GET /audit-logs
**Purpose:** Get audit logs

**Query Parameters:**
```
entity_type: string (optional)
entity_id: uuid (optional)
from_date: date (optional)
to_date: date (optional)
page: integer (optional, default: 1)
page_size: integer (optional, default: 20)
```

**Required Permission:** Audit:Read

---

## 3. Error Handling

**Request Body (multipart/form-data):**
```
file: [binary file data]
file_name: string (required)
file_type: string (required, enum: pdf|docx|txt)
```

**Validation Rules:**
- file_type must be one of: pdf, docx, txt
- file_size <= 10MB (10485760 bytes)
- file_name max length: 255 characters

**Success Response (200):**
```json
{
  "success": true,
  "data": {
    "candidate_id": "550e8400-e29b-41d4-a716-446655440000",
    "status": "processing",
    "message": "CV uploaded and queued for parsing",
    "estimated_processing_time": 10
  }
}
```

**Error Responses:**
- 400: Invalid file type or size
- 401: Missing or invalid token
- 403: Insufficient permissions (requires HR:CVWrite)
- 413: File too large
- 500: Internal server error

**Required Permission:** HR:CVWrite

---

### 2.2 GET /hr/recruitment/candidates
**Purpose:** List candidates with filtering and pagination

**Request Headers:**
```
Authorization: Bearer {jwt_token}
```

**Query Parameters:**
```
status: string (optional, enum: active|interview|hired|rejected|archived)
job_id: uuid (optional, filter by job match)
search: string (optional, search in name/email/skills)
page: integer (optional, default: 1, min: 1)
page_size: integer (optional, default: 20, min: 1, max: 100)
sort_by: string (optional, default: created_at, enum: created_at|name|match_score)
sort_order: string (optional, default: desc, enum: asc|desc)
```

**Success Response (200):**
```json
{
  "success": true,
  "data": {
    "candidates": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "John Doe",
        "email": "john@example.com",
        "phone": "+6281234567890",
        "location": "Jakarta, Indonesia",
        "status": "active",
        "match_score": 0.85,
        "total_experience_months": 60,
        "created_at": "2026-06-29T10:00:00Z",
        "updated_at": "2026-06-29T10:00:00Z"
      }
    ],
    "pagination": {
      "total": 100,
      "page": 1,
      "page_size": 20,
      "total_pages": 5
    }
  }
}
```

**Error Responses:**
- 400: Invalid query parameters
- 401: Missing or invalid token
- 403: Insufficient permissions (requires HR:CVRead)

**Required Permission:** HR:CVRead

---

### 2.3 GET /hr/recruitment/candidates/{id}
**Purpose:** Get detailed candidate information

**Request Headers:**
```
Authorization: Bearer {jwt_token}
```

**Path Parameters:**
```
id: uuid (required)
```

**Success Response (200):**
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "John Doe",
    "email": "john@example.com",
    "phone": "+6281234567890",
    "location": "Jakarta, Indonesia",
    "linkedin_url": "https://linkedin.com/in/johndoe",
    "github_url": "https://github.com/johndoe",
    "portfolio_url": "https://johndoe.dev",
    "summary": "Software engineer with 5 years experience in .NET and React",
    "total_experience_months": 60,
    "expected_salary_min": 15000000,
    "expected_salary_max": 25000000,
    "notice_period_days": 30,
    "status": "active",
    "education": [
      {
        "id": "uuid",
        "institution": "Universitas Indonesia",
        "degree": "Sarjana Komputer",
        "field_of_study": "Computer Science",
        "start_date": "2015-09-01",
        "end_date": "2019-06-30",
        "gpa": 3.75
      }
    ],
    "experience": [
      {
        "id": "uuid",
        "company": "Tech Corp",
        "role": "Senior Software Engineer",
        "start_date": "2020-01-01",
        "end_date": null,
        "current": true,
        "description": "Led development of microservices architecture",
        "location": "Jakarta"
      }
    ],
    "skills": [
      {
        "id": "uuid",
        "skill_name": "C#",
        "skill_category": "technical",
        "proficiency_level": "expert",
        "years_experience": 5.0
      }
    ],
    "documents": [
      {
        "id": "uuid",
        "file_name": "resume.pdf",
        "file_type": "pdf",
        "file_size_bytes": 245678,
        "uploaded_at": "2026-06-29T10:00:00Z",
        "is_primary": true
      }
    ],
    "created_at": "2026-06-29T10:00:00Z",
    "updated_at": "2026-06-29T10:00:00Z"
  }
}
```

**Error Responses:**
- 401: Missing or invalid token
- 403: Insufficient permissions (requires HR:CVRead)
- 404: Candidate not found

**Required Permission:** HR:CVRead

---

### 2.4 POST /hr/recruitment/candidates/{id}/generate
**Purpose:** Generate AI content (summary or interview questions)

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Path Parameters:**
```
id: uuid (required)
```

**Request Body:**
```json
{
  "type": "summary",
  "job_id": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Validation Rules:**
- type must be one of: summary, interview_questions
- job_id is required for interview_questions, optional for summary

**Success Response (200):**
```json
{
  "success": true,
  "data": {
    "type": "summary",
    "content": "John Doe is an experienced software engineer with 5 years of expertise in .NET and React development...",
    "generated_at": "2026-06-29T10:00:00Z",
    "model_used": "llama-3.2-8b"
  }
}
```

**Error Responses:**
- 400: Invalid request body or type
- 401: Missing or invalid token
- 403: Insufficient permissions (requires HR:CVRead)
- 404: Candidate not found
- 429: Rate limit exceeded (Groq API)
- 500: AI generation failed

**Required Permission:** HR:CVRead

---

### 2.5 POST /hr/recruitment/jobs
**Purpose:** Create new job posting

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "title": "Senior Software Engineer",
  "description": "We are looking for a senior software engineer...",
  "requirements": "5+ years experience in .NET development...",
  "required_skills": ["C#", ".NET", "SQL", "React"],
  "min_experience_years": 5,
  "max_experience_years": 10,
  "location": "Jakarta",
  "salary_min": 15000000,
  "salary_max": 25000000,
  "status": "open"
}
```

**Validation Rules:**
- title: required, max 255 characters
- description: required, max 5000 characters
- required_skills: required, array of strings, max 50 items
- min_experience_years: required, min 0, max 50
- max_experience_years: optional, must be >= min_experience_years
- salary_min: optional, must be positive
- salary_max: optional, must be >= salary_min
- status: required, enum: open|closed|on-hold

**Success Response (201):**
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Senior Software Engineer",
    "description": "We are looking for a senior software engineer...",
    "requirements": "5+ years experience in .NET development...",
    "required_skills": ["C#", ".NET", "SQL", "React"],
    "min_experience_years": 5,
    "max_experience_years": 10,
    "location": "Jakarta",
    "salary_min": 15000000,
    "salary_max": 25000000,
    "status": "open",
    "created_at": "2026-06-29T10:00:00Z"
  }
}
```

**Error Responses:**
- 400: Invalid request body
- 401: Missing or invalid token
- 403: Insufficient permissions (requires HR:CandidateManage)

**Required Permission:** HR:CandidateManage

---

### 2.6 GET /hr/recruitment/jobs/{id}/matches
**Purpose:** Get ranked candidates for a job

**Request Headers:**
```
Authorization: Bearer {jwt_token}
```

**Path Parameters:**
```
id: uuid (required)
```

**Query Parameters:**
```
min_score: decimal (optional, default: 0.0, min: 0.0, max: 1.0)
limit: integer (optional, default: 50, min: 1, max: 100)
```

**Success Response (200):**
```json
{
  "success": true,
  "data": {
    "job_id": "550e8400-e29b-41d4-a716-446655440000",
    "job_title": "Senior Software Engineer",
    "matches": [
      {
        "candidate_id": "uuid",
        "candidate_name": "John Doe",
        "candidate_email": "john@example.com",
        "match_score": 0.85,
        "semantic_similarity": 0.90,
        "skill_match_score": 0.80,
        "experience_match_score": 0.85,
        "calculated_at": "2026-06-29T10:00:00Z"
      }
    ],
    "total": 45
  }
}
```

**Error Responses:**
- 400: Invalid query parameters
- 401: Missing or invalid token
- 403: Insufficient permissions (requires HR:CVRead)
- 404: Job not found

**Required Permission:** HR:CVRead

---

### 2.7 PUT /hr/recruitment/candidates/{id}/status
**Purpose:** Update candidate status

**Request Headers:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Path Parameters:**
```
id: uuid (required)
```

**Request Body:**
```json
{
  "status": "interview"
}
```

**Validation Rules:**
- status: required, enum: active|interview|hired|rejected|archived

**Success Response (200):**
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "status": "interview",
    "updated_at": "2026-06-29T10:00:00Z"
  }
}
```

**Error Responses:**
- 400: Invalid request body
- 401: Missing or invalid token
- 403: Insufficient permissions (requires HR:CandidateManage)
- 404: Candidate not found

**Required Permission:** HR:CandidateManage

---

## 3. Error Handling

### 3.1 Standard Error Response Format
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {
      "field": "Additional error context"
    }
  }
}
```

### 3.2 Error Codes
| Code | HTTP Status | Description |
|------|-------------|-------------|
| INVALID_REQUEST | 400 | Request validation failed |
| UNAUTHORIZED | 401 | Missing or invalid authentication token |
| FORBIDDEN | 403 | Insufficient permissions |
| NOT_FOUND | 404 | Resource not found |
| CONFLICT | 409 | Resource conflict (duplicate, etc.) |
| RATE_LIMIT_EXCEEDED | 429 | Rate limit exceeded |
| INTERNAL_ERROR | 500 | Internal server error |
| SERVICE_UNAVAILABLE | 503 | External service unavailable |

### 3.3 Specific Error Codes
| Code | Description |
|------|-------------|
| CANDIDATE_NOT_FOUND | Candidate with specified ID not found |
| JOB_NOT_FOUND | Job posting with specified ID not found |
| INVALID_FILE_TYPE | File type not supported |
| FILE_TOO_LARGE | File size exceeds limit |
| INVALID_STATUS | Invalid status value |
| DUPLICATE_EMAIL | Email already exists |
| AI_GENERATION_FAILED | Failed to generate AI content |
| GROQ_RATE_LIMIT | Groq API rate limit exceeded |

---

## 4. Security Specification

### 4.1 Authentication

#### 4.1.1 JWT Token Structure
```json
{
  "header": {
    "alg": "RS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user_id",
    "email": "user@example.com",
    "roles": ["HR:CVRead", "HR:CVWrite"],
    "tenant_id": "tenant_uuid",
    "iat": 1234567890,
    "exp": 1234567890 + 3600
  }
}
```

#### 4.1.2 Token Validation Rules
- Token must be signed with RS256 algorithm
- Token expiration: 1 hour (3600 seconds)
- Token refresh: Use refresh token to obtain new access token
- Token must include required permissions for the endpoint

#### 4.1.3 Authentication Flow
1. User logs in via NextAuth v5
2. Server generates JWT token with user permissions
3. Client stores token in httpOnly cookie (recommended) or memory
4. Client includes token in Authorization header for all requests
5. Server validates token signature and expiration
6. Server checks permissions for requested resource

### 4.2 Authorization (RBAC)

#### 4.2.1 Permission Definitions
| Permission | Description | Endpoints |
|-------------|-------------|-----------|
| HR:CVRead | View candidate data | GET /candidates, GET /candidates/{id}, GET /jobs/{id}/matches, POST /candidates/{id}/generate |
| HR:CVWrite | Upload and edit CV | POST /candidates/upload |
| HR:CandidateManage | Manage hiring workflow | POST /jobs, PUT /candidates/{id}/status |

#### 4.2.2 Permission Checking Logic
```csharp
// Pseudocode for permission check
public bool HasPermission(string requiredPermission, string[] userPermissions)
{
    return userPermissions.Contains(requiredPermission);
}

// Example endpoint protection
[HttpGet("{id}")]
[RequirePermission("HR:CVRead")]
public async Task<ActionResult<CandidateDetail>> GetCandidate(Guid id)
{
    // Endpoint logic
}
```

#### 4.2.3 Row-Level Security (RLS)
- Users can only access candidates within their tenant
- Tenant ID is extracted from JWT token
- Database queries automatically filter by tenant_id
- PostgreSQL RLS policies enforce tenant isolation

### 4.3 Data Encryption

#### 4.3.1 Encryption at Rest
- PostgreSQL: Neon managed encryption (AES-256)
- All PII data encrypted by default
- Encryption keys managed by Neon
- No plaintext storage of sensitive data

#### 4.3.2 Encryption in Transit
- TLS 1.3 for all HTTP connections
- Certificate validation enforced
- HSTS header enabled
- No insecure HTTP allowed

### 4.4 Input Validation

#### 4.4.1 File Upload Validation
```csharp
public class FileUploadValidator
{
    public bool ValidateFile(IFormFile file, string fileType)
    {
        // Check file size
        if (file.Length > 10 * 1024 * 1024) // 10MB
            return false;
        
        // Check file type
        var allowedTypes = new[] { "pdf", "docx", "txt" };
        if (!allowedTypes.Contains(fileType.ToLower()))
            return false;
        
        // Check file signature (magic bytes)
        var signature = GetFileSignature(file);
        if (!IsValidSignature(signature, fileType))
            return false;
        
        return true;
    }
}
```

#### 4.4.2 Input Sanitization
- SQL injection prevention: Parameterized queries only
- XSS prevention: Output encoding for all user-generated content
- CSRF protection: Anti-forgery tokens for state-changing operations
- Email validation: RFC-compliant email format validation
- UUID validation: Proper UUID format validation

### 4.5 Rate Limiting

#### 4.5.1 Rate Limit Configuration
| Endpoint | Rate Limit | Window |
|----------|------------|--------|
| POST /candidates/upload | 10 requests | 1 minute |
| GET /candidates | 100 requests | 1 minute |
| POST /candidates/{id}/generate | 20 requests | 1 minute |
| Other endpoints | 1000 requests | 1 hour |

#### 4.5.2 Rate Limit Headers
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1719672000
Retry-After: 60
```

### 4.6 Security Headers

#### 4.6.1 Required Headers
| Header | Value | Purpose |
|--------|-------|---------|
| Strict-Transport-Security | max-age=31536000; includeSubDomains | Enforce HTTPS |
| X-Content-Type-Options | nosniff | Prevent MIME sniffing |
| X-Frame-Options | DENY | Prevent clickjacking |
| Content-Security-Policy | default-src 'self'; script-src 'self' 'unsafe-inline' | Prevent XSS |
| X-XSS-Protection | 1; mode=block | XSS protection |
| Referrer-Policy | strict-origin-when-cross-origin | Control referrer info |
| Permissions-Policy | geolocation=(), microphone=(), camera=() | Restrict features |

### 4.7 Audit Logging

#### 4.7.1 Audit Log Structure
```json
{
  "id": "uuid",
  "timestamp": "2026-06-29T10:00:00Z",
  "user_id": "uuid",
  "tenant_id": "uuid",
  "action": "CREATE",
  "resource_type": "candidate",
  "resource_id": "uuid",
  "ip_address": "192.168.1.1",
  "user_agent": "Mozilla/5.0...",
  "changes": {
    "old_value": null,
    "new_value": {...}
  }
}
```

#### 4.7.2 Logged Actions
- All CREATE operations
- All UPDATE operations
- All DELETE operations
- All status changes
- Failed authentication attempts
- Permission denied attempts

### 4.8 Data Privacy

#### 4.8.1 PII Data Handling
- Email, phone, address classified as PII
- Access to PII requires HR:CVRead permission
- PII access logged in audit trail
- Data retention: 2 years for rejected candidates
- Right to deletion: Soft delete with actual deletion after retention period

#### 4.8.2 GDPR Compliance
- Explicit consent for data processing
- Data portability: Export candidate data on request
- Right to be forgotten: Complete deletion on request
- Data breach notification: Within 72 hours
- Privacy by design: Minimize data collection

### 4.9 API Security Best Practices

#### 4.9.1 Implementation Checklist
- [ ] All endpoints require authentication (except health check)
- [ ] All endpoints require appropriate permissions
- [ ] Input validation on all endpoints
- [ ] Output encoding for all responses
- [ ] SQL injection prevention via parameterized queries
- [ ] XSS prevention via output encoding
- [ ] CSRF protection for state-changing operations
- [ ] Rate limiting on all endpoints
- [ ] Security headers on all responses
- [ ] Audit logging for all sensitive operations
- [ ] Error messages don't leak sensitive information
- [ ] Secrets stored in environment variables
- [ ] Dependencies regularly updated
- [ ] Security scanning in CI/CD pipeline

#### 4.9.2 External API Security (Groq)
- API key stored in environment variable
- API key rotated every 90 days
- Rate limiting handled with retry logic
- Fallback mechanism if Groq unavailable
- No sensitive data sent to Groq (PII removed before sending)

---

## 5. Implementation Reference

### 5.1 .NET Backend Implementation

#### 5.1.1 Authentication Middleware
```csharp
public class JwtAuthenticationMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        
        if (!string.IsNullOrEmpty(token))
        {
            var principal = ValidateToken(token);
            if (principal != null)
            {
                context.User = principal;
            }
        }
        
        await _next(context);
    }
}
```

#### 5.1.2 Authorization Attribute
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = permission;
    }
}
```

#### 5.1.3 Endpoint Example
```csharp
[ApiController]
[Route("api/v1/candidates")]
[RequirePermission("HR:CVRead")]
public class CandidatesController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<CandidateDetail>> GetCandidate(Guid id)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var candidate = await _candidateService.GetByIdAsync(id, tenantId);
        
        if (candidate == null)
            return NotFound(new ErrorResponse
            {
                Code = "CANDIDATE_NOT_FOUND",
                Message = "Candidate not found"
            });
        
        return Ok(new SuccessResponse<CandidateDetail> { Data = candidate });
    }
}
```

### 5.2 Next.js Frontend Implementation

#### 5.2.1 API Client
```typescript
import { getAccessToken } from '@/lib/auth';

const apiClient = {
  async get(url: string, params?: any) {
    const token = await getAccessToken();
    const response = await fetch(`${API_BASE_URL}${url}`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });
    return response.json();
  },

  async post(url: string, body: any) {
    const token = await getAccessToken();
    const response = await fetch(`${API_BASE_URL}${url}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });
    return response.json();
  },
};
```

#### 5.2.2 Error Handling
```typescript
async function handleApiCall<T>(
  apiCall: () => Promise<T>
): Promise<T | null> {
  try {
    return await apiCall();
  } catch (error) {
    if (error.status === 401) {
      // Redirect to login
      router.push('/login');
    } else if (error.status === 403) {
      // Show permission denied
      toast.error('Insufficient permissions');
    } else {
      // Show generic error
      toast.error('An error occurred');
    }
    return null;
  }
}
```

---

## 6. Testing Checklist

### 6.1 Security Testing
- [ ] Test authentication with invalid token
- [ ] Test authentication with expired token
- [ ] Test authorization with insufficient permissions
- [ ] Test SQL injection attempts
- [ ] Test XSS attempts
- [ ] Test CSRF attempts
- [ ] Test rate limiting
- [ ] Test file upload with malicious files
- [ ] Test file size limit enforcement
- [ ] Test PII access logging

### 6.2 API Contract Testing
- [ ] Test all endpoints with valid requests
- [ ] Test all endpoints with invalid requests
- [ ] Test all error responses
- [ ] Test pagination
- [ ] Test filtering
- [ ] Test sorting
- [ ] Test file upload
- [ ] Test AI generation
- [ ] Test job matching

---

## 7. Change History

| Version | Date | Author | Description of Changes |
|---------|------|--------|----------------------|
| 1.0 | 2026-06-29 | AI Engineer | Initial version - API Contract & Security Specification |
