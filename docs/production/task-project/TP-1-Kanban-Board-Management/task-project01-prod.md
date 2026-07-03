# task-project01-prod.md
# User Story TP-1: Kanban Board Management
## Production Requirements Document

---

**Module:** Task & Project Management (TaskProject)
**User Story ID:** TP-1
**Priority:** Must Have
**Status:** Draft
**Created:** 2026-07-02
**Author:** FluxGrid ERP Team

---

## 1. Feature Overview

### 1.1 Summary

Kanban Board Management adalah fitur inti dalam modul Task & Project Management FluxGrid ERP yang memungkinkan Project Manager dan anggota tim untuk memvisualisasikan kemajuan proyek melalui papan kanban interaktif. Fitur ini memberikan tampilan visual terhadap alur kerja proyek dengan kolom status yang dapat dikustomisasi, pemindahan task via drag-and-drop, penugasan task ke anggota tim, serta penetapan prioritas task.

### 1.2 User Story

> **As a** Project Manager,
> **I want to** manage tasks using a Kanban board,
> **So that** I can visualize project progress clearly and efficiently.

### 1.3 Feature Scope

Fitur ini mencakup:
- Tampilan papan kanban per proyek dengan kolom status yang dapat dikonfigurasi
- Manajemen task card (buat, edit, hapus, pindahkan)
- Drag-and-drop task antar kolom status
- Assignment task ke anggota tim proyek
- Penetapan prioritas task (Critical, High, Medium, Low)
- Filter dan pencarian task di dalam board
- Real-time update saat task dipindahkan (via Redis pub/sub)
- Audit trail setiap perubahan task

---

## 2. Business Value & ROI

### 2.1 Business Value

| Dimensi | Nilai |
|---|---|
| **Produktivitas Tim** | Mengurangi waktu rapat status harian karena progress terlihat secara visual real-time |
| **Visibilitas Manajemen** | Project Manager dapat memantau bottleneck workflow tanpa perlu bertanya ke anggota tim |
| **Akuntabilitas** | Setiap task terassign ke individu yang jelas, audit trail mencatat siapa melakukan apa dan kapan |
| **Kolaborasi** | Tim dapat berkoordinasi lebih cepat karena status task selalu ter-update |
| **Compliance** | Audit trail mendukung kebutuhan SOX compliance dan ISO 9001 untuk industri manufaktur Indonesia |

### 2.2 ROI Estimate

- **Pengurangan waktu status update meeting:** ~30 menit/hari/tim → hemat ±10 jam/bulan/tim
- **Pengurangan task yang terlewat:** estimasi penurunan missed tasks sebesar 40% dibanding manajemen via spreadsheet
- **Onboarding lebih cepat:** anggota tim baru dapat melihat workflow langsung dari board tanpa briefing panjang

### 2.3 Key Performance Indicators (KPI)

| KPI | Target | Measurement |
|---|---|---|
| Adoption Rate | ≥ 80% tim menggunakan board dalam 30 hari | Analytics login + board access |
| Task Completion Rate | Naik ≥ 15% dalam 60 hari | Jumlah task berpindah ke "Done" per sprint |
| Average Task Cycle Time | Turun ≤ 20% | Selisih created_at → completed_at |
| Board Load Time | < 1.5 detik | Lighthouse / server timing |
| Drag-and-drop Error Rate | < 0.5% operasi | Error tracking |

---

## 3. User Persona Details

### 3.1 Primary Persona: Project Manager (Manajer Proyek)

| Atribut | Detail |
|---|---|
| **Nama Fiktif** | Budi Santoso |
| **Jabatan** | Manajer Proyek Senior |
| **Industri** | Manufaktur (pabrik komponen otomotif) |
| **Pengalaman ERP** | 3–5 tahun menggunakan SAP atau Odoo |
| **Goals** | Memastikan proyek selesai tepat waktu dan sesuai anggaran |
| **Pain Points** | Update status task via email/chat sangat fragmentasi; tidak ada satu tampilan terpadu |
| **Tech Savviness** | Menengah — familiar dengan browser app, tidak coding |
| **Device** | Desktop (Chrome/Edge), kadang tablet saat di lantai produksi |

### 3.2 Secondary Persona: Team Member (Anggota Tim)

| Atribut | Detail |
|---|---|
| **Nama Fiktif** | Sari Dewi |
| **Jabatan** | Engineer / Staf Operasional |
| **Goals** | Mengetahui task apa yang harus dikerjakan hari ini; update status cepat tanpa buka-tutup banyak menu |
| **Pain Points** | Sering tidak tahu task mana yang prioritas; kesulitan melaporkan progress ke atasan |
| **Tech Savviness** | Dasar — terbiasa dengan WhatsApp, email, Excel |
| **Device** | Desktop dan mobile (saat WFH) |

### 3.3 Tertiary Persona: Stakeholder / Direktur

| Atribut | Detail |
|---|---|
| **Goals** | Melihat progress proyek secara high-level tanpa mengganggu tim |
| **Usage Pattern** | Read-only, occasional — cukup lihat board 1–2x seminggu |

---

## 4. Full User Journey

### 4.1 Journey: Project Manager Menyiapkan Board Proyek Baru

```
Step 1: Login & Navigasi
  → PM login ke FluxGrid ERP
  → Navigasi ke menu "Task & Project" dari sidebar
  → Pilih proyek yang sudah ada atau buat proyek baru

Step 2: Konfigurasi Kolom Status
  → Klik tombol "Manage Columns" / gear icon di pojok kanan atas board
  → Panel konfigurasi kolom muncul (slide-in drawer)
  → PM melihat kolom default: [Backlog, In Progress, Review, Done]
  → PM mengubah nama kolom sesuai workflow proyek (misal: Todo, Dev, QA, UAT, Released)
  → PM drag-and-drop kolom untuk mengatur urutan
  → PM klik "Save" → kolom tersimpan, board ter-refresh

Step 3: Membuat Task Pertama
  → PM klik tombol "+ Add Task" di kolom "Todo"
  → Task card mini muncul langsung di dalam kolom (inline edit)
  → PM ketik judul task, tekan Enter
  → Task card tersimpan di kolom tersebut

Step 4: Detail Task
  → PM klik task card untuk membuka detail
  → Modal/Slide-over terbuka berisi form lengkap:
    - Judul (wajib)
    - Deskripsi (rich text)
    - Assignee (dropdown pilih anggota tim)
    - Priority (Critical/High/Medium/Low)
    - Due Date
    - Attachments
    - Labels/Tags
  → PM isi semua field → klik "Save"

Step 5: Assign ke Anggota Tim
  → PM drag task card dari kolom "Todo" ke kolom "In Progress"
  → Atau PM buka detail task → ubah assignee → save
  → Anggota tim mendapat notifikasi (in-app)

Step 6: Monitor Progress
  → PM kembali ke board view
  → Melihat task berpindah kolom seiring perkembangan pekerjaan
  → Filter berdasarkan assignee atau priority untuk fokus tampilan
```

### 4.2 Journey: Team Member Mengupdate Status Task

```
Step 1: Lihat Task Saya
  → Anggota tim login → navigasi ke board proyek
  → Filter "Assigned to Me" aktif secara default (setting personal)
  → Melihat daftar task yang ditugaskan kepadanya

Step 2: Update Status via Drag-and-Drop
  → Anggota tim drag task card dari "In Progress" ke "Review"
  → Konfirmasi muncul (opsional, berdasarkan konfigurasi transisi)
  → Kolom WIP limit ditampilkan (jika dikonfigurasi)
  → Task berpindah, timestamp diperbarui, audit trail dicatat

Step 3: Tambah Komentar
  → Anggota tim klik task → buka modal detail
  → Scroll ke bagian "Activity" / "Comments"
  → Ketik komentar → submit
  → PM mendapat notifikasi

Step 4: Tandai Selesai
  → Drag task ke kolom "Done" atau klik checkbox quick-complete
  → Task card muncul dengan visual "selesai" (warna berbeda / strikethrough)
```

### 4.3 Journey: Stakeholder Melihat Progress

```
Step 1: Login → Navigasi Board
  → Stakeholder mengakses board dalam mode read-only (RBAC: viewer role)
  → Tidak dapat drag task atau edit

Step 2: Filter & View
  → Filter berdasarkan sprint, assignee, atau label
  → Lihat summary statistik di atas board (total task, % complete, overdue count)

Step 3: Export
  → Klik "Export" → unduh CSV atau PDF snapshot board
```

---

## 5. Acceptance Criteria (Detailed & Testable)

### AC-1: Custom Status Columns Per Project

| ID | Kriteria | Kondisi Pass |
|---|---|---|
| AC-1.1 | PM dapat menambahkan kolom baru dengan nama custom | Kolom baru muncul di board setelah disimpan |
| AC-1.2 | PM dapat mengganti nama kolom yang sudah ada | Nama baru tampil di header kolom |
| AC-1.3 | PM dapat menghapus kolom yang tidak berisi task | Kolom hilang dari board |
| AC-1.4 | Sistem mencegah penghapusan kolom yang masih berisi task | Error message ditampilkan dengan jumlah task di kolom tersebut |
| AC-1.5 | PM dapat mengatur ulang urutan kolom via drag | Urutan kolom tersimpan dan konsisten setelah refresh |
| AC-1.6 | Minimum 1 kolom harus ada di setiap board | Tombol hapus di-disable jika hanya ada 1 kolom |
| AC-1.7 | Maksimum 15 kolom per board | Tombol "Add Column" di-disable setelah 15 kolom |
| AC-1.8 | Nama kolom maksimal 50 karakter | Validasi di-input field, counter karakter ditampilkan |
| AC-1.9 | Perubahan kolom tercatat di audit trail | Audit log menampilkan: user, waktu, aksi (create/rename/delete/reorder column) |

### AC-2: Drag-and-Drop Task Movement

| ID | Kriteria | Kondisi Pass |
|---|---|---|
| AC-2.1 | Task dapat di-drag dari satu kolom ke kolom lain | Task berpindah kolom, status ter-update di database |
| AC-2.2 | Task dapat di-reorder dalam kolom yang sama | Urutan baru tersimpan dan konsisten |
| AC-2.3 | Visual feedback saat drag (placeholder di target) | Ghost card / placeholder muncul saat dragging |
| AC-2.4 | Jika drop dibatalkan (ESC atau drop di area invalid), task kembali ke posisi semula | Task tidak berpindah |
| AC-2.5 | Drag-and-drop berfungsi di semua browser target (Chrome, Firefox, Edge, Safari) | Cross-browser test pass |
| AC-2.6 | Drag-and-drop berfungsi di touch device (tablet) | Touch drag berjalan normal |
| AC-2.7 | Pemindahan task via DnD mencatat audit trail | Log: siapa, dari kolom mana, ke kolom mana, timestamp |
| AC-2.8 | WIP limit enforcement: kolom dengan limit tidak menerima task jika sudah penuh | Error toast muncul; task kembali ke posisi asal |
| AC-2.9 | Drag task yang tidak dimiliki (non-assignee, non-PM) dibatasi via RBAC | Unauthorized user tidak bisa drag |

### AC-3: Task Assignment to Team Members

| ID | Kriteria | Kondisi Pass |
|---|---|---|
| AC-3.1 | PM dapat assign task ke anggota tim yang terdaftar di proyek | Dropdown menampilkan daftar member proyek |
| AC-3.2 | PM dapat unassign task (kosongkan assignee) | Field assignee menjadi kosong |
| AC-3.3 | Setiap task hanya bisa memiliki satu assignee utama | Hanya satu user dipilih |
| AC-3.4 | Anggota tim mendapat notifikasi in-app saat di-assign | Notifikasi muncul di bell icon dalam < 5 detik |
| AC-3.5 | Avatar assignee tampil di task card di board | Avatar/initial tampil di pojok kanan bawah card |
| AC-3.6 | Filter "Assigned to Me" menampilkan hanya task milik user yang login | Task list terfilter akurat |
| AC-3.7 | Assignment dicatat di audit trail | Log: siapa yang assign, ke siapa, kapan |

### AC-4: Task Priority Setting

| ID | Kriteria | Kondisi Pass |
|---|---|---|
| AC-4.1 | Task dapat diberi prioritas: Critical, High, Medium, Low | Semua opsi tersedia di dropdown/segmented control |
| AC-4.2 | Default prioritas adalah "Medium" | Saat task baru dibuat, nilai awal adalah Medium |
| AC-4.3 | Prioritas ditampilkan secara visual di task card (badge/icon/warna) | Indikator visual konsisten dengan design system |
| AC-4.4 | PM dapat filter board berdasarkan prioritas | Hanya task dengan prioritas dipilih yang tampil |
| AC-4.5 | Sort task dalam kolom berdasarkan prioritas | Critical paling atas, Low paling bawah |
| AC-4.6 | Perubahan prioritas dicatat di audit trail | Log: dari prioritas apa ke prioritas apa, oleh siapa |

---

## 6. Edge Cases & Constraints

### 6.1 Edge Cases

| # | Skenario | Expected Behavior |
|---|---|---|
| E-1 | Dua user men-drag task yang sama secara bersamaan (concurrent drag) | Last-write-wins dengan optimistic locking; user kedua mendapat notifikasi bahwa task telah dipindahkan |
| E-2 | Koneksi internet terputus saat drag-and-drop | Task kembali ke posisi semula; toast error "Gagal menyimpan perubahan, coba lagi" |
| E-3 | Board dengan 500+ task cards | Virtualized rendering; hanya card yang terlihat di viewport di-render |
| E-4 | User yang di-assign ke task keluar dari proyek | Task tetap ada, assignee field menampilkan "(User Dihapus)", PM mendapat notifikasi untuk re-assign |
| E-5 | Nama kolom mengandung karakter khusus (< > & " ') | Di-sanitize sebelum disimpan; ditampilkan dengan benar di UI |
| E-6 | Proyek dihapus sementara board masih terbuka | Board ter-redirect ke halaman 404 proyek dengan pesan yang jelas |
| E-7 | Task dibuat tanpa judul | Validasi client-side + server-side; task tidak tersimpan |
| E-8 | Board diakses dari dua tab browser sekaligus oleh user yang sama | Real-time sync via WebSocket/SSE; perubahan di satu tab refleks di tab lain |
| E-9 | Import task massal (> 1000 task sekaligus) | Background job processing; progress bar ditampilkan |
| E-10 | User mencoba mengakses board proyek yang tidak ia ikuti | 403 Forbidden redirect ke halaman daftar proyek |

### 6.2 Business Constraints

| Constraint | Detail |
|---|---|
| **Batas Task per Board** | Maksimum 2.000 task aktif per board (task archived tidak dihitung) |
| **Batas Kolom per Board** | Maksimum 15 kolom |
| **Batas Anggota per Proyek** | Maksimum 50 anggota (sesuai tier Enterprise FluxGrid) |
| **Retensi Audit Trail** | Minimal 2 tahun (sesuai regulasi ketenagakerjaan Indonesia) |
| **Attachment per Task** | Maksimum 10 file, total 50 MB per task |

### 6.3 Technical Constraints

- Board state harus konsisten antara UI dan database dalam < 2 detik
- Drag-and-drop library harus mendukung touch events (tablet/iPad)
- Semua mutasi data harus melalui domain events (tidak boleh direct DB write dari UI layer)
- Operasi board tidak boleh mempengaruhi performa modul ERP lain (isolasi resource)

---

## 7. Dependencies on Other Modules

| Modul | Dependency Type | Detail |
|---|---|---|
| **Auth & RBAC** | Hard dependency | User harus authenticated; RBAC roles menentukan aksi yang dibolehkan di board |
| **Project Management** | Hard dependency | Board adalah bagian dari Project entity; Project harus ada sebelum Board |
| **User Management** | Hard dependency | Daftar assignee diambil dari Project Members (User module) |
| **Notification System** | Soft dependency | In-app notification saat task di-assign atau dipindahkan; board tetap berfungsi jika notif down |
| **Audit Trail** | Hard dependency | Setiap aksi wajib dicatat; board tidak boleh beroperasi tanpa audit trail aktif |
| **File Storage (Attachments)** | Soft dependency | Upload lampiran task; board tetap berfungsi tanpa storage |
| **Redis (Upstash)** | Hard dependency | Real-time sync state board antar user; optimistic locking |
| **Reporting Module** | Soft dependency | Data task digunakan untuk laporan burndown chart dan velocity |

---

## 8. Out of Scope (TP-1)

Item berikut **tidak termasuk** dalam user story TP-1 dan akan dibahas di user story terpisah:

| Item | Alasan |
|---|---|
| Sprint / Iteration management | Dibahas di TP-2 (Sprint Board) |
| Time tracking per task | Dibahas di TP-5 (Time Tracking) |
| Gantt chart view | Dibahas di TP-6 (Gantt View) |
| Task dependencies (blocking/blocked by) | Dibahas di TP-3 |
| Recurring tasks | Dibahas di TP-4 |
| Burndown chart & velocity metrics | Dibahas di Reporting module |
| Email notification untuk task assignment | Dibahas di Notification module |
| Mobile native app (Android/iOS) | Tidak dalam scope FluxGrid v1.0 |
| Board templates | FluxGrid v1.1 roadmap |
| Automation rules (e.g., auto-assign ketika kolom berubah) | FluxGrid v1.2 roadmap |
| Multi-board view (portfolio level) | FluxGrid v2.0 roadmap |

---

## 9. Success Metrics & Acceptance Gates

### 9.1 Launch Criteria (Go/No-Go)

- [ ] Semua AC (AC-1 s/d AC-4) lulus UAT
- [ ] Load time board < 1.5 detik dengan 200 task
- [ ] Drag-and-drop berjalan tanpa error di Chrome, Firefox, Edge
- [ ] Audit trail mencatat semua aksi dengan benar
- [ ] RBAC: role Viewer tidak bisa drag/edit
- [ ] Zero data loss saat concurrent user (load test 50 user simultan)

### 9.2 Post-Launch Monitoring (30 hari)

- [ ] Error rate < 0.1% dari total operasi board
- [ ] P95 board load time < 2 detik
- [ ] Adoption rate ≥ 60% dari target user dalam minggu pertama

---

*Dokumen ini adalah bagian dari FluxGrid ERP SDD (Software Design Document) series. Versi ini dihasilkan untuk sprint planning awal. Review dan approval diperlukan dari: Product Owner, Tech Lead, QA Lead.*
