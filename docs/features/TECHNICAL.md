# Technical Design Document (TDD)

## Document Information
- **Document Version**: 3.1
- **Created Date**: 2026-06-29
- **Last Updated**: 2026-07-24
- **Author**: AI Engineer
- **Project**: FluxGrid ERP
- **Scope**: Complete ERP System (WMS, Finance, HR, Admin, Notifications)

---

## 1. Introduction

### 1.1 Purpose
Technical design document untuk FluxGrid ERP - sistem Modular Monolith untuk industri berat (Mining, Oil & Gas, Logistics, Manufacturing). Dokumen ini mencakup desain teknis untuk 3 modul: WMS (Warehouse Management), Finance (General Ledger), HR & Payroll. Semua modul mengikuti Clean Architecture dengan DDD dan berkomunikasi melalui Domain Events.

> **Catatan:** Modul **Task & Project Management** (kanban, time tracking, task dependencies) telah di-extract menjadi standalone app terpisah dengan Go backend. Lihat [`TASK-APP.md`](../TASK-APP.md) untuk dokumentasi lengkap.

### 1.2 Scope
**Included Modules:**

**1. WMS - Warehouse Management System**
- Stock Ledger dengan double-entry inventory
- Inbound/Outbound processing
- Valuation methods (FIFO, Average Cost)


**2. Finance - General Ledger & Reporting**
- Double-entry ledger (debit = kredit)
- Chart of Accounts management
- Period closing
- Financial reports (Trial Balance, P&L, Balance Sheet)
- Budget Management & Dashboard

**3. HR & Payroll**
- Employee data management
- Payroll engine dengan PPh 21
- HR Recruitment (CV parsing, job matching)
- AI Integration: CV Parsing, Candidate-Job Matching, Productivity Analytics

**Shared Features:**
- Modular Monolith architecture dengan Clean Architecture dan DDD
- Domain Events untuk komunikasi antar modul
- RBAC dengan granular permissions
- Audit Trail immutable
- Row-Level Security (RLS)
- AI Service Layer abstraction (Groq API) — HR only
- **Notification System** (polling-based, DB-persisted)
- **Admin Area** (User & Role Management, Super Admin only)

### 1.3 References
- PRD: docs/features/PRD.md
- README: README.md (FluxGrid ERP Technical Blueprint)
- Clean Architecture principles
- Domain-Driven Design (DDD)

---

## 2. System Architecture

### 2.1 High-Level Architecture
FluxGrid ERP adalah sistem Modular Monolith dengan 3 modul: WMS, Finance, HR. Setiap modul mengikuti Clean Architecture dengan layer: Domain, Application, Infrastructure, dan API. Komunikasi antar modul dilakukan melalui Domain Events via MediatR untuk menjaga loose coupling.

### 2.2 Architecture Diagram
```mermaid
graph TB
    subgraph Frontend
        UI[Next.js 15 + shadcn/ui]
    end
    
    subgraph Backend[.NET 8 Modular Monolith]
        API[API Layer]
        APP[Application Layer]
        WMS[WMS Module]
        FIN[Finance Module]
        HR[HR Module]
        SHARED[Shared Kernel]
    end
    
    subgraph External
        REDIS[Upstash Redis]
        PG[Koyeb Managed PostgreSQL]
    end
    
    UI -->|HTTP/REST| API
    API --> APP
    APP --> WMS
    APP --> FIN
    APP --> HR
    WMS -->|Domain Events| SHARED
    FIN -->|Domain Events| SHARED
    HR -->|Domain Events| SHARED
    WMS -->|StockMovement Event| FIN
    HR -->|PayrollProcessed Event| FIN
    WMS -->|Queue/Cache| REDIS
    FIN -->|Queue/Cache| REDIS
    HR -->|Queue/Cache| REDIS
    WMS -->|pgvector| PG
    FIN -->|pgvector| PG
    HR -->|pgvector| PG
    HR -->|LLM Inference| GROQ
    FIN -->|LLM Inference| GROQ

    
    style WMS fill:#e1f5ff
    style FIN fill:#e1ffe1
    style HR fill:#fff4e1
    style SHARED fill:#ffe1e1
    style GROQ fill:#ffe1e1
    style PG fill:#e1ffe1
```

---

## 3. Database Design

### 3.1 Entity Relationship Diagram
```mermaid
erDiagram
    %% WMS Entities
    inventory_items ||--o{ stock_ledger : "has movements"
    inventory_items ||--o{ purchase_receipts : "received in"
    inventory_items ||--o{ sales_orders : "ordered in"
    locations ||--o{ stock_ledger : "stored at"
    purchase_receipts ||--o{ pick_lists : "generates"
    sales_orders ||--o{ pick_lists : "requires"
    pick_lists ||--o{ shipments : "shipped in"
    
    %% Finance Entities
    chart_of_accounts ||--o{ journal_entry_lines : "used in"
    journal_entries ||--o{ journal_entry_lines : "has lines"
    periods ||--o{ journal_entries : "contains"
    journal_entries ||--o{ anomalies : "may be flagged as"
    
    %% HR Entities
    employees ||--o{ leaves : "requests"
    employees ||--o{ payroll_records : "paid in"
    organizational_units ||--o{ employees : "contains"
    positions ||--o{ employees : "assigned to"
    payroll_periods ||--o{ payroll_records : "contains"
    candidates ||--o{ candidate_education : "has"
    candidates ||--o{ candidate_experience : "has"
    candidates ||--o{ candidate_skills : "has"
    candidates ||--o{ candidate_job_matches : "matched with"
    job_postings ||--o{ candidate_job_matches : "matches with"
    candidates ||--o{ candidate_activity_logs : "has activities"
    candidate_job_matches }o--|| candidates : "links candidate"
    candidate_job_matches }o--|| job_postings : "links job"
    
    %% Shared Entities
    users ||--o{ audit_logs : "generates"
    roles ||--o{ permissions : "has"
    users ||--o{ roles : "assigned"
    tenants ||--o{ users : "contains"
```

### 3.2 HR-8 Entities
- **candidate_activity_logs:** id, candidate_id, action, performed_by, details, created_at
- **candidate_job_matches:** id, candidate_id, job_id, score, is_manual, created_at

### 3.3 Notification System
- **notifications:** id, user_id, type, title, body, is_read, created_at
- Index on (user_id, is_read) for fast unread queries
- Polling strategy: Frontend polls GET /api/notifications/unread every 30 seconds
- Badge count derived from unread count response

### 3.4 Admin Area
- No new tables — uses existing users, roles, permissions tables
- Admin endpoints (/api/admin/*) are role-gated to "Admin" (Super Admin)
- Super Admin bypass already implemented in Program.cs via RequireAssertion

---

## 4. API Design

### 4.1 API Endpoints

#### WMS Endpoints

**POST /api/v1/wms/purchase-receipts**
- **Description**: Create purchase receipt for inbound goods
- **Authentication**: Required (WMS:Write)

**POST /api/v1/wms/pick-lists**
- **Description**: Generate pick list for outbound order
- **Authentication**: Required (WMS:Write)

**POST /api/v1/wms/shipments**
- **Description**: Confirm shipment
- **Authentication**: Required (WMS:Write)

**GET /api/v1/wms/stock-ledger**
- **Description**: Get stock ledger with movements
- **Authentication**: Required (WMS:Read)

**GET /api/v1/wms/inventory**
- **Description**: Get current inventory levels
- **Authentication**: Required (WMS:Read)

---

#### Finance Endpoints

**POST /api/v1/finance/journal-entries**
- **Description**: Create journal entry
- **Authentication**: Required (Finance:Write)

**GET /api/v1/finance/chart-of-accounts**
- **Description**: Get chart of accounts hierarchy (nested tree or flat list)
- **Query Params**: `flat` (boolean, optional — returns flat list vs nested tree)
- **Authentication**: Required (finance.coa.read)

**POST /api/v1/finance/chart-of-accounts**
- **Description**: Create a new account
- **Request Body**: `code`, `name`, `parent_id` (optional), `type`, `is_active` (optional)
- **Validation**: Unique code per tenant, max 5 levels depth, circular reference check, type inheritance from parent
- **Authentication**: Required (finance.coa.manage)

**PUT /api/v1/finance/chart-of-accounts/{id}**
- **Description**: Update account details or deactivate
- **Request Body**: Partial — `code`, `name`, `parent_id`, `type`, `is_active`
- **Validation**: Circular reference on parent change, cascade deactivation to children
- **Authentication**: Required (finance.coa.manage)

**DELETE /api/v1/finance/chart-of-accounts/{id}**
- **Description**: Soft-deactivate account (cascades to children)
- **Validation**: Checks journal entry references before deactivation
- **Authentication**: Required (finance.coa.manage)

**POST /api/v1/finance/periods/{id}/close**
- **Description**: Close accounting period
- **Authentication**: Required (Finance:Admin)

**GET /api/v1/finance/reports/trial-balance**
- **Description**: Generate Trial Balance report
- **Authentication**: Required (Finance:Read)

**GET /api/v1/finance/reports/pl**
- **Description**: Generate Profit & Loss statement
- **Authentication**: Required (Finance:Read)

**GET /api/v1/finance/reports/balance-sheet**
- **Description**: Generate Balance Sheet
- **Authentication**: Required (Finance:Read)

---

#### HR Endpoints

**GET /api/v1/hr/employees**
- **Description**: List employees
- **Authentication**: Required (HR:Read)

**POST /api/v1/hr/payroll/process**
- **Description**: Process payroll for period
- **Authentication**: Required (HR:PayrollProcess)

**POST /api/v1/hr/recruitment/candidates/upload**
- **Description**: Upload CV file
- **Authentication**: Required (HR:CVWrite)

**GET /api/v1/hr/recruitment/candidates**
- **Description**: List candidates
- **Authentication**: Required (HR:CVRead)

**POST /api/v1/hr/recruitment/jobs**
- **Description**: Create job posting
- **Authentication**: Required (HR:CandidateManage)

**HR-8 Pipeline & Activity Endpoints:**
- PUT /api/v1/hr/recruitment/candidates/{id}/status
- GET /api/v1/hr/recruitment/candidates/{id}/activities
- POST /api/v1/hr/recruitment/candidates/{id}/activities
- POST /api/v1/hr/recruitment/candidates/{id}/jobs
- DELETE /api/v1/hr/recruitment/candidates/{id}/jobs/{jobId}
- GET /api/v1/hr/recruitment/candidates/{id}/jobs
- POST /api/v1/hr/recruitment/candidates/bulk-assign

---

#### Shared Endpoints

**GET /api/v1/auth/me**
- **Description**: Get current user info
- **Authentication**: Required

**POST /api/v1/auth/logout**
- **Description**: Logout user
- **Authentication**: Required

**GET /api/v1/audit-logs**
- **Description**: Get audit logs
- **Authentication**: Required (Audit:Read)

**GET /api/auth/profile**
- **Description**: Get current user profile
- **Authentication**: Required

**PUT /api/auth/profile**
- **Description**: Update current user profile (name, email)
- **Authentication**: Required

**GET /api/v1/wms/stock-ledger/transfers**
- **Description**: Get warehouse transfer entries (filtered from stock_ledger)
- **Query Params**: from_location_id, to_location_id, item_id, date_from, date_to, page, page_size
- **Authentication**: Required (WMS:Read)

---

#### Admin Endpoints (Super Admin only)

**GET /api/admin/users**
- **Description**: List all users
- **Authentication**: Required (Role = "Admin")

**POST /api/admin/users**
- **Description**: Create a new user
- **Authentication**: Required (Role = "Admin")

**PUT /api/admin/users/{id}**
- **Description**: Update user details
- **Authentication**: Required (Role = "Admin")

**DELETE /api/admin/users/{id}**
- **Description**: Deactivate user
- **Authentication**: Required (Role = "Admin")

**GET /api/admin/roles**
- **Description**: List all roles with permissions
- **Authentication**: Required (Role = "Admin")

**POST /api/admin/roles**
- **Description**: Create a new role
- **Authentication**: Required (Role = "Admin")

**PUT /api/admin/roles/{id}**
- **Description**: Update role and permissions
- **Authentication**: Required (Role = "Admin")

**DELETE /api/admin/roles/{id}**
- **Description**: Delete role
- **Authentication**: Required (Role = "Admin")

**GET /api/admin/permissions**
- **Description**: List all available permissions from enum
- **Authentication**: Required (Role = "Admin")

---

#### Notification Endpoints

**GET /api/notifications/unread**
- **Description**: Get unread notifications count + list (max 20)
- **Authentication**: Required
- **Polling**: Frontend polls every 30 seconds

**PUT /api/notifications/{id}/read**
- **Description**: Mark notification as read
- **Authentication**: Required

**PUT /api/notifications/read-all**
- **Description**: Mark all notifications as read
- **Authentication**: Required

---

#### Support Endpoint (Optional)

**POST /api/support/contact**
- **Description**: Submit support/contact message
- **Authentication**: Required

### 4.2 API Versioning Strategy
URL-based versioning: `/api/v1/`. Future versions will be `/api/v2/` with backward compatibility maintained for at least 6 months.

### 4.3 Domain Events Integration

Semua modul raises dan listens ke Domain Events untuk cross-module communication:

#### Events Raised by WMS
```csharp
// StockMovement - Raised when inventory moves in/out
public class StockMovement : IDomainEvent
{
    public Guid ItemId { get; }
    public decimal Quantity { get; }
    public string MovementType { get; } // "IN" or "OUT"
    public DateTime Timestamp { get; }
}

// StockOutAlert - Raised when stock level below reorder point
public class StockOutAlert : IDomainEvent
{
    public Guid ItemId { get; }
    public decimal CurrentStock { get; }
    public decimal ReorderPoint { get; }
}
```

#### Events Raised by Finance
```csharp
// AccountCreated - Raised when a chart of account is created
public sealed record AccountCreated(
    Guid AccountId, string Code, string Name, string Type,
    Guid? ParentId, Guid TenantId
) : IDomainEvent;

// AccountUpdated - Raised when an account is updated or deactivated
public sealed record AccountUpdated(
    Guid AccountId, string Code, string Name, string Type,
    bool IsActive, Guid TenantId
) : IDomainEvent;

// JournalEntryPosted - Raised when journal entry is posted
public class JournalEntryPosted : IDomainEvent
{
    public Guid JournalEntryId { get; }
    public decimal TotalAmount { get; }
    public DateTime PostedDate { get; }
}

// PeriodClosed - Raised when accounting period is closed
public class PeriodClosed : IDomainEvent
{
    public Guid PeriodId { get; }
    public DateTime ClosedDate { get; }
}

// BudgetThresholdExceeded - Raised when budget variance exceeds threshold
public class BudgetThresholdExceeded : IDomainEvent
{
    public Guid BudgetId { get; }
    public decimal VariancePercentage { get; }
}
```

#### Events Raised by HR
```csharp
// EmployeeHired - Raised when new employee is hired
public class EmployeeHired : IDomainEvent
{
    public Guid EmployeeId { get; }
    public Guid CandidateId { get; }
    public DateTime HiredDate { get; }
}

// PayrollProcessed - Raised when payroll is processed
public class PayrollProcessed : IDomainEvent
{
    public Guid PayrollPeriodId { get; }
    public decimal TotalAmount { get; }
    public DateTime ProcessedDate { get; }
}
```

#### Event Handler Example
```csharp
// Finance listens to PayrollProcessed from HR
public class PayrollProcessedHandler : INotificationHandler<PayrollProcessed>
{
    private readonly IJournalEntryService _journalService;
    
    public async Task Handle(PayrollProcessed notification, CancellationToken ct)
    {
        // Post payroll to journal entries
        await _journalService.PostPayrollEntryAsync(
            notification.PayrollPeriodId,
            notification.TotalAmount
        );
    }
}

```

### 4.4 Error Handling
Standard error codes:
- 400: Bad Request (invalid input)
- 401: Unauthorized (missing/invalid token)
- 403: Forbidden (insufficient permissions)
- 404: Not Found (resource doesn't exist)
- 409: Conflict (duplicate, etc.)
- 429: Too Many Requests (rate limit exceeded)
- 500: Internal Server Error
- 503: Service Unavailable (external service down)

Error response format:
```json
{
  "success": false,
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "Resource with specified ID not found",
    "details": {}
  }
}
```

---

## 5. Frontend Design

### 5.1 Component Architecture
```
fluxgrid-frontend/
├── app/
│   ├── (auth)/login/        # Login page
│   ├── dashboard/           # Dashboard page
│   ├── settings/            # Settings page (Profile, Security, Theme)
│   ├── support/             # Support page (FAQ, contact)
│   ├── help/                # Help & Documentation
│   ├── projects/            # Projects placeholder
│   ├── admin/
│   │   ├── users/           # User management (Super Admin)
│   │   └── roles/           # Role management (Super Admin)
│   ├── wms/
│   │   ├── stock-ledger/
│   │   ├── inbound/
│   │   ├── outbound/
│   │   └── transfers/       # Transfer log
│   ├── finance/
│   │   ├── chart-of-accounts/
│   │   ├── journal-entries/
│   │   ├── reports/
│   │   └── dashboard/
│   ├── hr/
│   │   ├── employees/
│   │   ├── payroll/
│   │   ├── recruitment/
│   │   └── dashboard/
│   └── api/auth/            # Auth API routes
├── components/
│   ├── ui/                  # shadcn/ui primitives
│   ├── Sidebar.tsx          # Fixed sidebar with nav
│   ├── Header.tsx           # Top bar with nav, search, bell, user menu
│   ├── Footer.tsx           # Copyright footer
│   ├── settings/            # ProfileTab, SecurityTab, ThemeTab
│   ├── admin/               # UserTable, UserFormModal, RoleFormModal, PermissionPicker
│   ├── notifications/       # NotificationDropdown, NotificationItem
│   ├── wms/                 # TransferTable, TransferFilters
│   └── support/             # FaqAccordion, ContactForm
├── hooks/
│   ├── useDashboard.ts      # Dashboard data
│   ├── useProfile.ts        # Profile API (TanStack Query)
│   ├── useAdmin.ts          # Admin API (users, roles, permissions)
│   ├── useNotifications.ts  # Notifications (polling 30s)
│   └── useTransfers.ts      # Transfer log data
└── lib/
    ├── api-client.ts        # Fetch wrapper + JWT cookie
    ├── auth-context.tsx     # Auth provider
    └── providers.tsx        # QueryClient provider
```

### 5.2 State Management
- **State Management Library**: TanStack Query (React Query)
- **Global State**: Server state managed by React Query
- **Local State**: React useState/useReducer for component state
- **Module-specific hooks**: useWMS, useFinance, useHR

### 5.3 Routing
| Route | Component | Access Control |
|-------|-----------|----------------|
| /dashboard | Main Dashboard | Any authenticated user |
| /settings | Settings (Profile, Security, Theme) | Any authenticated user |
| /support | Support (FAQ, contact) | Any authenticated user |
| /help | Help & Documentation | Any authenticated user |
| /projects | Projects placeholder | Any authenticated user |
| /admin/users | User Management | Super Admin only |
| /admin/roles | Role Management | Super Admin only |
| /wms/stock-ledger | Stock Ledger | WMS:Read |
| /wms/inbound | Inbound Processing | WMS:Write |
| /wms/outbound | Outbound Processing | WMS:Write |
| /wms/transfers | Transfer Log | WMS:Read |
| /wms/dashboard | WMS Dashboard | WMS:Read |
| /finance/chart-of-accounts | Chart of Accounts (tree CRUD) | finance.coa.read |
| /finance/journal-entries | Journal Entries | Finance:Read/Write |
| /finance/reports | Financial Reports | Finance:Read |
| /finance/dashboard | Finance Dashboard | Finance:Read |
| /hr/employees | Employee List | HR:Read |
| /hr/payroll | Payroll | HR:PayrollProcess |
| /hr/recruitment/candidates | Candidates | HR:CVRead |
| /hr/recruitment/jobs | Jobs | HR:CandidateManage |
| /hr/dashboard | HR Dashboard | HR:Read |
| /auth/login | Login | Public |

### 5.4 UI/UX Considerations
- Responsive design untuk tablet dan desktop
- Accessibility: WCAG 2.1 AA compliance
- Loading states untuk async operations
- Error boundaries untuk graceful error handling
- Progressive disclosure untuk complex data
- Keyboard shortcuts untuk power users
- Consistent UI patterns across all modules (shadcn/ui)
- Real-time updates untuk collaborative features
- Mobile-friendly untuk basic operations

---

## 6. Security Design

### 6.1 Authentication & Authorization
- **Authentication Method**: JWT Bearer Token via NextAuth v5 (Frontend) + .NET JWT Middleware (Backend)
- **Authorization Model**: RBAC (Role-Based Access Control)
- **Super Admin**: Role `Admin` bypasses all permission checks. Implemented via `RequireAssertion` in `Program.cs`:
  ```csharp
  policy.RequireAssertion(context =>
      context.User.HasClaim("permissions", permission) ||
      context.User.IsInRole("Admin"));
  ```
- **Granular Permissions**:
  - **WMS:** WMS:Read, WMS:Write, WMS:Admin
  - **Finance:** Finance:Read, Finance:Write, Finance:Admin, Finance:Audit, finance.coa.read, finance.coa.manage
   - **HR:** HR:Read, HR:Write, HR:PayrollProcess, HR:CVRead, HR:CVWrite, HR:CandidateManage, HR:RecruitmentManage
   - **Admin:** Admin:Manage (Super Admin only — user/role CRUD)
   - **Notification:** Notification:Read, Notification:Manage
   - **Shared:** Audit:Read, Audit:Write

### 6.2 Data Encryption
- **At Rest**: PostgreSQL encryption (Neon managed)
- **In Transit**: TLS 1.3 for all HTTP connections
- **PII Data**: Encrypted at rest, access restricted, logged in audit trail

### 6.3 Input Validation
- File type validation (WMS: documents, HR: CV files)
- File size validation (max 10MB)
- Email format validation
- SQL injection prevention via parameterized queries
- XSS prevention via output encoding
- CSRF protection for state-changing operations
- UUID validation for all ID parameters
- Decimal precision validation for financial amounts

### 6.4 Security Headers
| Header | Value | Purpose |
|--------|-------|---------|
| Strict-Transport-Security | max-age=31536000 | Enforce HTTPS |
| X-Content-Type-Options | nosniff | Prevent MIME sniffing |
| X-Frame-Options | DENY | Prevent clickjacking |
| Content-Security-Policy | default-src 'self' | Prevent XSS |
| X-XSS-Protection | 1; mode=block | XSS protection |
| Referrer-Policy | strict-origin-when-cross-origin | Control referrer info |
| Permissions-Policy | geolocation=(), microphone=(), camera=() | Restrict features |

---

## 7. Performance Considerations

### 7.1 Caching Strategy
- **Caching Layer**: Upstash Redis
- **Cache Keys**:
  - `wms:inventory:{id}` - Inventory levels
  - `wms:stock-ledger:{item_id}` - Stock ledger data
  - `finance:chart-of-accounts` - Chart of accounts hierarchy
  - `finance:ledger:{period_id}` - Ledger balances
  - `hr:employee:{id}` - Employee profile data
  - `hr:payroll:{period_id}` - Payroll calculations
  - `hr:candidate:{id}` - Candidate profile data
   - `hr:job:{id}:matches` - Job match results
- **Cache Invalidation**: TTL-based (24 hours), manual invalidation on updates

### 7.2 Database Optimization
- **Query Optimization**: Use indexes on frequently queried columns
- **Connection Pooling**: Npgsql connection pooling (max 100 connections)
- **Read Replicas**: Not needed for initial scale
- **Vector Search**: pgvector ivfflat indexes for similarity search (HR recruitment)
- **Partitioning**: Consider table partitioning for large tables (journal_entries, stock_ledger)
- **Materialized Views**: For complex reporting queries (financial reports)

### 7.3 CDN & Asset Optimization
- Koyeb Edge CDN for static assets
- Image optimization for document previews
- Lazy loading for large data tables
- Code splitting by module (WMS, Finance, HR)

---

## 8. Scalability Design

### 8.1 Horizontal Scaling
- Serverless architecture auto-scales via Vercel (frontend) and Cloudflare Workers (backend)
- Stateless API design enables horizontal scaling
- Database connection pooling handles high concurrency

### 8.2 Vertical Scaling
- Not applicable due to serverless architecture
- Neon PostgreSQL scales vertically automatically

### 8.3 Load Balancing
- Vercel and Cloudflare Workers handle load balancing automatically
- No manual load balancer configuration needed

---

## 9. Monitoring & Logging

### 9.1 Logging Strategy
- **Logging Framework**: Serilog (.NET)
- **Log Levels**: Debug, Information, Warning, Error, Critical
- **Log Aggregation**: Vercel logs for frontend, Cloudflare logs for backend

### 9.2 Monitoring
- **Metrics**: API response time, error rate, module-specific metrics (WMS: stock levels, Finance: journal entry rate, HR: payroll processing time)
- **Alerting**: Vercel alerts for frontend errors, Cloudflare alerts for backend errors
- **Health Checks**: `/api/health` endpoint for system health

### 9.3 Error Tracking
- **Error Tracking Tool**: Sentry
- **Error Reporting**: Automatic error reporting with stack traces and user context

---

## 10. Deployment Strategy

### 10.1 Environment Configuration
| Environment | Purpose | Configuration |
|-------------|---------|---------------|
| Development | Local development | Local .NET, Neon dev database |
| Staging | Pre-production testing | Vercel preview, Neon staging |
| Production | Live production | Vercel production, Neon production |

### 10.2 CI/CD Pipeline
- GitHub Actions for CI/CD
- Stages: Lint → Test → Build → Deploy
- Automatic deployment on merge to main
- Preview deployments on PR

### 10.3 Deployment Process
1. Code pushed to GitHub
2. GitHub Actions runs tests
3. Build frontend (Next.js) and backend (.NET)
4. Deploy frontend to Vercel
5. Deploy backend to Cloudflare Workers
6. Run database migrations if needed

### 10.4 Rollback Strategy
- Vercel: Instant rollback to previous deployment
- Cloudflare Workers: Rollback via versioned deployments
- Database: Migration rollback scripts

---

## 11. Testing Strategy

### 11.1 Unit Testing
- **Framework**: xUnit (.NET), Jest (React)
- **Coverage Target**: 80%
- **Key Test Areas**: Business logic (all modules), validation, parsing logic, domain events handlers

### 11.2 Integration Testing
- **Framework**: xUnit with TestServer (.NET)
- **Test Scenarios**: API endpoints (all modules), database operations, domain events propagation

### 11.3 End-to-End Testing
- **Framework**: Playwright
- **Test Scenarios**: 
  - WMS: Purchase receipt flow, pick/pack/ship flow
  - Finance: Journal entry creation, period closing, report generation
   - HR: Employee onboarding, payroll processing, CV upload flow

### 11.4 Performance Testing
- **Tools**: k6
- **Test Scenarios**: 
  - Load test stock ledger updates (100 concurrent)
  - Load test journal entry posting (50 concurrent)
  - Load test payroll processing (10 concurrent)
  - Load test CV upload (50 concurrent)
  - Load test kanban board updates (50 concurrent)

---

## 12. Development Guidelines

### 12.1 Code Style
- **Linting**: ESLint + Prettier (React), StyleCop (.NET)
- **Code Review Process**: Required for all PRs, minimum 1 approval
- **Commit Conventions**: Conventional Commits (feat:, fix:, docs:, etc.)

### 12.2 Documentation Standards
- XML documentation for public APIs (.NET)
- JSDoc for React components
- README for each module

### 12.3 Branching Strategy
- Trunk-based development
- Feature branches for new features
- PR required for merge to main

---

## 13. Assumptions & Dependencies

### 13.1 Assumptions
- Neon PostgreSQL supports pgvector extension
- Users will adopt ERP system for daily operations
- Existing business processes can be mapped to ERP workflows

### 13.2 External Dependencies
| Dependency | Version | Purpose |
|------------|---------|---------|
| Groq API | Latest | LLM inference (HR: CV parsing) |
| Neon PostgreSQL | Latest | Database with pgvector |
| Upstash Redis | Latest | Caching and queue |

### 13.3 Internal Dependencies
- Shared Kernel (domain events, audit trail, RBAC)
- Existing Auth system (NextAuth v5 + JWT)

---

## 14. Risks & Mitigation

| Risk ID | Risk Description | Probability | Impact | Mitigation Strategy |
|---------|-----------------|-------------|--------|---------------------|
| **Technical Risks** |||||
| TR-001 | Groq API rate limits exceeded (HR) | Medium | High | Queue system, caching, graceful degradation |
| TR-002 | LLM hallucination in AI features | Medium | High | Confidence scoring, human review, validation |
| TR-003 | pgvector performance degradation | Low | Medium | Index maintenance, query optimization |
| TR-004 | Data privacy breach | Low | Critical | Encryption, RBAC, audit logs |
| **Business Risks** |||||
| TR-005 | User adoption resistance | Medium | Medium | Training, onboarding guide, phased rollout |
| TR-006 | Data migration complexity | Medium | High | Phased migration, data validation, rollback plan |
| **Compliance Risks** |||||
| TR-007 | Non-compliance with industry regulations | Low | Critical | Legal review, compliance audit, regular updates |

---

## 15. Future Considerations

### 15.1 Planned Enhancements
- **WMS:** Barcode scanning integration, mobile app for warehouse staff
- **Finance:** Multi-currency support, advanced analytics
- **HR:** Performance reviews, training management, benefits administration
- **Overall:** Mobile app for basic operations, advanced analytics dashboard, multi-language support

### 15.2 Technical Debt
- Consider migrating from Groq to self-hosted LLM if cost becomes prohibitive (HR only)
- Evaluate vector database alternatives if pgvector performance degrades
- Consider microservice extraction for high-load modules
- Implement more sophisticated AI algorithms based on feedback

---

## 16. Approval

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Technical Lead | | | |
| Software Architect | | | |
| DevOps Engineer | | | |

---

## 17. Change History

| Version | Date | Author | Description of Changes |
|---------|------|--------|----------------------|
| 1.0 | 2026-06-29 | AI Engineer | Initial version - Complete FluxGrid ERP TDD covering all 4 modules (WMS, Finance, HR, TaskProject) |
| 2.0 | 2026-07-08 | AI Engineer | Remove TaskProject module — extracted to standalone Go + Next.js app. See TASK-APP.md |
| 3.0 | 2026-07-24 | AI Engineer | Add Notification System, Admin Area, Transfer Log, Settings, Support. Update architecture, frontend structure, routing, permissions. |
