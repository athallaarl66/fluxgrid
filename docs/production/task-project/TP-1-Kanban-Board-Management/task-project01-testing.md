# task-project01-testing.md
# User Story TP-1: Kanban Board Management
## Testing Scenarios & QA Specification

---

**Module:** Task & Project Management (TaskProject)
**User Story ID:** TP-1
**Test Document Version:** 1.0
**Created:** 2026-07-02
**QA Lead:** FluxGrid QA Team
**Testing Framework:** Playwright (E2E), Jest/Vitest (Unit), k6 (Load)

---

## 1. Test Strategy Overview

### 1.1 Testing Approach

FluxGrid Kanban Board menggunakan pendekatan **testing pyramid** yang terdiri dari:

| Layer | Tool | Coverage Target | Scope |
|---|---|---|---|
| Unit Tests | Vitest | ≥ 80% | Domain logic, use cases, validators |
| Integration Tests | Vitest + testcontainers | ≥ 70% | Repository layer, API handlers, DB queries |
| E2E Tests | Playwright | Key user journeys | Full browser simulation |
| Performance Tests | k6 | P95 benchmarks | Load, stress, soak testing |
| Security Tests | OWASP ZAP + manual | OWASP Top 10 | Penetration testing |
| Accessibility Tests | axe-core + Playwright | WCAG 2.1 AA | Screen reader, keyboard nav |

### 1.2 Test Environment

| Environment | URL | Database | Purpose |
|---|---|---|---|
| Local Dev | localhost:3000 | PostgreSQL Docker | Developer testing |
| Staging | staging.flexgrid.id | Neon staging DB | QA testing + UAT |
| Production | app.flexgrid.id | Neon production DB | Smoke tests only |

### 1.3 Test Execution Flow

```
1. Unit Tests → CI trigger on every PR
2. Integration Tests → CI trigger on every PR  
3. E2E Tests → CI trigger on merge to develop
4. Performance Tests → Weekly scheduled + pre-release
5. Security Tests → Monthly + pre-release
6. UAT → Sprint review dengan Product Owner
```

---

## 2. Test Cases

### TC-001: Membuat Kolom Status Baru (Happy Path)

**Category:** Functional — Column Management
**Priority:** High
**Acceptance Criteria Mapped:** AC-1.1

```gherkin
Feature: Custom Status Columns Management

Scenario: PM berhasil menambahkan kolom status baru
  Given PM sudah login dengan role "Project Manager"
  And PM berada di halaman board proyek "Proyek Alpha"
  And board memiliki 3 kolom: [Backlog, In Progress, Done]
  When PM klik tombol "Manage Columns"
  And panel konfigurasi kolom terbuka
  And PM klik tombol "+ Add Column"
  And PM ketik nama kolom "Quality Assurance"
  And PM klik tombol "Save Changes"
  Then kolom "Quality Assurance" muncul di board
  And total kolom di board menjadi 4
  And urutan kolom adalah [Backlog, In Progress, Done, Quality Assurance]
  And audit trail mencatat: "{PM_username} menambahkan kolom 'Quality Assurance'"
  And perubahan persisten setelah page refresh
```

**Test Data:**
- User: `pm_user@flexgrid.test` / role: `project_manager`
- Project: `project_alpha` (ID: `prj_001`)
- Existing columns: `backlog`, `in_progress`, `done`

**Expected Result:** ✅ Pass
**Post-condition:** Board memiliki 4 kolom; audit_logs tabel memiliki entry baru

---

### TC-002: Drag-and-Drop Task Antar Kolom (Happy Path)

**Category:** Functional — Drag and Drop
**Priority:** Critical
**Acceptance Criteria Mapped:** AC-2.1, AC-2.3, AC-2.7

```gherkin
Scenario: Team Member berhasil memindahkan task via drag-and-drop
  Given Team Member sudah login dengan role "team_member"
  And board proyek "Proyek Alpha" terbuka dengan kolom [Todo, In Progress, Done]
  And kolom "Todo" berisi task "Implementasi Login" yang di-assign ke Team Member
  When Team Member drag card "Implementasi Login" dari kolom "Todo"
  And placeholder card muncul di kolom "In Progress"
  And Team Member drop card ke kolom "In Progress"
  Then task "Implementasi Login" muncul di kolom "In Progress"
  And task tidak lagi ada di kolom "Todo"
  And status task di database berubah menjadi "in_progress"
  And audit trail mencatat: "{team_member_username} memindahkan task dari 'Todo' ke 'In Progress'"
  And timestamp updated_at task diperbarui
  And perubahan tampil real-time di tab browser lain yang membuka board yang sama
```

**Expected Result:** ✅ Pass

---

### TC-003: Assign Task ke Anggota Tim (Happy Path)

**Category:** Functional — Task Assignment
**Priority:** High
**Acceptance Criteria Mapped:** AC-3.1, AC-3.4, AC-3.5

```gherkin
Scenario: PM berhasil assign task ke anggota tim
  Given PM sudah login
  And task "Desain Mockup Homepage" ada di kolom "Todo" tanpa assignee
  When PM klik task card "Desain Mockup Homepage"
  And modal detail task terbuka
  And PM klik field "Assignee"
  And dropdown menampilkan daftar anggota proyek: ["Sari Dewi", "Budi Santoso", "Ahmad Fauzi"]
  And PM pilih "Sari Dewi"
  And PM klik tombol "Save"
  Then modal tertutup
  And task card menampilkan avatar "Sari Dewi" di pojok kanan bawah
  And notifikasi in-app muncul untuk "Sari Dewi" dalam waktu < 5 detik
  And audit trail mencatat assignment
  And database field assignee_user_id terisi dengan ID Sari Dewi
```

**Expected Result:** ✅ Pass

---

### TC-004: Set Task Priority (Happy Path)

**Category:** Functional — Priority Setting
**Priority:** High
**Acceptance Criteria Mapped:** AC-4.1, AC-4.3, AC-4.6

```gherkin
Scenario: PM mengubah prioritas task dari Medium ke Critical
  Given PM sudah login
  And task "Deploy ke Production" ada di board dengan prioritas "Medium" (default)
  When PM klik task card
  And modal detail terbuka
  And PM klik field "Priority"
  And dropdown menampilkan opsi: [Critical, High, Medium, Low]
  And PM pilih "Critical"
  And PM klik "Save"
  Then modal tertutup
  And task card menampilkan badge merah bertulisan "Critical"
  And audit trail mencatat: "Priority diubah dari 'Medium' ke 'Critical' oleh {PM_username}"
  And filter berdasarkan "Critical" menampilkan task ini
```

**Expected Result:** ✅ Pass

---

### TC-005: Hapus Kolom yang Masih Berisi Task (Negative Testing)

**Category:** Negative — Column Deletion Constraint
**Priority:** High
**Acceptance Criteria Mapped:** AC-1.4

```gherkin
Scenario: PM gagal menghapus kolom yang masih berisi task
  Given PM sudah login
  And kolom "In Progress" berisi 3 task aktif
  When PM klik "Manage Columns"
  And PM klik ikon hapus (trash) pada kolom "In Progress"
  Then konfirmasi dialog muncul dengan pesan:
    "Kolom 'In Progress' masih berisi 3 task. Pindahkan semua task sebelum menghapus kolom."
  And tombol "Delete" di-disable atau tidak ada di dialog
  And kolom "In Progress" tetap ada di board
  And tidak ada perubahan di database
```

**Expected Result:** ✅ Pass (error ditangani dengan baik)

---

### TC-006: Drag-and-Drop Dibatalkan (ESC Key)

**Category:** Edge Case — Cancelled Drag Operation
**Priority:** Medium
**Acceptance Criteria Mapped:** AC-2.4

```gherkin
Scenario: User membatalkan drag dengan menekan tombol ESC
  Given User sedang men-drag task card "Buat Report Bulanan" dari kolom "Todo"
  And placeholder muncul di kolom "In Progress"
  When User menekan tombol ESC pada keyboard
  Then task card kembali ke posisi semula di kolom "Todo"
  And tidak ada perubahan status task di database
  And tidak ada entry di audit trail untuk operasi ini
  And tidak ada error di console browser
```

**Expected Result:** ✅ Pass

---

### TC-007: WIP Limit Enforcement

**Category:** Business Rule — WIP Limit
**Priority:** Medium
**Acceptance Criteria Mapped:** AC-2.8

```gherkin
Scenario: User gagal memindahkan task ke kolom yang sudah mencapai WIP limit
  Given kolom "In Progress" dikonfigurasi dengan WIP limit = 3
  And kolom "In Progress" sudah berisi 3 task
  And kolom "Todo" berisi task "Task Baru"
  When User men-drag "Task Baru" dari "Todo" ke "In Progress"
  And User melepas drag di kolom "In Progress"
  Then task kembali ke kolom "Todo"
  And toast error muncul: "Kolom 'In Progress' telah mencapai batas WIP (3 task). Selesaikan task yang ada terlebih dahulu."
  And tidak ada perubahan di database
```

**Expected Result:** ✅ Pass

---

### TC-008: RBAC — Viewer Tidak Bisa Drag Task

**Category:** Security / RBAC
**Priority:** Critical
**Acceptance Criteria Mapped:** AC-2.9

```gherkin
Scenario: User dengan role Viewer mencoba drag task dan gagal
  Given User login dengan role "viewer" di proyek "Proyek Alpha"
  And board terbuka
  When User mencoba men-drag task card
  Then drag tidak dimulai (event tidak terpicu)
  And cursor tetap default (bukan grab cursor)
  And tooltip muncul: "Anda tidak memiliki izin untuk memindahkan task"
  And tidak ada request API yang dikirim
  And audit trail tidak mencatat apapun
```

**Expected Result:** ✅ Pass

---

### TC-009: Concurrent Drag oleh Dua User (Edge Case)

**Category:** Edge Case — Race Condition
**Priority:** High
**Acceptance Criteria Mapped:** E-1

```gherkin
Scenario: Dua user men-drag task yang sama secara bersamaan
  Given User A (PM) dan User B (Team Member) membuka board yang sama
  And task "Task Konflik" ada di kolom "Todo"
  When User A men-drag "Task Konflik" ke "In Progress" (pada waktu T)
  And User B men-drag "Task Konflik" ke "Done" (pada waktu T+500ms, sebelum User A selesai)
  Then salah satu operasi berhasil (last-write-wins atau first-write-wins sesuai implementasi)
  And user yang operasinya gagal mendapat toast: "Task telah dipindahkan oleh user lain. Board akan diperbarui."
  And board keduanya menampilkan posisi task yang sama (konsisten)
  And tidak ada data corruption di database
  And hanya satu entry di audit trail untuk task tersebut
```

**Expected Result:** ✅ Pass (conflict resolved gracefully)

---

### TC-010: Board Loading dengan Banyak Task (Performance)

**Category:** Performance
**Priority:** High
**Acceptance Criteria Mapped:** Success Metrics — Load Time

```gherkin
Scenario: Board tetap responsif dengan 200 task
  Given board proyek memiliki 200 task tersebar di 5 kolom
  When User membuka URL board proyek
  Then Time to First Byte (TTFB) < 200ms
  And Time to Interactive (TTI) < 1.5 detik
  And First Contentful Paint (FCP) < 800ms
  And tidak ada layout shift (CLS < 0.1)
  And scroll dalam kolom tetap smooth (60fps)
  And virtual scrolling aktif untuk kolom dengan > 50 task
```

**Expected Result:** ✅ Pass (benchmark terpenuhi)

---

### TC-011: Batas Maksimum Kolom (Edge Case)

**Category:** Edge Case — Column Limit
**Priority:** Medium
**Acceptance Criteria Mapped:** AC-1.7

```gherkin
Scenario: PM mencoba menambahkan kolom ke-16 (melebihi batas)
  Given board sudah memiliki 15 kolom
  When PM klik "Manage Columns"
  Then tombol "+ Add Column" ditampilkan dalam kondisi disabled (grayed out)
  And tooltip muncul: "Batas maksimum 15 kolom telah tercapai"
  And PM tidak dapat menambah kolom baru
  And tidak ada request API yang dikirim
```

**Expected Result:** ✅ Pass

---

### TC-012: Aksesibilitas Keyboard Navigation (Accessibility)

**Category:** Accessibility
**Priority:** High
**Standard:** WCAG 2.1 AA — Criterion 2.1.1

```gherkin
Scenario: User dapat navigasi dan memindahkan task menggunakan keyboard saja
  Given User membuka board dan tidak menggunakan mouse
  When User menekan Tab untuk fokus ke task card pertama
  And task card mendapat focus ring yang visible
  And User menekan Space untuk "pick up" task
  And visual indicator muncul bahwa task sedang dalam mode "keyboard move"
  And User menekan Arrow Key kanan untuk memindah ke kolom berikutnya
  And User menekan Enter untuk "drop" task
  Then task berpindah ke kolom target
  And focus kembali ke task card yang dipindahkan di lokasi baru
  And screen reader mengumumce: "Task [nama task] dipindahkan ke kolom [nama kolom]"
```

**Expected Result:** ✅ Pass

---

### TC-013: Pemindahan Task Saat Offline (Negative — Network)

**Category:** Negative — Network Failure
**Priority:** Medium
**Acceptance Criteria Mapped:** E-2

```gherkin
Scenario: User men-drag task saat koneksi internet terputus
  Given User sedang di board
  And koneksi internet terputus (diblokir via DevTools Network tab)
  When User men-drag task dari "Todo" ke "In Progress"
  And drop task
  Then task secara optimistis berpindah ke "In Progress" di UI
  And sistem mencoba POST ke API endpoint
  And API request gagal (network error)
  And task kembali ke posisi semula di "Todo"
  And toast error muncul: "Gagal menyimpan perubahan. Periksa koneksi internet Anda."
  And tidak ada perubahan di database (task masih di kolom asal)
```

**Expected Result:** ✅ Pass

---

### TC-014: Keamanan — SQL Injection pada Nama Kolom

**Category:** Security
**Priority:** Critical

```gherkin
Scenario: User mencoba SQL injection pada input nama kolom
  Given PM membuka panel "Manage Columns"
  When PM menginput nama kolom: "'; DROP TABLE kanban_columns; --"
  And PM klik "Save"
  Then server menolak input dan mengembalikan HTTP 400
  And error message di UI: "Nama kolom mengandung karakter yang tidak diizinkan"
  And database table kanban_columns tetap utuh
  And tidak ada query yang berhasil dieksekusi selain SELECT
  And security log mencatat percobaan input berbahaya dengan IP user
```

**Expected Result:** ✅ Pass

---

### TC-015: Filter Task Berdasarkan Assignee

**Category:** Functional — Filter
**Priority:** Medium
**Acceptance Criteria Mapped:** AC-3.6

```gherkin
Scenario: PM memfilter board untuk melihat task milik satu anggota
  Given board memiliki 20 task, masing-masing di-assign ke 5 anggota berbeda
  When PM klik ikon filter
  And PM pilih filter "Assignee: Sari Dewi"
  And PM klik "Apply"
  Then board hanya menampilkan task yang di-assign ke Sari Dewi
  And task milik anggota lain tersembunyi (tidak dihapus)
  And badge filter aktif muncul di toolbar: "Filtered: Sari Dewi"
  And PM klik "Clear Filter" → semua task tampil kembali
  And URL diperbarui dengan query params: ?assignee=user_id_sari
```

**Expected Result:** ✅ Pass

---

## 3. Performance Testing Requirements

### 3.1 Load Testing (k6)

**Tool:** k6
**Script Location:** `tests/performance/kanban-board.k6.js`

| Scenario | VUs | Duration | Target |
|---|---|---|---|
| Normal Load | 50 | 5 menit | P95 latency < 500ms |
| Peak Load | 200 | 10 menit | P95 latency < 1.5s; Error rate < 1% |
| Stress Test | 500 | 5 menit | System degrades gracefully, tidak crash |
| Soak Test | 100 | 1 jam | Tidak ada memory leak; latency stabil |

**Endpoints yang di-test:**
- `GET /api/projects/:id/boards` — Board load
- `PATCH /api/tasks/:id/status` — Task status update (DnD)
- `GET /api/projects/:id/members` — Member list (assignee dropdown)

### 3.2 Frontend Performance

| Metric | Target | Tool |
|---|---|---|
| FCP (First Contentful Paint) | < 800ms | Lighthouse |
| LCP (Largest Contentful Paint) | < 2.5s | Lighthouse |
| TTI (Time to Interactive) | < 1.5s | Lighthouse |
| CLS (Cumulative Layout Shift) | < 0.1 | Lighthouse |
| Board render (200 tasks) | < 1.5s | Custom perf mark |
| Drag operation response | < 16ms (1 frame) | Chrome DevTools |

---

## 4. Security Testing Requirements

### 4.1 Authentication & Authorization

| Test | Method | Expected |
|---|---|---|
| Akses board tanpa login | Direct URL | Redirect ke login page (302) |
| Akses board proyek lain | Direct URL | 403 Forbidden |
| Viewer drag task | Manual/Playwright | Drag tidak berfungsi, 403 dari API |
| CSRF pada task mutation | Tanpa CSRF token | 403 dari server |
| JWT expired token | API call | 401 Unauthorized |
| Privilege escalation | Modify request role | 403 dari server |

### 4.2 Input Validation

| Input | Test | Expected |
|---|---|---|
| Nama kolom XSS | `<script>alert(1)</script>` | Di-encode, tidak dieksekusi |
| Nama kolom SQL Injection | `'; DROP TABLE...` | HTTP 400, query tidak dieksekusi |
| Task title 10.000 karakter | POST request | HTTP 400, max length enforced |
| Negative WIP limit | `-1` | HTTP 400 validation error |
| Invalid UUID di URL | `/api/tasks/not-a-uuid` | HTTP 400 |

### 4.3 Rate Limiting

| Endpoint | Limit | Expected |
|---|---|---|
| `PATCH /api/tasks/:id/status` | 100 req/min per user | HTTP 429 setelah limit |
| `POST /api/boards/:id/columns` | 20 req/min per user | HTTP 429 setelah limit |

---

## 5. Accessibility Testing

### 5.1 WCAG 2.1 AA Checklist

| Criterion | ID | Test | Tool |
|---|---|---|---|
| Keyboard Navigation | 2.1.1 | Semua aksi bisa dilakukan via keyboard | Playwright + keyboard simulation |
| Focus Visible | 2.4.7 | Focus ring terlihat di semua elemen interaktif | axe-core |
| Color Contrast | 1.4.3 | Contrast ratio ≥ 4.5:1 (text), ≥ 3:1 (UI components) | Colour Contrast Analyser |
| Alternative Text | 1.1.1 | Avatar user memiliki alt text berisi nama user | axe-core |
| Error Identification | 3.3.1 | Pesan error deskriptif dan asosiasi dengan field | Manual |
| Resize Text | 1.4.4 | UI tidak rusak hingga 200% zoom | Browser zoom test |
| Screen Reader | — | Board dapat digunakan dengan NVDA/VoiceOver | Manual + screen reader |
| Drag-and-Drop Alternative | 2.1.1 | Tersedia mekanisme alternatif keyboard untuk DnD | Playwright keyboard |
| ARIA Labels | 4.1.2 | Semua komponen interaktif punya ARIA label yang tepat | axe-core |
| Language Attribute | 3.1.1 | HTML lang attribute = "id" (Bahasa Indonesia) | HTML validator |

---

## 6. Acceptance Criteria Verification Mapping

| Acceptance Criteria | Test Cases yang Memverifikasi | Status |
|---|---|---|
| AC-1.1 Tambah kolom baru | TC-001 | Planned |
| AC-1.4 Cegah hapus kolom berisi task | TC-005 | Planned |
| AC-1.7 Maksimum 15 kolom | TC-011 | Planned |
| AC-2.1 Drag-drop antar kolom | TC-002 | Planned |
| AC-2.4 Batalkan drag dengan ESC | TC-006 | Planned |
| AC-2.8 WIP limit enforcement | TC-007 | Planned |
| AC-2.9 RBAC drag restriction | TC-008 | Planned |
| AC-3.1 Assign task ke anggota | TC-003 | Planned |
| AC-3.6 Filter by assignee | TC-015 | Planned |
| AC-4.1 Set priority | TC-004 | Planned |
| E-1 Concurrent drag | TC-009 | Planned |
| E-2 Offline drag | TC-013 | Planned |
| Performance < 1.5s | TC-010 | Planned |
| Security SQL injection | TC-014 | Planned |
| Accessibility keyboard | TC-012 | Planned |

---

## 7. Test Data Requirements

### 7.1 Seed Data untuk Testing

```sql
-- Users
INSERT INTO users (id, name, email, role) VALUES
  ('usr_pm_001', 'Budi Santoso', 'budi@flexgrid.test', 'project_manager'),
  ('usr_tm_001', 'Sari Dewi', 'sari@flexgrid.test', 'team_member'),
  ('usr_tm_002', 'Ahmad Fauzi', 'ahmad@flexgrid.test', 'team_member'),
  ('usr_vw_001', 'Direktur Utama', 'dir@flexgrid.test', 'viewer');

-- Project
INSERT INTO projects (id, name, owner_id) VALUES
  ('prj_001', 'Proyek Alpha', 'usr_pm_001');

-- Project Members
INSERT INTO project_members (project_id, user_id, role) VALUES
  ('prj_001', 'usr_pm_001', 'manager'),
  ('prj_001', 'usr_tm_001', 'member'),
  ('prj_001', 'usr_tm_002', 'member'),
  ('prj_001', 'usr_vw_001', 'viewer');

-- Board
INSERT INTO kanban_boards (id, project_id, name) VALUES
  ('brd_001', 'prj_001', 'Main Board');

-- Columns
INSERT INTO kanban_columns (id, board_id, name, position, wip_limit) VALUES
  ('col_001', 'brd_001', 'Backlog', 0, NULL),
  ('col_002', 'brd_001', 'In Progress', 1, 3),
  ('col_003', 'brd_001', 'Review', 2, NULL),
  ('col_004', 'brd_001', 'Done', 3, NULL);

-- Tasks (200 tasks untuk performance testing)
-- Generated via factory in tests/factories/task.factory.ts
```

### 7.2 Test Environment Variables

```env
# .env.test
DATABASE_URL=postgresql://test:test@localhost:5433/flexmng_test
REDIS_URL=redis://localhost:6380
NEXT_PUBLIC_APP_URL=http://localhost:3000
TEST_PM_EMAIL=budi@flexgrid.test
TEST_PM_PASSWORD=TestPassword123!
TEST_TM_EMAIL=sari@flexgrid.test
TEST_TM_PASSWORD=TestPassword123!
TEST_VIEWER_EMAIL=dir@flexgrid.test
TEST_VIEWER_PASSWORD=TestPassword123!
```

### 7.3 Test Data Management

| Strategi | Detail |
|---|---|
| **Isolation** | Setiap test suite berjalan di database terisolasi (testcontainers) |
| **Cleanup** | `afterEach` menghapus data yang dibuat dalam test |
| **Fixtures** | Playwright fixtures mendefinisikan user sessions yang sudah authenticated |
| **Factory Pattern** | `TaskFactory`, `ProjectFactory`, `ColumnFactory` untuk data generation |
| **Snapshot** | DB snapshot untuk load test; restore sebelum setiap run |

---

## 8. Regression Test Plan

### 8.1 Test Suite Execution Order

```
1. auth.spec.ts (prerequisite: semua test butuh auth)
2. column-management.spec.ts
3. task-crud.spec.ts
4. drag-and-drop.spec.ts
5. task-assignment.spec.ts
6. task-priority.spec.ts
7. filters.spec.ts
8. rbac.spec.ts
9. audit-trail.spec.ts
10. real-time-sync.spec.ts (requires two browser contexts)
```

### 8.2 CI/CD Integration

```yaml
# .github/workflows/test.yml
- name: Run E2E Tests
  run: |
    npx playwright test tests/e2e/kanban/
  env:
    DATABASE_URL: ${{ secrets.TEST_DATABASE_URL }}
    REDIS_URL: ${{ secrets.TEST_REDIS_URL }}
```

---

*Dokumen ini adalah bagian dari FluxGrid ERP SDD Testing Series untuk TP-1 Kanban Board Management.*
