# FluxGrid ERP — Technical Blueprint (v2.0)

> Modular Monolith ERP untuk industri berat: Mining, Oil & Gas, Logistics.  
> Dirancang sebagai produk Mini ERP (à la Odoo/SAP) dengan arsitektur production-grade.

---

## 1. Project Overview

| Field | Detail |
|---|---|
| **Nama Proyek** | FluxGrid ERP |
| **Arsitektur** | Modular Monolith — Clean Architecture + Domain-Driven Design (DDD) |
| **Target Industri** | Mining, Oil & Gas, Logistics, Manufacturing |
| **Deployment** | Monorepo Docker — Koyeb (Frontend Service + Backend Service + Managed DB) |
| **Tujuan** | Produk Mini ERP dan portofolio engineering-grade yang mencerminkan kapabilitas enterprise |

---

## 2. Tech Stack

### Backend
| Layer | Teknologi |
|---|---|
| Runtime | .NET 8 (C#) dengan AOT Compilation |
| Arsitektur | Clean Architecture — Domain / Application / Infrastructure / API |
| API Style | Minimal API + RESTful Endpoints |
| Auth | JWT Bearer Token + RBAC Middleware |

### Frontend
| Layer | Teknologi |
|---|---|
| Framework | Next.js 15 (TypeScript) |
| UI Library | shadcn/ui + Tailwind CSS |
| Data Fetching | TanStack Query (server state) |
| Auth Client | Auth.js (NextAuth v5) |

### Data & Infra
| Komponen | Teknologi | Keterangan |
|---|---|---|
| Database | Koyeb Managed PostgreSQL | Managed DB, pgvector, Row-Level Security (RLS) |
| Caching | Upstash Redis | Rate limiting, session cache, event queue |
| Auth Strategy | JWT Claims | Payload berisi roles & permissions |
| AI Engine | Groq (Cloud Free Tier) | Llama 3.2 / Mixtral - Gratis, super cepat, no hardware requirement |
| Deployment | Koyeb (Docker) | Frontend & Backend sebagai 2 Docker service terpisah |
| Repository | Monorepo | Satu repo GitHub, folder `/frontend` dan `/backend` |

---

## 3. Arsitektur Sistem

### 3.1 Struktur Modular Monolith

```
FluxGrid/                       # Monorepo
├── frontend/                   # Next.js 15 (TypeScript)
│   ├── app/
│   ├── components/
│   ├── hooks/
│   ├── Dockerfile
│   └── package.json
├── backend/                    # .NET 8 (C#)
│   ├── src/
│   │   ├── Modules/
│   │   │   ├── WMS/            # Warehouse Management
│   │   │   │   ├── Domain/
│   │   │   │   ├── Application/
│   │   │   │   ├── Infrastructure/
│   │   │   │   └── API/
│   │   │   ├── Finance/
│   │   │   ├── HR/
│   │   │   └── TaskProject/
│   │   ├── Shared/
│   │   │   ├── Kernel/         # Base entities, value objects
│   │   │   ├── Events/         # Domain events & contracts
│   │   │   ├── RBAC/           # Permission definitions
│   │   │   └── AuditTrail/     # Immutable log system
│   │   └── Host/               # Entry point, DI wiring
│   ├── tests/
│   │   ├── Unit/
│   │   └── Integration/
│   └── Dockerfile
├── docs/
├── docker-compose.yml
└── README.md
```

### 3.2 Komunikasi Antar Modul

Modul **tidak boleh** saling memanggil langsung. Semua komunikasi antar modul dilakukan via **Domain Events** melalui in-process event bus (MediatR). Ini menjaga loose coupling dan mempermudah ekstraksi ke microservice di masa depan jika diperlukan.

```
Finance Module
    └── raises → LedgerEntryCreated (event)
                    └── WMS Module listens → update stock valuation
                    └── Audit Module listens → log the change
```

### 3.3 RBAC — Role-Based Access Control

Permission berbasis scope granular, bukan hanya role level:

```
Permissions:
  Finance:Read
  Finance:Write
  Finance:Audit       ← khusus auditor
  WMS:Read
  WMS:Write
  WMS:Admin
  HR:Read
  HR:Write
  HR:PayrollProcess
  HR:CVRead           ← akses data kandidat
  HR:CVWrite          ← upload & edit CV
  HR:CandidateManage  ← hiring workflow
  Task:Read
  Task:Write
```

JWT payload menyimpan daftar permission aktif. Middleware memvalidasi di level endpoint sebelum request masuk ke application layer.

### 3.4 Audit Trail

Setiap perubahan data penting direkam dalam tabel `audit_logs` yang bersifat **immutable** (append-only, no update/delete). Wajib untuk kepatuhan industri mining dan oil & gas.

```sql
audit_logs (
  id          UUID PRIMARY KEY,
  module      TEXT,           -- 'Finance', 'WMS', dll
  entity_type TEXT,
  entity_id   UUID,
  action      TEXT,           -- 'CREATE', 'UPDATE', 'DELETE'
  actor_id    UUID,
  old_value   JSONB,
  new_value   JSONB,
  timestamp   TIMESTAMPTZ
)
```

---

## 4. Modul & Integrasi AI

### 4.1 WMS — Warehouse Management System

**Fokus domain:** Inbound/Outbound logistics, Stock Ledger, Inventory Valuation

**Fitur utama:**
- Stock Ledger dengan double-entry inventory (setiap gerakan barang punya pasangan jurnal)
- Inbound: Purchase Receipt → Putaway
- Outbound: Pick → Pack → Ship
- Valuation method: FIFO / Average Cost

---

### 4.2 Finance — General Ledger & Reporting

**Fokus domain:** Double-entry bookkeeping, Chart of Accounts, P&L, Balance Sheet

**Fitur utama:**
- Double-entry ledger: setiap transaksi menghasilkan minimal dua journal entry (debit = kredit)
- Period closing dengan lock mechanism

### 4.3 HR & Payroll

**Fokus domain:** Data karyawan, Absensi, Komponen gaji, Payroll engine, Recruitment

**Fitur utama:**
- Master data karyawan dengan struktur jabatan
- **Web-Based Attendance (PWA):**
  - GPS Geofencing: Membatasi akses absen hanya di area kantor
  - AI Face Recognition: Selfie sebagai bukti fisik untuk menghindari kecurangan
  - Offline Support: Data tersimpan sementara di device jika koneksi buruk (Service Worker)
  - Skip hardware tap — menggunakan fitur HP (GPS + Kamera) karena lebih murah dan terintegrasi langsung
- Mesin absensi berbasis rule (late tolerance, overtime)
- Payroll engine: take-home pay = base + allowances − deductions − tax (PPh 21)
- Slip gaji per periode
- **HR Recruitment** - Upload CV, parsing otomatis, candidate scoring

**HR Recruitment Pipeline:**
```
CV Upload → Text Extraction (PDF/DOCX) → LLM Parsing (Structured JSON)
→ Validation → Storage → Job Matching → Candidate Scoring
```

**Fitur HR Recruitment:**
- Multi-format CV upload (PDF, DOCX, TXT)
- Automatic field extraction (name, email, skills, experience, education)
- Semantic job matching dengan vector embeddings
- Candidate scoring (0-100 match percentage)
- Auto-generated candidate summaries
- Interview question generation

**AI Integration — HR:**
- **Analisis Produktivitas:** Korelasi data absensi + task completion rate
- **CV Parsing & Extraction:** LLM-based structured data extraction dari CV
- **Candidate-Job Matching:** Semantic similarity dengan vector embeddings
- **Interview Question Generator:** AI-generated pertanyaan berdasarkan CV + JD
- **Face Recognition:** Selfie vector matching untuk verifikasi identitas saat absen

**HR Recruitment Tech Stack:**
| Layer | Teknologi | Keterangan |
|---|---|---|
| Text Extraction | pdf-parse (Node.js) | PDF text extraction |
| LLM Inference | Groq (Llama 3.2 / Mixtral) | Cloud, gratis, super cepat |
| Embeddings | Groq Embeddings API | Vector generation |
| Vector Storage | pgvector (PostgreSQL) | Semantic search |
| Processing Queue | Upstash Redis | Async CV processing |
| Face Recognition | face-api.js / TensorFlow.js | Browser-based face detection & matching |

---

### 4.4 Task & Project Management

**Fokus domain:** Kanban board, Time tracking, Dependency antar task

**Fitur utama:**
- Board dengan status kustom per project
- Time log per task
- Dependency graph (task B tidak bisa mulai sebelum task A selesai)
- Integrasi ke HR untuk data produktivitas

---

## 5. Database Design — Prinsip Utama

- **Row-Level Security (RLS)** di Neon PostgreSQL: user hanya bisa akses baris yang sesuai tenant/permission-nya
- **Soft delete** untuk semua entitas utama (`deleted_at TIMESTAMPTZ`)
- **Optimistic concurrency** via `row_version` untuk mencegah konflik concurrent update
- **Read Models** terpisah untuk keperluan dashboard/analytics — tidak query langsung dari write model
- **pgvector extension** untuk vector embeddings (semantic search CV ATS)

### 5.1 HR Recruitment Database Schema

```sql
-- Main candidate table
CREATE TABLE candidates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  email TEXT NOT NULL UNIQUE,
  phone TEXT,
  location TEXT,
  linkedin_url TEXT,
  github_url TEXT,
  portfolio_url TEXT,
  summary TEXT,
  total_experience_months INT,
  expected_salary_min DECIMAL,
  expected_salary_max DECIMAL,
  notice_period_days INT,
  status TEXT DEFAULT 'active', -- active, hired, rejected, archived
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW(),
  deleted_at TIMESTAMPTZ
);

-- Education
CREATE TABLE candidate_education (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  candidate_id UUID REFERENCES candidates(id) ON DELETE CASCADE,
  institution TEXT NOT NULL,
  degree TEXT NOT NULL,
  field_of_study TEXT,
  start_date DATE,
  end_date DATE,
  gpa DECIMAL(3,2),
  description TEXT,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Work experience
CREATE TABLE candidate_experience (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  candidate_id UUID REFERENCES candidates(id) ON DELETE CASCADE,
  company TEXT NOT NULL,
  role TEXT NOT NULL,
  start_date DATE,
  end_date DATE,
  current BOOLEAN DEFAULT FALSE,
  description TEXT,
  location TEXT,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Skills
CREATE TABLE candidate_skills (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  candidate_id UUID REFERENCES candidates(id) ON DELETE CASCADE,
  skill_name TEXT NOT NULL,
  skill_category TEXT, -- technical, soft, language, tool
  proficiency_level TEXT, -- beginner, intermediate, advanced, expert
  years_experience DECIMAL(2,1),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Documents (original CVs)
CREATE TABLE candidate_documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  candidate_id UUID REFERENCES candidates(id) ON DELETE CASCADE,
  file_name TEXT NOT NULL,
  file_type TEXT NOT NULL,
  file_size_bytes INT,
  storage_path TEXT NOT NULL,
  uploaded_at TIMESTAMPTZ DEFAULT NOW(),
  is_primary BOOLEAN DEFAULT TRUE
);

-- Job descriptions (for matching)
CREATE TABLE job_postings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  title TEXT NOT NULL,
  description TEXT NOT NULL,
  requirements TEXT,
  required_skills TEXT[], -- array of skill names
  min_experience_years INT,
  max_experience_years INT,
  location TEXT,
  salary_min DECIMAL,
  salary_max DECIMAL,
  status TEXT DEFAULT 'open',
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Matching results (cache)
CREATE TABLE candidate_job_matches (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  candidate_id UUID REFERENCES candidates(id),
  job_id UUID REFERENCES job_postings(id),
  match_score DECIMAL(3,2), -- 0.00 to 1.00
  semantic_similarity DECIMAL(3,2),
  skill_match_score DECIMAL(3,2),
  experience_match_score DECIMAL(3,2),
  calculated_at TIMESTAMPTZ DEFAULT NOW(),
  UNIQUE(candidate_id, job_id)
);

-- Vector embeddings (using pgvector extension)
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE candidate_embeddings (
  candidate_id UUID PRIMARY KEY REFERENCES candidates(id),
  embedding vector(1536), -- OpenAI dimension, adjust for local model
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE job_embeddings (
  job_id UUID PRIMARY KEY REFERENCES job_postings(id),
  embedding vector(1536),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

## 6. AI Service Layer

Semua pemanggilan AI Engine diencapsulasi dalam `IAIService` di Application layer. Module tidak tahu apakah engine-nya OpenAI atau Anthropic — bisa diswap tanpa ubah domain logic.

```csharp
public interface IAIService
{
    Task<string> GenerateInsightAsync(string prompt, CancellationToken ct);
    Task<AnomalyResult> DetectAnomalyAsync(TransactionContext context, CancellationToken ct);


    // CV ATS Methods
    Task<CVParseResult> ParseCVAsync(byte[] cvBytes, string fileType, CancellationToken ct);
    Task<CandidateScore> ScoreCandidateAsync(CVData cv, JobDescription job, CancellationToken ct);
    Task<string> GenerateSummaryAsync(CVData cv, CancellationToken ct);
    Task<string> GenerateInterviewQuestionsAsync(CVData cv, JobDescription job, CancellationToken ct);
}
```

---

## 7. Roadmap Eksekusi

### Phase 1 — Foundation & Infrastructure
- [ ] Setup Monorepo structure (`/frontend` Next.js + `/backend` .NET 8)
- [ ] Siapkan Dockerfile untuk frontend dan backend
- [ ] Siapkan Koyeb Managed PostgreSQL + pgvector extension
- [ ] Deploy backend (.NET) ke Koyeb sebagai Docker service
- [ ] Deploy frontend (Next.js) ke Koyeb sebagai Docker service kedua
- [ ] Auth.js (NextAuth v5) + JWT + RBAC middleware
- [ ] Shared Kernel: base entity, domain event bus, audit trail
- [ ] CI/CD pipeline dasar (GitHub Actions → Docker Build → Koyeb Deploy)

### Phase 2 — Core Modules
- [ ] Finance: Chart of Accounts + Double-entry Ledger
- [ ] WMS: Stock Ledger + Inbound/Outbound flow
- [ ] Audit Trail terintegrasi ke kedua modul

### Phase 3 — Execution Modules
- [ ] Task & Project: Kanban board + time tracking + dependency
- [ ] HR: master data + absensi (PWA + GPS Geofencing + Face Recognition) + payroll engine
- [ ] HR: Recruitment - upload CV, text extraction, LLM parsing
- [ ] Integrasi HR → Payroll → Finance (payroll posting ke jurnal)

### Phase 4 — Intelligence Layer
- [ ] AI Service Layer abstraction
- [ ] Anomaly detection di Finance
- [ ] Productivity analytics di HR
- [ ] HR Recruitment AI: CV parsing, candidate-job matching, interview generation
- [ ] Face Recognition enrollment & verification
- [ ] Predictive dashboard di frontend

### Phase 5 — Production Hardening
- [ ] AOT Compilation optimization
- [ ] Performance testing (load test key endpoints)
- [ ] docker-compose.yml untuk local development
- [ ] Monitoring & observability setup (Sentry, Koyeb Logs)

---

## 8. Non-Functional Requirements

| Aspek | Target |
|---|---|
| **Latency** | P95 < 300ms untuk operasi read |
| **Availability** | 99.5% uptime (Koyeb auto-scaling) |
| **Security** | JWT expiry pendek + refresh token rotation |
| **Observability** | Structured logging (Serilog) + error tracking (Sentry) |
| **Skalabilitas** | Modul bisa diekstrak ke microservice tanpa ubah domain logic |
| **PWA Offline** | Attendance data sync otomatis saat koneksi kembali |
| **Deployment** | Docker-based, reproducible builds via Koyeb |

---

*FluxGrid ERP v2.0 — Mini ERP for industries where data integrity isn't optional. Powered by .NET 8 + Next.js 15, deployed on Koyeb.*