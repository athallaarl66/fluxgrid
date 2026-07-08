# Product Requirements Document (PRD)

## Document Information
- **Document Version**: 3.0
- **Created Date**: 2026-06-29
- **Last Updated**: 2026-07-08
- **Author**: AI Engineer
- **Project**: FluxGrid ERP
- **Scope**: Complete ERP System (WMS, Finance, HR)

---

## 1. Executive Summary

### 1.1 Purpose
FluxGrid ERP adalah sistem Modular Monolith untuk industri berat (Mining, Oil & Gas, Logistics, Manufacturing) yang menyediakan end-to-end enterprise resource management. Sistem ini terdiri dari 3 modul: WMS (Warehouse Management), Finance (General Ledger), HR & Payroll. Semua modul berkomunikasi melalui Domain Events untuk menjaga loose coupling dan memungkinkan ekstraksi ke microservice di masa depan.

> **Catatan:** Modul **Task & Project Management** (kanban, time tracking, task dependencies) telah di-extract menjadi standalone app terpisah dengan Go backend. Lihat [`TASK-APP.md`](../TASK-APP.md) untuk dokumentasi lengkap.

### 1.2 Scope
**Included Modules:**

**1. WMS - Warehouse Management System**
- Stock Ledger dengan double-entry inventory
- Inbound: Purchase Receipt → Putaway
- Outbound: Pick → Pack → Ship
- Valuation method: FIFO / Average Cost

**2. Finance - General Ledger & Reporting**
- Double-entry ledger (debit = kredit)
- Chart of Accounts management
- Period closing dengan lock mechanism
- Laporan: Trial Balance, P&L, Balance Sheet
- Budget Management & Dashboard

**3. HR & Payroll**
- Master data karyawan dengan struktur jabatan
- **Web-Based Attendance (PWA):** GPS Geofencing, AI Face Recognition, Offline Support
- Mesin absensi berbasis rule (late tolerance, overtime)
- Payroll engine: take-home pay = base + allowances − deductions − tax (PPh 21)
- Slip gaji per periode
- HR Recruitment: Upload CV, parsing otomatis, candidate scoring
- AI Integration: CV Parsing, Candidate-Job Matching, Interview Generation, Productivity Analytics, Face Recognition

**Shared Features:**
- **Monorepo:** Frontend (Next.js 15) + Backend (.NET 8) dalam satu repository
- **Deployment:** Docker-based pada Koyeb (2 services + Managed PostgreSQL)
- Modular Monolith architecture dengan Clean Architecture dan DDD
- Domain Events untuk komunikasi antar modul
- RBAC (Role-Based Access Control) dengan granular permissions
  - Role **Admin = Super Admin**: bypass semua permission check (policy OR logic di `Program.cs`)
- Audit Trail immutable untuk compliance
- Row-Level Security (RLS) di PostgreSQL
- AI Service Layer abstraction (Groq API) — hanya untuk HR

**Future — User & Role Management:**
Super Admin akan dapat membuat akun, mengelola role, dan assign permission secara dinamis melalui UI. Fitur ini mencakup:
- CRUD user
- CRUD role dengan permission picker
- Assign/unassign role ke user
- Audit log untuk semua perubahan RBAC

**Excluded:**
- Video interview integration
- Automated email sequences
- Social media scraping
- Background check integration
- CRM (Customer Relationship Management)
- Supply Chain Management (beyond warehouse)

### 1.3 Business Objectives
- Menyediakan sistem ERP terintegrasi untuk industri Mining, Oil & Gas, Logistics, Manufacturing
- Mengurangi manual work melalui automation dan AI-powered features
- Meningkatkan akurasi data keuangan dengan double-entry ledger
- Optimasi inventory management dengan stock alerts dan real-time tracking
- Mempercepat proses recruitment dengan AI-powered CV parsing dan matching
- Mendemonstrasikan arsitektur Modular Monolith dengan Domain Events untuk portofolio engineering-grade
- Menyediakan learning platform untuk transisi ke AI engineering

---

## 2. Background & Context

### 2.1 Current Situation
Saat ini, operasional perusahaan di industri Mining, Oil & Gas, Logistics, Manufacturing menggunakan berbagai sistem terpisah:
- **Warehouse:** Manual inventory tracking, spreadsheet-based stock management
- **Finance:** Manual journal entries, spreadsheet-based accounting, tidak ada real-time reporting
- **HR:** Manual payroll calculation, spreadsheet-based employee data, manual CV screening
- **Project:** Manual task tracking, tidak ada visibility ke productivity, manual time logging

### 2.2 Problem Statement
- **Data Silos:** Setiap departemen menggunakan sistem terpisah, tidak ada integrasi data
- **Manual Processes:** Banyak proses yang masih manual dan paper-based
- **Lack of Visibility:** Tidak ada real-time visibility ke seluruh operasional perusahaan
- **Human Error:** Manual data entry menyebabkan kesalahan dan inkonsistensi
- **Inefficient Decision Making:** Tidak ada data-driven decision making karena data tidak terstruktur
- **Compliance Risk:** Tidak ada audit trail yang proper untuk kepatuhan industri

### 2.3 Business Case
**ROI Estimation:**
- **WMS:** 40% reduction dalam stock-out incidents, 30% improvement dalam inventory turnover
- **Finance:** 50% reduction dalam closing time, 90% reduction dalam manual journal entry errors
- **HR:** 70% reduction dalam screening time, 100% accuracy dalam payroll calculation
- **Overall:** 35% operational efficiency improvement, 20% cost reduction

**Strategic Value:**
- Single source of truth untuk seluruh operasional perusahaan
- Real-time visibility ke seluruh business processes
- Scalable architecture untuk future growth
- AI-powered insights untuk better decision making
- Compliance-ready dengan audit trail dan RLS

**Target Industries:** Mining, Oil & Gas, Logistics, Manufacturing - industri dengan kompleksitas operasional tinggi dan kebutuhan compliance yang ketat.

---

## 3. Stakeholder Analysis

| Stakeholder | Role | Interest | Influence |
|-------------|------|----------|-----------|
| CEO | Strategic Decision Maker | High | High |
| CFO | Finance User | High | High |
| Warehouse Manager | WMS User | High | High |
| HR Manager | HR User | High | High |
| Finance Staff | Finance Daily User | High | Medium |
| Warehouse Staff | WMS Daily User | High | Medium |
| HR Staff | HR Daily User | High | Medium |
| IT Team | Technical Support | Medium | Medium |
| External Auditor | Compliance | High | High |
| Management | Strategic Oversight | High | High |

**CEO:** Strategic oversight, needs real-time visibility ke seluruh operasional
**CFO:** Financial reporting, budget management, compliance
**Warehouse Manager:** Inventory management, stock optimization, warehouse operations
**HR Manager:** Employee management, payroll, recruitment
**Finance Staff:** Daily accounting operations, journal entries, reporting
**Warehouse Staff:** Daily warehouse operations, inbound/outbound processing
**HR Staff:** Daily HR operations, attendance, payroll processing
**IT Team:** Maintenance, integration, system administration
**External Auditor:** Compliance, audit trail access, reporting
**Management:** Strategic decisions, budget approval, resource planning

---

## 4. Functional Requirements

### 4.1 User Stories by Module

#### WMS - Warehouse Management System

**User Story WMS-1: Stock Ledger Management**
**As a** Warehouse Manager
**I want** to maintain a double-entry stock ledger
**So that** every inventory movement is tracked with proper debit/credit entries

**Acceptance Criteria:**
- [ ] Every stock movement creates paired journal entries (in/out)
- [ ] Support multiple valuation methods (FIFO, Average Cost)
- [ ] Real-time stock balance calculation
- [ ] Audit trail untuk semua perubahan

**Priority:** Must Have

**User Story WMS-2: Inbound Processing**
**As a** Warehouse Staff
**I want** to process purchase receipts and putaway
**So that** incoming goods are properly recorded and stored

**Acceptance Criteria:**
- [ ] Purchase receipt creation with PO reference
- [ ] Quality check recording
- [ ] Putaway location assignment
- [ ] Automatic stock ledger update

**Priority:** Must Have

**User Story WMS-3: Outbound Processing**
**As a** Warehouse Staff
**I want** to process pick, pack, and ship operations
**So that** outbound orders are fulfilled accurately

**Acceptance Criteria:**
- [ ] Pick list generation
- [ ] Packing verification
- [ ] Shipping confirmation
- [ ] Automatic stock ledger update

**Priority:** Must Have



#### Finance - General Ledger & Reporting

**User Story FIN-1: Chart of Accounts Management**
**As a** CFO
**I want** to manage the chart of accounts
**So that** financial transactions are properly categorized

**Acceptance Criteria:**
- [ ] Create/edit/delete account codes
- [ ] Account hierarchy (assets, liabilities, equity, revenue, expenses)
- [ ] Account type validation
- [ ] Audit trail untuk perubahan

**Priority:** Must Have

**User Story FIN-2: Journal Entry Management**
**As a** Finance Staff
**I want** to create and manage journal entries
**So that** all financial transactions are recorded

**Acceptance Criteria:**
- [ ] Double-entry validation (debit = credit)
- [ ] Journal entry approval workflow
- [ ] Batch journal entry creation
- [ ] Automatic posting to ledger

**Priority:** Must Have

**User Story FIN-3: Period Closing**
**As a** CFO
**I want** to close accounting periods
**So that** financial statements can be generated

**Acceptance Criteria:**
- [ ] Period lock mechanism
- [ ] Pre-close validation checks
- [ ] Closing journal entries
- [ ] Re-opening capability with audit trail

**Priority:** Must Have

**User Story FIN-4: Financial Reporting**
**As a** CFO
**I want** to generate financial reports
**So that** I can analyze company financial performance

**Acceptance Criteria:**
- [ ] Trial Balance report
- [ ] Profit & Loss statement
- [ ] Balance Sheet
- [ ] Custom date range filtering

**Priority:** Must Have

**User Story FIN-5: Budget Management & Dashboard**
**As a** CFO
**I want** to manage budgets per account/period and view a financial dashboard with KPIs
**So that** I can track performance against plan at a glance

**Acceptance Criteria:**
- [ ] Budget CRUD per account and period
- [ ] Budget vs Actual variance report with flagging
- [ ] Financial dashboard with KPI cards and charts
- [ ] Recent entries and monthly trend data

**Priority:** Must Have

---

#### HR & Payroll

**User Story HR-1: Employee Data Management**
**As a** HR Manager
**I want** to manage employee master data
**So that** employee information is centralized and up-to-date

**Acceptance Criteria:**
- [ ] Employee profile creation
- [ ] Organizational structure management
- [ ] Position and salary grade management
- [ ] Employment history tracking

**Priority:** Must Have

**User Story HR-2: Attendance Management**
**As a** HR Staff
**I want** to track employee attendance
**So that** payroll calculation is accurate

**Acceptance Criteria:**
- [ ] Clock in/out recording
- [ ] Late tolerance rules
- [ ] Overtime calculation
- [ ] Leave management

**Priority:** Must Have

**User Story HR-3: Payroll Processing**
**As a** HR Manager
**I want** to process payroll automatically
**So that** employees are paid accurately and on time

**Acceptance Criteria:**
- [ ] Take-home pay calculation (base + allowances - deductions - tax)
- [ ] PPh 21 tax calculation
- [ ] Payslip generation
- [ ] Payroll posting to Finance module

**Priority:** Must Have

**User Story HR-4: CV Upload**
**As a** Recruiter
**I want** to upload CV dalam berbagai format (PDF, DOCX, TXT)
**So that** saya bisa memproses kandidat tanpa konversi manual

**Acceptance Criteria:**
- [ ] Mendukung PDF, DOCX, TXT format
- [ ] Max file size 10MB
- [ ] Drag & drop upload interface
- [ ] Progress indicator saat upload

**Priority:** Must Have

**User Story HR-5: Automatic CV Parsing**
**As a** Recruiter
**I want** CV di-parse otomatis untuk extract data kandidat
**So that** saya tidak perlu input data manual

**Acceptance Criteria:**
- [ ] Extract: name, email, phone, location
- [ ] Extract: education, experience, skills
- [ ] Output dalam structured JSON format
- [ ] Confidence score per field

**Priority:** Must Have

**User Story HR-6: Job Matching**
**As a** Hiring Manager
**I want** sistem match kandidat dengan job description
**So that** saya dapat kandidat yang paling relevan

**Acceptance Criteria:**
- [ ] Semantic similarity scoring (0-100)
- [ ] Skill matching
- [ ] Experience level matching
- [ ] Ranked candidate list per job

**Priority:** Must Have

**User Story HR-7: Productivity Analytics**
**As a** HR Manager
**I want** to analyze employee productivity
**So that** I can identify top performers and areas for improvement

**Acceptance Criteria:**
- [ ] Correlate attendance data with task completion
- [ ] Productivity score calculation
- [ ] Performance trend analysis
- [ ] Integration with Task App module (standalone, see TASK-APP.md)

**Priority:** Should Have



### 4.2 Functional Requirements by Module

#### WMS Module
- **FR-WMS-1:** System harus maintain stock ledger dengan double-entry inventory
- **FR-WMS-2:** System harus support FIFO dan Average Cost valuation methods
- **FR-WMS-3:** System harus process purchase receipts dengan quality check
- **FR-WMS-4:** System harus assign putaway locations secara otomatis
- **FR-WMS-5:** System harus generate pick lists berdasarkan order
- **FR-WMS-6:** System harus verify packing sebelum shipping


#### Finance Module
- **FR-FIN-1:** System harus manage chart of accounts dengan hierarchy
- **FR-FIN-2:** System harus enforce double-entry validation (debit = credit)
- **FR-FIN-3:** System harus support journal entry approval workflow
- **FR-FIN-4:** System harus lock accounting periods setelah closing
- **FR-FIN-5:** System harus generate Trial Balance, P&L, dan Balance Sheet
- **FR-FIN-6:** System harus support budget management per account dan period
- **FR-FIN-7:** System harus generate LLM summaries untuk fraud alerts
- **FR-FIN-8:** System harus post payroll entries dari HR module

#### HR Module
- **FR-HR-1:** System harus manage employee master data dengan organizational structure
- **FR-HR-2:** System harus track attendance dengan clock in/out
- **FR-HR-3:** System harus calculate overtime berdasarkan company rules
- **FR-HR-4:** System harus process payroll dengan PPh 21 calculation
- **FR-HR-5:** System harus generate payslips per periode
- **FR-HR-6:** System harus accept CV uploads (PDF, DOCX, TXT)
- **FR-HR-7:** System harus parse CV menggunakan Groq API
- **FR-HR-8:** System harus match candidates dengan job descriptions
- **FR-HR-9:** System harus correlate attendance dengan task completion untuk productivity

#### Shared Kernel
- **FR-SHARED-1:** System harus enforce RBAC dengan granular permissions
- **FR-SHARED-2:** System harus maintain audit trail immutable
- **FR-SHARED-3:** System harus support Row-Level Security (RLS) di PostgreSQL
- **FR-SHARED-4:** System harus communicate antar modul via Domain Events
- **FR-SHARED-5:** System harus abstract AI service layer (Groq API) untuk HR modules

---

## 5. Non-Functional Requirements

### 5.1 Performance Requirements
- **WMS:** Stock ledger update < 1 second, Inbound/Outbound processing < 3 seconds
- **Finance:** Journal entry posting < 2 seconds, Financial report generation < 5 seconds
- **HR:** Attendance recording < 1 second, Payroll processing < 30 seconds (async), CV upload < 2 seconds, CV parsing < 10 seconds (async)
- **Overall:** Support 100+ concurrent users, P95 latency < 300ms untuk read operations

### 5.2 Security Requirements
- All data encrypted at rest (PostgreSQL)
- All data encrypted in transit (TLS 1.3)
- RBAC enforcement untuk semua operations dengan granular permissions
- Row-Level Security (RLS) di PostgreSQL untuk tenant isolation
- Audit trail immutable untuk semua data changes
- Rate limiting untuk API endpoints
- Input validation untuk prevent injection attacks
- PII access logged dengan audit trail
- Data retention policy sesuai compliance requirements

### 5.3 Scalability Requirements
- Support 10,000+ inventory items (WMS)
- Support 100,000+ journal entries (Finance)
- Support 1,000+ employees (HR)
- Support 100+ concurrent users
- Horizontal scaling via serverless architecture
- Database connection pooling untuk high concurrency
- Async processing queue untuk heavy operations (payroll, CV parsing, report generation)
- Vector search optimized dengan pgvector indexes (untuk HR recruitment)

### 5.4 Reliability & Availability
- 99.5% uptime target
- Graceful degradation untuk HR external API (Groq)
- Retry logic dengan exponential backoff untuk external APIs
- Database backups daily
- Error monitoring dan alerting
- Data consistency checks untuk double-entry systems (WMS, Finance)

### 5.5 Usability Requirements
- Intuitive UI dengan consistent patterns across all modules
- Responsive design untuk tablet dan desktop
- Accessibility: WCAG 2.1 AA compliance
- Loading states untuk async operations
- Error boundaries untuk graceful error handling
- Progressive disclosure untuk complex data
- Keyboard shortcuts untuk power users
- Onboarding guide untuk new users
- Mobile-friendly untuk basic operations (attendance, time tracking)

---

## 6. User Interface Requirements

### 6.1 Screen/Page Requirements

**WMS Pages:**
- **Stock Ledger Page:** Table view dengan inventory movements, balance calculation, valuation method toggle
- **Inbound Processing Page:** Purchase receipt form, quality check recording, putaway location assignment
- **Outbound Processing Page:** Pick list generation, packing verification, shipping confirmation
- **Dashboard Page:** Inventory overview, stock-out alerts

**Finance Pages:**
- **Chart of Accounts Page:** Account hierarchy tree, create/edit account forms
- **Journal Entry Page:** Journal entry form with double-entry validation, approval workflow
- **Period Closing Page:** Closing checklist, validation checks, closing confirmation
- **Reports Page:** Trial Balance, P&L, Balance Sheet dengan date range filtering
- **Dashboard Page:** Financial overview, KPI cards, charts, key metrics

**HR Pages:**
- **Employee List Page:** Table view dengan filter, search, organizational structure
- **Employee Detail Page:** Profile summary, employment history, salary info
- **Attendance Page:** Clock in/out, attendance calendar, leave management
- **Payroll Page:** Payroll processing, payslip generation, payroll history
- **Recruitment Pages:** Candidate list, candidate detail, CV upload, job management, job matching
- **Dashboard Page:** HR overview, productivity analytics, recruitment pipeline

### 6.2 User Experience Guidelines
- Consistent UI patterns across all modules (shadcn/ui components)
- Minimal clicks untuk common actions
- Progressive disclosure untuk complex data (tabs, accordions)
- Visual indicators untuk status (color-coded badges, progress bars)
- Consistent terminology dan iconography
- Loading states untuk async operations
- Empty states dengan helpful CTAs
- Responsive design untuk tablet usage
- Keyboard shortcuts untuk power users
- Real-time updates untuk collaborative features

---

## 7. Data Requirements

### 7.1 Data Entities

**WMS Entities:**
- **stock_ledger:** Inventory movements dengan double-entry (in/out)
- **inventory_items:** Master data barang (SKU, description, unit of measure)
- **locations:** Warehouse locations (bins, shelves, zones)
- **purchase_receipts:** Inbound purchase order receipts
- **sales_orders:** Outbound customer orders
- **pick_lists:** Generated pick lists untuk orders
- **shipments:** Shipping confirmations

**Finance Entities:**
- **chart_of_accounts:** Account codes dengan hierarchy
- **journal_entries:** Journal entry headers
- **journal_entry_lines:** Debit/credit lines (double-entry)
- **periods:** Accounting periods dengan status (open/closed)
- **ledger:** General ledger balances
- **anomalies:** Flagged anomalous transactions

**HR Entities:**
- **employees:** Master data karyawan
- **organizational_units:** Department/division structure
- **positions:** Job positions dan salary grades
- **attendance:** Clock in/out records
- **leaves:** Leave requests dan approvals
- **payroll_periods:** Payroll processing periods
- **payroll_records:** Calculatedipayroll per employee
- **candidates:** Recruitment candidate profiles
- **candidate_education:** Education history
- **candidate_experience:** Work experience
- **candidate_skills:** Skills data
- **job_postings:** Job descriptions
- **candidate_job_matches:** Matching scores

**Shared Entities:**
- **users:** System users
- **roles:** Role definitions
- **permissions:** Granular permissions
- **audit_logs:** Immutable audit trail
- **tenants:** Tenant data untuk multi-tenancy

### 7.2 Data Flow

**WMS Flow:**
1. Purchase receipt received → Quality check → Putaway → Stock ledger update (debit)
2. Sales order created → Pick list generation → Pick → Pack → Ship → Stock ledger update (credit)


**Finance Flow:**
1. Journal entry created → Double-entry validation → Approval → Post to ledger
2. Period closing → Validation checks → Lock period → Generate reports
3. Transaction monitoring → Statistical analysis → Flag anomalies → Generate LLM alerts
4. HR payroll processed → Post journal entries to Finance

**HR Flow:**
1. Employee clock in/out → Attendance recorded → Overtime calculated
2. Payroll period end → Calculate take-home pay → Generate payslips → Post to Finance
3. CV uploaded → Text extraction → AI parsing → Store candidate data
4. Job posting created → Generate embeddings → Match candidates → Score and rank
5. Task App time logs → Correlate with attendance → Calculate productivity

**Cross-Module Flow (via Domain Events):**
- PayrollProcessed (HR) → Post journal entries (Finance)
- StockMovement (WMS) → Update inventory valuation (Finance)

### 7.3 Data Retention
- **WMS:** Stock ledger retained indefinitely (7 years for compliance), Purchase receipts 7 years, Sales orders 7 years
- **Finance:** Journal entries retained indefinitely (7 years for compliance), Audit logs indefinitely
- **HR:** Employee data retained indefinitely, Attendance records 2 years, Payroll records 7 years, Active candidates indefinitely, Rejected candidates auto-archive 1 year, delete 2 years
- **Shared:** Audit logs retained indefinitely, PII data encrypted, access restricted

---

## 8. Integration Requirements

### 8.1 External Systems
- **Groq API:** LLM inference untuk HR module (CV parsing, embeddings, text generation)
  - Endpoint: https://api.groq.com/openai/v1/chat/completions
  - Auth: API key
  - Rate limit: Free tier limits
  - Fallback: Queue dengan retry logic

### 8.2 Internal Module Integration (via Domain Events)

**Events Raised by WMS:**
- `StockMovement` → Update inventory valuation di Finance
- `StockOutAlert` → Notify purchasing untuk reorder
- `ReceiptProcessed` → Update accounts payable di Finance

**Events Raised by Finance:**
- `JournalEntryPosted` → Update financial metrics di dashboard
- `PeriodClosed` → Lock related modules untuk period
- `BudgetThresholdExceeded` → Notify Finance Manager

**Events Raised by HR:**
- `EmployeeHired` → Create employee record, assign onboarding tasks (via webhook ke Task App)
- `EmployeeTerminated` → Remove access, archive data
- `PayrollProcessed` → Post journal entries ke Finance
- `CandidateHired` → Convert candidate ke employee

**Events Listened by Each Module:**
- **WMS:** `PurchaseOrderCreated` (dari external), `SalesOrderCreated` (dari external)
- **Finance:** `PayrollProcessed` (dari HR), `StockMovement` (dari WMS)

### 8.3 API Requirements

**WMS Endpoints:**
- POST /api/v1/wms/purchase-receipts
- POST /api/v1/wms/pick-lists
- POST /api/v1/wms/shipments
- GET /api/v1/wms/stock-ledger
- GET /api/v1/wms/inventory

**Finance Endpoints:**
- POST /api/v1/finance/journal-entries
- GET /api/v1/finance/chart-of-accounts
- POST /api/v1/finance/chart-of-accounts
- PUT /api/v1/finance/chart-of-accounts/{id}
- DELETE /api/v1/finance/chart-of-accounts/{id}
- POST /api/v1/finance/periods/{id}/close
- GET /api/v1/finance/reports/trial-balance
- GET /api/v1/finance/reports/pl
- GET /api/v1/finance/reports/balance-sheet

**HR Endpoints:**
- GET /api/v1/hr/employees
- POST /api/v1/hr/attendance/clock-in
- POST /api/v1/hr/attendance/clock-out
- POST /api/v1/hr/payroll/process
- POST /api/v1/hr/recruitment/candidates/upload
- GET /api/v1/hr/recruitment/candidates
- POST /api/v1/hr/recruitment/jobs

**Shared Endpoints:**
- GET /api/v1/auth/me
- POST /api/v1/auth/logout
- GET /api/v1/audit-logs

---

## 9. Business Rules

| Rule ID | Rule Description | Condition | Action |
|---------|-----------------|-----------|--------|
| **WMS Rules** ||||
| BR-WMS-001 | Double-entry inventory | Every stock movement | Create paired debit/credit entries in stock ledger |
| BR-WMS-002 | FIFO valuation | Valuation method = FIFO | Use earliest stock for cost calculation |
| BR-WMS-003 | Stock-out alert | Stock level < reorder point | Generate alert and reorder recommendation |
| BR-WMS-004 | Quality check hold | Quality check failed | Block putaway until resolved |
| **Finance Rules** ||||
| BR-FIN-001 | Double-entry validation | Journal entry creation | Enforce debit = credit |
| BR-FIN-002 | Period lock | Period status = closed | Prevent any modifications to period |
| BR-FIN-003 | Budget variance threshold | Variance > 20% | Flag in variance report |
| BR-FIN-004 | Approval required | Journal entry amount > threshold | Require manager approval |
| **HR Rules** ||||
| BR-HR-001 | Late tolerance | Clock in > tolerance minutes | Mark as late, deduct from attendance |
| BR-HR-002 | Overtime calculation | Work hours > daily limit | Calculate overtime pay at 1.5x rate |
| BR-HR-003 | Payroll posting | Payroll processed | Auto-post journal entries to Finance |
| BR-HR-004 | Duplicate candidate | Email/phone match existing | Flag as potential duplicate |
| BR-HR-005 | Minimum match score | Candidate score < 30% | Hide from top results |
| **Shared Rules** ||||
| BR-SHARED-001 | Audit logging | Any data change | Log to audit trail |
| BR-SHARED-002 | RBAC enforcement | API endpoint access | Validate permissions before access |
| BR-SHARED-003 | RLS enforcement | Database query | Filter by tenant_id automatically | |

---

## 10. Assumptions & Constraints

### 10.1 Assumptions
- PostgreSQL pgvector extension supported di Neon
- Users akan adopt ERP system untuk daily operations
- Existing business processes can be mapped to ERP workflows
- Compliance requirements sesuai dengan industry standards

### 10.2 Constraints
- Budget: Rp 0 (gratis tier semua services)
- Hardware: Laptop user (12GB RAM) - tidak bisa run local LLM
- Rate limits: Groq free tier has request limits (HR only)
- Data privacy: Must comply dengan data protection regulations
- Timeline: Development dalam 12 weeks untuk AI engineer learning path
- Architecture: Must follow Modular Monolith pattern dengan Clean Architecture dan DDD
- Integration: Must communicate antar modul via Domain Events (MediatR), bukan direct calls
- Compliance: Must comply dengan industry-specific regulations (Mining, Oil & Gas, Logistics)

### 10.3 Dependencies
- Neon PostgreSQL dengan pgvector extension
- Upstash Redis untuk queue dan caching
- Groq API untuk LLM inference (HR module)
- Shared Kernel (domain events, audit trail, RBAC)
- Existing Auth system (NextAuth v5 + JWT)

---

## 11. Risks & Mitigation

| Risk ID | Risk Description | Probability | Impact | Mitigation Strategy |
|---------|-----------------|-------------|--------|---------------------|
| **Technical Risks** |||||
| R-001 | Groq API rate limits exceeded (HR) | Medium | High | Implement queue system, cache results, graceful degradation |
| R-002 | LLM hallucination in AI features | Medium | High | Confidence scoring, human review, validation rules |
| R-003 | pgvector performance degradation | Low | Medium | Index maintenance, query optimization |
| R-004 | Data privacy breach | Low | Critical | Encryption, RBAC, audit logs, data retention policies |
| **Business Risks** |||||
| R-005 | User adoption resistance | Medium | Medium | Training, onboarding guide, phased rollout |
| R-006 | Data migration complexity | Medium | High | Phased migration, data validation, rollback plan |
| R-007 | Integration failures with existing systems | Medium | High | Extensive testing, fallback mechanisms |
| **Compliance Risks** |||||
| R-008 | Non-compliance with industry regulations | Low | Critical | Legal review, compliance audit, regular updates | |

---

## 12. Success Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| **WMS Metrics** ||||
| Stock-out incidents reduction | > 40% | Compare before/after implementation |
| Inventory turnover improvement | > 30% | Inventory turnover ratio calculation |
| Inbound processing time | < 3 hours per receipt | Average time from receipt to putaway |
| **Finance Metrics** ||||
| Period closing time | < 2 days | Time from period end to close |
| Journal entry error rate | < 1% | Error rate in posted entries |
| Budget variance accuracy | 100% | Variance calculation vs manual audit |
| **HR Metrics** ||||
| Payroll processing time | < 1 day | Time from period end to payslip generation |
| Payroll accuracy | 100% | Error rate in payroll calculations |
| CV parsing accuracy | > 85% | Sample validation against manual entry |
| Time to screen 100 CVs | < 30 minutes | Track time from upload to shortlist |
| **Overall Metrics** ||||
| User adoption rate | > 80% | Active user metrics |
| System uptime | > 99.5% | Availability monitoring |
| P95 latency | < 300ms | Performance monitoring |
| AI learning progress | Complete 6 AI engineering skills | Track skill acquisition milestones | |

---

## 13. Approval

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Product Owner | | | |
| Technical Lead | | | |
| Business Analyst | | | |

---

## 14. Change History

| Version | Date | Author | Description of Changes |
|---------|------|--------|----------------------|
| 1.0 | 2026-06-29 | AI Engineer | Initial version - Complete FluxGrid ERP PRD covering all 4 modules (WMS, Finance, HR, TaskProject) |
| 2.0 | 2026-07-08 | AI Engineer | Remove TaskProject module — extracted to standalone Go + Next.js app. See TASK-APP.md |
