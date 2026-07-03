# task-project01-design.md
# User Story TP-1: Kanban Board Management
## Design Specification Document

---

**Module:** Task & Project Management (TaskProject)
**User Story ID:** TP-1
**Design Version:** 1.0
**Created:** 2026-07-02
**Designer Reference:** FluxGrid Design System v1.0

---

## 1. Screen Overview & Layout Description

### 1.1 Page: Kanban Board View

**Route:** `/projects/[projectId]/board`

Halaman kanban board adalah layar utama untuk manajemen task. Layout menggunakan full-width horizontal scroll untuk mengakomodasi banyak kolom, dengan toolbar fixed di atas.

**Overall Layout Structure:**
```
┌─────────────────────────────────────────────────────────────────┐
│  GLOBAL SIDEBAR (64px fixed left)                               │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  PAGE HEADER                                              │  │
│  │  [Project Name] > [Board Name]   [Filter] [Members] [⚙]  │  │
│  ├───────────────────────────────────────────────────────────┤  │
│  │  BOARD TOOLBAR                                            │  │
│  │  [+ Add Task] [Filter ▾] [Group By ▾] [View: Board|List] │  │
│  │  [Active Filters: Assignee: Sari Dewi ✕]                 │  │
│  ├───────────────────────────────────────────────────────────┤  │
│  │  KANBAN BOARD AREA (horizontal scroll, overflow-x: auto) │  │
│  │                                                           │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐ │  │
│  │  │ BACKLOG  │  │IN PROGRS │  │  REVIEW  │  │  DONE    │ │  │
│  │  │ (5 task) │  │  WIP:3/3 │  │ (2 task) │  │ (8 task) │ │  │
│  │  ├──────────┤  ├──────────┤  ├──────────┤  ├──────────┤ │  │
│  │  │ [Card 1] │  │ [Card 4] │  │ [Card 7] │  │ [Card 9] │ │  │
│  │  │ [Card 2] │  │ [Card 5] │  │ [Card 8] │  │ [Card10] │ │  │
│  │  │ [Card 3] │  │ [Card 6] │  │          │  │    ...   │ │  │
│  │  │          │  │          │  │          │  │          │ │  │
│  │  │ [+ Add]  │  │ [+ Add]  │  │ [+ Add]  │  │ [+ Add]  │ │  │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘ │  │
│  │                                            [+ Add Column]│  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Wireframe Descriptions (Detailed Text-based)

### 2.1 Kanban Column Header

```
┌────────────────────────────────────────┐
│  ═══ IN PROGRESS                  [≡]  │  ← drag handle (column reorder)
│  WIP: 3/3 tasks  ●●●                  │  ← WIP indicator (red when full)
│  [⋮] more options                      │  ← kebab menu: rename, set WIP, delete
└────────────────────────────────────────┘
```

**Elemen Column Header:**
- **Column drag handle** (≡): grip icon di kiri, visible saat hover → trigger column reorder
- **Column name**: font semibold 14px, editable dengan double-click
- **Task count badge**: pill kecil di kanan nama (e.g., "5")
- **WIP indicator**: "WIP: 3/3" dengan progress dots merah jika penuh
- **Kebab menu (⋮)**: Rename Column | Set WIP Limit | Delete Column | Set as Done Column

### 2.2 Task Card

```
┌───────────────────────────────────────────┐
│ ● CRITICAL                    [🔖 Feature] │  ← priority badge | label tag
│                                            │
│  Implementasi Login dengan SSO             │  ← title (font-medium 13px)
│                                            │
│  [📎 2]  [💬 5]  [📅 15 Jul]             │  ← metadata row
│                                            │
│  ────────────────────────   [👤 SD]       │  ← progress bar | assignee avatar
└───────────────────────────────────────────┘
```

**Elemen Task Card:**
- **Priority badge** (pojok kiri atas):
  - `● CRITICAL` → background: `red-100`, text: `red-700`, dot: `red-500`
  - `● HIGH` → `orange-100` / `orange-700`
  - `● MEDIUM` → `yellow-100` / `yellow-700`
  - `● LOW` → `slate-100` / `slate-500`
- **Label tag**: chip kecil warna-warni, maximal 2 label tampil (selebihnya "+N")
- **Title**: max 2 baris, truncated dengan ellipsis
- **Metadata row:**
  - Attachment count `📎 2`
  - Comment count `💬 5`
  - Due date `📅 15 Jul` — merah jika overdue, orange jika < 3 hari
- **Progress bar**: thin bar hijau di bawah card (jika task memiliki sub-tasks)
- **Assignee avatar**: circular 24px, initial fallback (misal "SD" untuk Sari Dewi)
- **Card hover state**: shadow naik, border ring biru, cursor `grab`
- **Card dragging state**: opacity 0.6, shadow besar, cursor `grabbing`

### 2.3 Task Detail Modal / Slide-over

```
┌─────────────────────────────────────────────────────────────┐
│  [←] Back to Board                           [Edit] [⋮] [✕] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [● CRITICAL]  [🔖 Feature] [🔖 Backend]                    │
│                                                             │
│  Implementasi Login dengan SSO                              │  ← editable h2
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  Description (rich text / Markdown preview)                 │
│  > Perlu implementasikan SSO dengan Google OAuth 2.0...    │
│                                                             │
│  ─────────────────────────────────────────────────────────  │
│  DETAILS                                                    │
│  ┌─────────────────┬──────────────────────────────────┐    │
│  │ Status          │ [In Progress ▾]                  │    │
│  │ Priority        │ [● Critical ▾]                   │    │
│  │ Assignee        │ [👤 Sari Dewi ▾]                 │    │
│  │ Due Date        │ [📅 15 Jul 2026 ▾]               │    │
│  │ Created by      │ Budi Santoso · 28 Jun 2026       │    │
│  │ Labels          │ [Feature ✕] [Backend ✕] [+ Add]  │    │
│  └─────────────────┴──────────────────────────────────┘    │
│                                                             │
│  ATTACHMENTS (2)                                            │
│  [📄 wireframe.pdf  2.1MB] [🖼️ mockup.png  450KB] [+ Upload] │
│                                                             │
│  ACTIVITY                                                   │
│  ● Sari Dewi memindahkan task ke "In Progress" — 2 jam lalu │
│  ● Budi Santoso membuat task — 28 Jun 2026                  │
│                                                             │
│  [Comment box: "Tambah komentar..."]          [Send 📤]     │
│                                                             │
│  [Save Changes]                         [Delete Task 🗑️]   │
└─────────────────────────────────────────────────────────────┘
```

### 2.4 Column Management Drawer

```
┌────────────────────────────────────────────────────────┐
│  Manage Board Columns                              [✕] │
│  ──────────────────────────────────────────────────── │
│  Drag to reorder columns                              │
│                                                       │
│  [≡] Backlog              [Rename] [Set WIP] [🗑️]    │
│  [≡] In Progress          [Rename] [Set WIP] [🗑️]    │
│  [≡] Review               [Rename] [Set WIP] [🗑️]    │
│  [≡] Done                 [Rename] [Set WIP] [🗑️]    │
│                                                       │
│  [+ Add Column]  (disabled if 15 columns)            │
│  ──────────────────────────────────────────────────── │
│  [Cancel]                         [Save Changes]     │
└────────────────────────────────────────────────────────┘
```

### 2.5 Board Toolbar

```
┌──────────────────────────────────────────────────────────────────┐
│ [+ Add Task]  │  [🔍 Search tasks...]  │  [Filter ▾]  [Group ▾]  │
│               │                        │                          │
│ Active: [Assignee: Sari Dewi ✕] [Priority: High ✕] [Clear All]  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 3. Component Hierarchy

```
<KanbanBoardPage>
  ├── <BoardHeader>
  │   ├── <Breadcrumb> (project > board)
  │   ├── <BoardTitle>
  │   ├── <MembersAvatarGroup>
  │   └── <BoardSettingsButton> → opens <ColumnManagementDrawer>
  │
  ├── <BoardToolbar>
  │   ├── <AddTaskButton>
  │   ├── <SearchInput>
  │   ├── <FilterDropdown>
  │   │   ├── <AssigneeFilter>
  │   │   ├── <PriorityFilter>
  │   │   ├── <LabelFilter>
  │   │   └── <DueDateFilter>
  │   ├── <GroupByDropdown>
  │   ├── <ViewToggle> (Board / List)
  │   └── <ActiveFilterBadges>
  │
  ├── <DndContext> (from @dnd-kit/core)
  │   └── <BoardColumns> (horizontal scroll container)
  │       ├── <KanbanColumn> (×n)
  │       │   ├── <ColumnHeader>
  │       │   │   ├── <ColumnDragHandle>
  │       │   │   ├── <ColumnTitle>
  │       │   │   ├── <TaskCountBadge>
  │       │   │   ├── <WipIndicator>
  │       │   │   └── <ColumnOptionsMenu>
  │       │   ├── <SortableContext>
  │       │   │   └── <TaskCard> (×n, virtualized)
  │       │   │       ├── <PriorityBadge>
  │       │   │       ├── <TaskTitle>
  │       │   │       ├── <LabelChips>
  │       │   │       ├── <TaskMetaRow>
  │       │   │       │   ├── <AttachmentCount>
  │       │   │       │   ├── <CommentCount>
  │       │   │       │   └── <DueDateChip>
  │       │   │       ├── <SubtaskProgressBar>
  │       │   │       └── <AssigneeAvatar>
  │       │   └── <AddTaskInlineInput>
  │       └── <AddColumnButton>
  │
  └── <TaskDetailSlideover> (portal, conditionally rendered)
      ├── <TaskDetailHeader>
      ├── <TaskTitleInput>
      ├── <TaskDescriptionEditor> (rich text)
      ├── <TaskDetailsForm>
      │   ├── <StatusSelect>
      │   ├── <PrioritySelect>
      │   ├── <AssigneeSelect>
      │   ├── <DueDatePicker>
      │   └── <LabelMultiSelect>
      ├── <AttachmentList>
      ├── <ActivityFeed>
      └── <CommentInput>
```

---

## 4. UI Components List (shadcn/ui)

| Komponen | shadcn/ui Component | Custom Wrapper | Keterangan |
|---|---|---|---|
| Task Card | — | `<TaskCard>` | Full custom, menggunakan Card primitive |
| Task Detail Modal | `<Sheet>` (slide-over) | `<TaskDetailSheet>` | Dari kanan, 560px wide |
| Column Menu | `<DropdownMenu>` | `<ColumnOptionsMenu>` | shadcn DropdownMenu |
| Filter Dropdown | `<Popover>` + `<Command>` | `<FilterPopover>` | Multi-select filter |
| Assignee Select | `<Select>` / `<Combobox>` | `<AssigneeCombobox>` | Search + select member |
| Priority Select | `<Select>` | `<PrioritySelect>` | Dengan icon warna |
| Date Picker | `<Popover>` + `<Calendar>` | `<DueDatePicker>` | shadcn Calendar |
| Search Input | `<Input>` | `<BoardSearchInput>` | Dengan debounce |
| Avatar | `<Avatar>` | `<UserAvatar>` | Fallback initials |
| Toast | `<Sonner>` (toast library) | — | Error/success feedback |
| Dialog Konfirmasi | `<AlertDialog>` | `<ConfirmDeleteDialog>` | Hapus kolom/task |
| Badge/Chip | `<Badge>` | `<PriorityBadge>`, `<LabelChip>` | Custom variants |
| Toolbar Button | `<Button>` variant="ghost" | — | Toolbar actions |
| Column Drawer | `<Sheet>` (left or right) | `<ColumnManagementDrawer>` | Manage columns |
| Progress Bar | — | `<SubtaskProgress>` | Custom thin bar |
| Tooltip | `<Tooltip>` | — | WIP limit info, disabled button hints |
| Breadcrumb | `<Breadcrumb>` | — | shadcn Breadcrumb |
| Separator | `<Separator>` | — | Section dividers |
| Skeleton | `<Skeleton>` | `<BoardSkeleton>` | Loading state |

---

## 5. Visual Guidelines

### 5.1 Color Palette

Mengacu pada FluxGrid Design System. Token utama yang digunakan di Kanban Board:

| Token | Value | Penggunaan |
|---|---|---|
| `--color-background` | `#F8FAFC` (slate-50) | Board background |
| `--color-surface` | `#FFFFFF` | Task card background |
| `--color-surface-hover` | `#F1F5F9` (slate-100) | Card hover bg |
| `--color-column-header` | `#F1F5F9` | Column header bg |
| `--color-border` | `#E2E8F0` (slate-200) | Card border, column border |
| `--color-primary` | `#2563EB` (blue-600) | Action buttons, links |
| `--color-text-primary` | `#0F172A` (slate-900) | Card title |
| `--color-text-secondary` | `#64748B` (slate-500) | Metadata text |

**Priority Colors:**

| Prioritas | Badge BG | Badge Text | Dot Color |
|---|---|---|---|
| Critical | `red-50` (#FEF2F2) | `red-700` | `red-500` |
| High | `orange-50` | `orange-700` | `orange-500` |
| Medium | `yellow-50` | `yellow-700` | `yellow-400` |
| Low | `slate-100` | `slate-500` | `slate-400` |

**Status Colors (kolom default):**

| Kolom | Accent Color (header border-top) |
|---|---|
| Backlog | `slate-400` |
| In Progress | `blue-500` |
| Review | `purple-500` |
| Done | `green-500` |

### 5.2 Typography

| Elemen | Font | Size | Weight | Line Height |
|---|---|---|---|---|
| Column Title | Inter | 14px | 600 (semibold) | 20px |
| Task Card Title | Inter | 13px | 500 (medium) | 18px |
| Metadata text | Inter | 11px | 400 (regular) | 16px |
| Priority badge | Inter | 11px | 600 | 16px |
| Modal Title | Inter | 20px | 600 | 28px |
| Section labels | Inter | 12px | 500 | 16px, uppercase, letter-spacing 0.05em |

### 5.3 Spacing & Sizing

| Elemen | Value |
|---|---|
| Column width | 280px (fixed) |
| Column gap (horizontal) | 16px |
| Column padding | 12px |
| Column header height | 52px |
| Task card padding | 12px |
| Task card border-radius | 8px |
| Task card min-height | 80px |
| Task card gap (vertical) | 8px |
| Board padding (top/bottom) | 24px |
| Modal width | 560px (right slide-over) |
| Avatar size (card) | 24px |
| Avatar size (header) | 28px |

---

## 6. Responsive Design Requirements

### 6.1 Breakpoints

| Breakpoint | Width | Behavior |
|---|---|---|
| Desktop (default) | ≥ 1024px | Full kanban board, horizontal scroll, sidebar visible |
| Tablet | 768px – 1023px | Sidebar collapses to icon-only; board horizontal scroll; column width 240px |
| Mobile | < 768px | Board beralih ke list/card view; kanban DnD diganti dengan swipe gestures atau status select |

### 6.2 Mobile Adaptation

Di layar mobile (< 768px), kanban board tidak ditampilkan dalam format horizontal karena UX buruk. Sebagai gantinya:
- Tampilan beralih ke **card list view** yang dapat difilter per kolom
- Status update dilakukan via **Status Select dropdown** di task card (bukan drag-and-drop)
- Tombol "Board View" di-disable dengan tooltip: "Board view optimal di layar lebar"
- Fitur drag-and-drop tetap tersedia di tablet (768px+) dengan touch events

### 6.3 Touch Support (Tablet)

- DnD library (`@dnd-kit`) mendukung pointer events (mouse + touch)
- Drag activation delay di touch: 200ms (untuk membedakan scroll vs drag)
- Visual long-press feedback: card sedikit membesar (scale 1.03) sebelum drag aktif

---

## 7. Animation & Micro-interactions

### 7.1 Card Drag Animation

| State | Animation | Duration | Easing |
|---|---|---|---|
| **Lift (pick up)** | `scale: 1.02`, `shadow-xl`, `opacity: 0.9` | 150ms | `ease-out` |
| **Floating (dragging)** | Follows cursor/pointer, `rotate: 1deg` | Real-time | — |
| **Placeholder (drop target)** | Dashed border slot, background `blue-50`, height matches card | Instant | — |
| **Drop (success)** | Card snaps to position, scale kembali ke 1, subtle bounce | 200ms | `spring(stiffness: 300, damping: 20)` |
| **Cancel (return)** | Card kembali ke posisi semula | 200ms | `ease-in-out` |

### 7.2 Column Transitions

| Event | Animation |
|---|---|
| Kolom baru ditambahkan | Slide in dari kanan + fade in, 250ms |
| Kolom dihapus | Fade out + collapse width, 200ms |
| Kolom di-reorder (DnD) | Smooth swap animation, 180ms |

### 7.3 Card Transitions

| Event | Animation |
|---|---|
| Card baru dibuat | Fade in + slide down dari atas, 200ms |
| Card dihapus | Fade out + height collapse, 180ms |
| Filter aktif | Card yang tidak cocok fade out (opacity 0.2), bukan hidden |

### 7.4 Modal / Slide-over

| Event | Animation |
|---|---|
| Slide-over open | Slide in dari kanan, overlay fade in, 300ms |
| Slide-over close | Slide out ke kanan, overlay fade out, 200ms |
| Tombol Save loading | Button text diganti spinner, disabled state |
| Save success | Toast muncul dari bottom-right, auto-dismiss 3s |

### 7.5 Feedback Toasts

| Skenario | Toast | Duration |
|---|---|---|
| Task berhasil dipindahkan | ✅ "Task dipindahkan ke [kolom]" | 2s |
| WIP limit terlampaui | ⚠️ "Batas WIP kolom tercapai" | 4s |
| Operasi gagal (network) | ❌ "Gagal menyimpan. Coba lagi." | 5s (dengan tombol Retry) |
| Kolom berhasil dibuat | ✅ "Kolom '[nama]' berhasil ditambahkan" | 2s |
| Task berhasil di-assign | ✅ "Task di-assign ke [nama]" | 2s |

---

## 8. Accessibility Requirements (WCAG 2.1 AA)

### 8.1 Keyboard Navigation

| Aksi | Keyboard Shortcut |
|---|---|
| Fokus ke board | `Tab` |
| Navigasi antar card | `Tab` / `Shift+Tab` |
| Buka detail task | `Enter` pada card yang difokus |
| Pick up card untuk keyboard DnD | `Space` saat card difokus |
| Pindah ke kolom berikutnya (keyboard DnD) | `Arrow Right` |
| Pindah ke kolom sebelumnya (keyboard DnD) | `Arrow Left` |
| Drop card | `Space` atau `Enter` |
| Cancel keyboard DnD | `Escape` |
| Tutup modal | `Escape` |
| Simpan perubahan di modal | `Ctrl+Enter` / `Cmd+Enter` |

### 8.2 ARIA Requirements

```html
<!-- Column -->
<div
  role="region"
  aria-label="Kolom: In Progress, 3 task"
>
  <!-- Column header -->
  <h2 aria-level="2">In Progress</h2>
  <span aria-live="polite" aria-atomic="true">3 dari 3 task (WIP penuh)</span>

  <!-- Task list -->
  <ul role="list" aria-label="Daftar task di kolom In Progress">
    <!-- Task card -->
    <li
      role="listitem"
      aria-label="Task: Implementasi Login, prioritas Critical, assignee Sari Dewi"
      tabIndex={0}
      aria-grabbed={isDragging ? "true" : "false"}
    >
      ...
    </li>
  </ul>
</div>

<!-- DnD live announcer -->
<div
  role="alert"
  aria-live="assertive"
  className="sr-only"
>
  {/* Announces: "Task 'Implementasi Login' diangkat. Gunakan arrow keys untuk memilih posisi." */}
  {/* Announces: "Task 'Implementasi Login' dijatuhkan ke kolom 'In Progress', posisi 2 dari 3." */}
</div>
```

### 8.3 Color Accessibility

- Semua teks di atas background memenuhi contrast ratio ≥ 4.5:1
- Priority tidak hanya dibedakan oleh warna — selalu disertai teks label
- Focus ring menggunakan `outline: 2px solid #2563EB` dengan `outline-offset: 2px`
- Board background dan card background memiliki contrast ≥ 3:1

### 8.4 Screen Reader Support

- Board diuji dengan NVDA (Windows) dan VoiceOver (macOS)
- DnD operations menggunakan aria-live regions untuk pengumuman
- Modal meng-trap focus saat terbuka; focus dikembalikan ke trigger saat modal ditutup
- Avatar images memiliki alt text berupa nama user, bukan kosong

---

## 9. State Management (UI States)

### 9.1 Board States

| State | Tampilan |
|---|---|
| **Initial Loading** | `<BoardSkeleton>`: 3 kolom placeholder dengan card skeletons beranimasi |
| **Empty Board** | Illustration + teks "Belum ada kolom. Mulai dengan menambahkan kolom status." + CTA button |
| **Empty Column** | Drop zone area dengan dashed border, teks "Tidak ada task di sini. Drag task ke sini atau + tambah task." |
| **Loaded (normal)** | Board tampil penuh dengan task cards |
| **Filtered (no results)** | Semua kolom kosong + banner: "Tidak ada task yang cocok dengan filter. Clear filter untuk melihat semua." |
| **Error Loading** | Error card dengan ikon ❌ + teks pesan error + tombol "Coba Muat Ulang" |
| **Offline** | Banner warning di atas board: "⚠️ Anda sedang offline. Perubahan tidak akan tersimpan." |

### 9.2 Task Card States

| State | Visual |
|---|---|
| **Default** | Shadow-sm, border `slate-200` |
| **Hover** | Shadow-md, border `slate-300`, cursor `grab` |
| **Focus (keyboard)** | Ring `blue-500` 2px |
| **Dragging** | Opacity 0.6, shadow-xl, cursor `grabbing` |
| **Overdue** | Due date badge merah, card border kiri merah 3px |
| **Completed (in Done column)** | Title dengan opacity 0.6 |

### 9.3 Column States

| State | Visual |
|---|---|
| **Normal** | Background `slate-100`, border `slate-200` |
| **Drag Over (active drop target)** | Background `blue-50`, border `blue-300` dashed |
| **WIP Full** | WIP badge merah, header bg `red-50` subtle |
| **WIP Warning (80% full)** | WIP badge orange |

---

## 10. Kanban-Specific Interactions

### 10.1 Drag-and-Drop Library

**Library:** `@dnd-kit/core` + `@dnd-kit/sortable`

Alasan pemilihan:
- Mendukung touch events (pointer events API)
- Accessibility-first (aria-grabbed, keyboard DnD built-in)
- Tidak memerlukan native HTML5 DnD (lebih konsisten cross-browser)
- Lightweight, tree-shakeable

**DnD Strategy:**
- Task reorder dalam kolom: `SortableContext` dengan `verticalListSortingStrategy`
- Task pindah antar kolom: `DragOverlay` + custom collision detection
- Column reorder: `SortableContext` dengan `horizontalListSortingStrategy`
- `DragOverlay` merender card clone saat drag (menghindari CSS glitch)

### 10.2 Collision Detection

```
Strategi: closestCorners
- Lebih akurat untuk kanban multi-column dibanding closestCenter
- Mendeteksi corner collision untuk menentukan kolom target
- Custom modifier: restrictToWindowEdges (mencegah drag keluar viewport)
```

### 10.3 Optimistic Updates

1. User melakukan drag-drop
2. UI langsung update (optimistic) — task berpindah kolom secara visual
3. API request dikirim di background
4. **Jika sukses:** state confirmed, tidak ada perubahan visible
5. **Jika gagal:** rollback — task kembali ke posisi semula, toast error muncul

### 10.4 Column Management Flow

1. User klik **⚙ Settings** di board header
2. `<ColumnManagementDrawer>` muncul dari kiri (Sheet)
3. User dapat:
   - Drag kolom di dalam drawer untuk reorder
   - Double-click nama kolom → inline edit (Input field muncul)
   - Klik 🗑️ → Alert Dialog konfirmasi
   - Klik "+ Add Column" → new row muncul dengan empty input
4. User klik "Save Changes" → semua perubahan dikirim dalam satu batch API call
5. Board di-refresh dengan kolom baru

---

*Dokumen ini adalah bagian dari FluxGrid ERP SDD Design Series untuk TP-1 Kanban Board Management. Design system reference: FluxGrid Design System v1.0.*
