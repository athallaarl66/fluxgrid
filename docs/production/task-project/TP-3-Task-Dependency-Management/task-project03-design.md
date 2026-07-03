# task-project03-design.md — Design Specification: TP-3 Task Dependency Management

> **Module:** Task & Project Management (TaskProject)
> **Story ID:** TP-3
> **Design System:** FluxGrid Design System v1.0
> **UI Library:** shadcn/ui + Tailwind CSS
> **Graph Library:** React Flow v11
> **Last Updated:** 2026-07-02

---

## 1. Screen Overview

This feature introduces two primary UI surfaces and one secondary surface:

| Surface | Location | Description |
|---|---|---|
| **Task Dependency Panel** | Task Detail Modal / Drawer — "Dependencies" Tab | Inline management of predecessors and successors for a specific task |
| **Dependency Graph View** | Project Detail Page — "Dependency Graph" Tab | Full-canvas DAG visualization of all task dependencies within a project |
| **Blocked Task Indicator** | Task Card (Kanban Board & Table View) | Visual indicator showing blocked status and the reason (blocked by X) |

---

## 2. Wireframe Descriptions

### 2.1 Task Dependency Panel (Task Detail — Dependencies Tab)

```
┌─────────────────────────────────────────────────────────────────────┐
│  Task Detail: Install Server Rack                          [×] Close │
├────────────┬──────────────┬──────────────┬──────────────────────────┤
│  Overview  │  Subtasks    │  Activity    │  Dependencies ← (active) │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  PREDECESSORS  (This task is blocked by)                            │
│  ┌──────────────────────────────────────────────────────┐           │
│  │  🔒  Procure Hardware                  IN_PROGRESS  ✕ │           │
│  └──────────────────────────────────────────────────────┘           │
│  [ + Add Predecessor ]                                              │
│                                                                     │
│  SUCCESSORS  (This task is blocking)                                │
│  ┌──────────────────────────────────────────────────────┐           │
│  │  ✓  Configure Network                  TO_DO        ✕ │           │
│  │  ✓  Install OS                         TO_DO        ✕ │           │
│  └──────────────────────────────────────────────────────┘           │
│  [ + Add Successor ]                                                │
│                                                                     │
│  ─────────────────────────────────────────────────────────         │
│  ℹ️ This task is BLOCKED. Complete 'Procure Hardware' first.         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**Add Predecessor Search Inline:**
```
┌──────────────────────────────────────────────────────┐
│  🔍 Search tasks in this project...                   │
│  ────────────────────────────────────────            │
│  ✓  Procure Hardware       [IN_PROGRESS]  🔒 Would Block │
│  ✓  Configure Network      [TO_DO]                    │
│  ✗  Install Server Rack    (current task – excluded)  │
│  ✓  Install OS             [TO_DO]                    │
└──────────────────────────────────────────────────────┘
```

Note: Items that would create a cycle are shown with ⚠️ icon and "Would create cycle" label, and are disabled (not selectable).

---

### 2.2 Dependency Graph View (Project Level)

```
┌─────────────────────────────────────────────────────────────────────┐
│  Data Center Migration                    [Board] [Graph ✓] [List]  │
├──────────────┬──────────────────────────────────────────────────────┤
│  [🔍 Search] │                      [Zoom In] [Zoom Out] [Fit View] │
│  [⬇ Export] │                                    [Export PNG] [PNG] │
│              │                                                       │
│  LEGEND      │   ┌──────────┐     ┌──────────────────┐             │
│  ──────────  │   │ Procure  │────▶│  Install Server  │             │
│  🟠 Critical  │   │ Hardware │     │     Rack         │             │
│  🔵 Normal   │   │[IN PROG] │     │   [🔒BLOCKED]    │             │
│  🔒 Blocked  │   └──────────┘     └────────┬─────────┘             │
│  ✓ Done      │                            ├────────────────────┐    │
│              │              ┌─────────────▼────────┐ ┌─────────▼──┐│
│  MINIMAP     │              │  Configure Network   │ │ Install OS  ││
│  ┌────────┐  │              │    [TO_DO] 🟠        │ │ [TO_DO] 🔵 ││
│  │ • • •  │  │              └─────────────┬────────┘ └─────────┬──┘│
│  │  •     │  │                            └──────────┬─────────┘   │
│  └────────┘  │                          ┌────────────▼────────┐    │
│              │                          │  Deploy Application  │    │
│              │                          │    [TO_DO] 🟠        │    │
│              │                          └─────────────┬────────┘    │
│              │                        ┌───────────────▼────────┐   │
│              │                        │  User Acceptance Test   │   │
│              │                        │    [TO_DO] 🟠           │   │
│              │                        └───────────────┬─────────┘   │
│              │                                ┌───────▼──────────┐  │
│              │                                │     Go Live       │  │
│              │                                │   [TO_DO] 🟠     │  │
│              │                                └──────────────────┘  │
└──────────────┴──────────────────────────────────────────────────────┘
```

---

### 2.3 Blocked Task Card (Kanban Board)

```
┌────────────────────────────────┐
│  🔒 BLOCKED                    │
│  Install Server Rack           │
│  ────────────────────────────  │
│  📌 Blocked by: Procure Hardware│
│  👤 Bu Sari                    │
│  📅 Due: 10 Jul 2026           │
│  ⏱ 8h estimated                │
└────────────────────────────────┘
```

---

## 3. Component Hierarchy

```
<ProjectDependencyGraphPage>
  ├── <ProjectTabNav>               -- Tabs: Board | Graph | List
  ├── <GraphToolbar>
  │   ├── <SearchInput>             -- Filter nodes by task name
  │   ├── <ZoomControls>            -- Zoom in, zoom out, fit view
  │   └── <ExportButton>            -- Export PNG
  ├── <ReactFlowProvider>
  │   ├── <ReactFlow>               -- Main graph canvas (DAG)
  │   │   ├── <TaskNode>            -- Custom node component
  │   │   │   ├── <NodeStatusBadge> -- TO_DO | IN_PROGRESS | BLOCKED | DONE
  │   │   │   ├── <NodeLabel>       -- Task name (truncated if > 25 chars)
  │   │   │   └── <CriticalBadge>  -- ⚡ icon if on critical path
  │   │   ├── <DependencyEdge>     -- Custom animated edge (arrow)
  │   │   ├── <Background>         -- Grid/dot background pattern
  │   │   ├── <MiniMap>            -- Mini-map for navigation
  │   │   └── <Controls>           -- Built-in zoom/pan controls
  │   └── <NodeTooltip>            -- Hover tooltip (Radix Tooltip)
  └── <TaskDetailDrawer>           -- Slides in when node is clicked
      └── <TaskDependencyPanel>    -- Dependency tab content
          ├── <PredecessorList>
          │   ├── <DependencyChip>  -- Each predecessor chip
          │   └── <AddDependencySearch>  -- Combobox search
          └── <SuccessorList>
              ├── <DependencyChip>
              └── <AddDependencySearch>
```

**Task Card (Kanban):**
```
<KanbanCard>
  ├── <BlockedBanner>              -- 🔒 BLOCKED header (conditional)
  ├── <CardTitle>                  -- Task name
  ├── <BlockedByInfo>             -- "Blocked by: [task name]" (conditional)
  ├── <AssigneeAvatar>
  └── <DueDateLabel>
```

---

## 4. UI Components Specification

### 4.1 TaskNode (React Flow Custom Node)

| Property | Value |
|---|---|
| **Shape** | Rounded rectangle (border-radius: 8px) |
| **Dimensions** | Min-width: 180px, Max-width: 220px; Height: 64px |
| **Background** | Normal: `bg-white dark:bg-zinc-800`; Critical: `bg-amber-50 dark:bg-amber-900/20` |
| **Border** | Normal: `border border-zinc-200`; Critical: `border-2 border-amber-500`; Blocked: `border-2 border-red-400 border-dashed`; Done: `border border-green-500` |
| **Shadow** | `shadow-sm`; on hover: `shadow-md ring-2 ring-blue-500` |
| **Label** | `text-sm font-medium text-zinc-900 dark:text-zinc-100` truncated with `title` tooltip |
| **Status Badge** | 12px pill badge, positioned bottom-left |
| **Critical Icon** | ⚡ amber icon, positioned top-right (only if on critical path) |
| **Handle** | Left (target) and Right (source) connection handles — hidden at rest, visible on node hover |

### 4.2 DependencyEdge (Custom Edge)

| Property | Value |
|---|---|
| **Type** | `smoothstep` (React Flow edge type) |
| **Stroke** | Normal: `#94a3b8` (zinc-400); Critical: `#f59e0b` (amber-400); width: 2px |
| **Animation** | `animated: true` for critical path edges (CSS dash-animation) |
| **Arrow** | MarkerEnd: arrowhead in matching color |
| **Label** | Optional — only shown if edge represents a lag time (TP-3 has no lag, so empty) |
| **Hover** | Stroke turns `#3b82f6` (blue-500); tooltip shows "Dependency: A → B" |

### 4.3 DependencyChip

| Property | Value |
|---|---|
| **Layout** | `flex items-center gap-2 px-3 py-2 rounded-lg border` |
| **Background** | Predecessor (blocked): `bg-red-50 border-red-200`; Successor (blocking): `bg-blue-50 border-blue-200`; Done predecessor: `bg-green-50 border-green-200` |
| **Icon** | 🔒 for active blocker; ✓ for done predecessor; → for successor |
| **Task Name** | `text-sm font-medium` + Status badge pill |
| **Remove Button** | `✕` button, `text-zinc-400 hover:text-red-500`; triggers confirmation dialog |

### 4.4 AddDependencySearch

| Property | Value |
|---|---|
| **Component** | shadcn/ui `Combobox` (built on Radix UI) |
| **Trigger** | "+ Add Predecessor" / "+ Add Successor" button with `Plus` icon |
| **Search** | Debounced input (300ms), queries `/api/projects/:id/tasks?search=` |
| **Results** | Task name + status badge; disabled items: current task (self), tasks that would create cycle (⚠️ icon), tasks already added |
| **Keyboard** | Arrow keys to navigate, Enter to select, Escape to close |

### 4.5 BlockedBanner (Task Card)

| Property | Value |
|---|---|
| **Background** | `bg-red-50 dark:bg-red-900/20` |
| **Text** | `🔒 BLOCKED` in `text-xs font-bold text-red-600 uppercase tracking-wide` |
| **Visibility** | Shown only when `task.status === 'BLOCKED'` |

### 4.6 GraphToolbar

| Control | Component | Behavior |
|---|---|---|
| Search tasks | `Input` with search icon | Filters graph nodes; non-matching nodes dim to 30% opacity |
| Zoom In / Zoom Out | Icon buttons | Calls `zoomIn()` / `zoomOut()` from React Flow instance |
| Fit View | Button | Calls `fitView()` — fits all nodes in viewport |
| Export PNG | Button with download icon | Calls `toObject()` + html-to-image library to download PNG |
| Legend toggle | Chevron button | Collapses/expands the sidebar legend |

---

## 5. Visual Design Guidelines

### 5.1 Color System

| Semantic | Token | Hex (Light) | Hex (Dark) |
|---|---|---|---|
| **Critical path node bg** | `--color-critical-node-bg` | `#fffbeb` (amber-50) | `#451a03/20` |
| **Critical path border** | `--color-critical-border` | `#f59e0b` (amber-500) | `#d97706` |
| **Critical path edge** | `--color-critical-edge` | `#f59e0b` | `#fbbf24` |
| **Blocked node border** | `--color-blocked-border` | `#f87171` (red-400) | `#ef4444` |
| **Normal node border** | `--color-default-border` | `#e2e8f0` (slate-200) | `#334155` |
| **Normal edge** | `--color-default-edge` | `#94a3b8` (slate-400) | `#64748b` |
| **Done node border** | `--color-done-border` | `#22c55e` (green-500) | `#16a34a` |
| **Graph background** | `--color-graph-bg` | `#f8fafc` (slate-50) | `#0f172a` |
| **Graph dots** | `--color-graph-dots` | `#e2e8f0` | `#1e293b` |

### 5.2 Status Badge Colors

| Status | Background | Text | Icon |
|---|---|---|---|
| `TO_DO` | `bg-slate-100` | `text-slate-600` | ⬜ |
| `IN_PROGRESS` | `bg-blue-100` | `text-blue-700` | 🔵 |
| `BLOCKED` | `bg-red-100` | `text-red-700` | 🔒 |
| `REVIEW` | `bg-purple-100` | `text-purple-700` | 👁 |
| `DONE` | `bg-green-100` | `text-green-700` | ✓ |
| `CANCELLED` | `bg-zinc-100` | `text-zinc-400` | ✗ |

### 5.3 Typography

| Element | Style |
|---|---|
| Graph node title | `text-sm font-medium` — Inter 14px/500 |
| Status badge text | `text-xs font-semibold` — Inter 11px/600 |
| Tooltip header | `text-sm font-semibold` — Inter 14px/600 |
| Tooltip body | `text-xs text-zinc-500` — Inter 12px/400 |
| Section header (panel) | `text-xs font-semibold uppercase tracking-widest text-zinc-400` |
| Dependency chip label | `text-sm font-medium` |

### 5.4 Spacing

| Context | Value |
|---|---|
| Node padding | `12px 16px` |
| Node gap in auto-layout | Horizontal: 80px; Vertical: 48px |
| Panel section spacing | `gap-4` (16px) |
| Dependency chip list gap | `gap-2` (8px) |
| Toolbar height | `h-12` (48px) |

---

## 6. Responsive Design

| Breakpoint | Graph Behavior | Panel Behavior |
|---|---|---|
| **Desktop (≥ 1280px)** | Full graph canvas; sidebar legend visible | Task Dependency Panel as 400px right drawer |
| **Tablet (768px – 1279px)** | Graph canvas fills width; legend collapses to toggle button | Dependency panel as bottom sheet (50% height) |
| **Mobile (< 768px)** | Graph view disabled; replaced by "Dependency List" — linear list of predecessor/successor relationships | Full-screen bottom sheet with simplified list |

> [!NOTE]
> The full DAG graph canvas is only available on tablet/desktop. On mobile, users manage dependencies through the text-based list view within the task detail, which is functionally equivalent but without the visual canvas.

---

## 7. Graph Interactions & Animations

### 7.1 Node Interactions

| Interaction | Trigger | Animation / Behavior |
|---|---|---|
| **Hover** | Mouse enters node | Border brightens; shadow elevates (`shadow-md`); handles become visible; 150ms ease-in-out |
| **Click** | Mouse click on node | Task Detail Drawer slides in from right (300ms slide + fade); node gets focus ring |
| **Keyboard focus** | Tab to node | Focus ring (`ring-2 ring-blue-500`); same drawer behavior on Enter |
| **Selected** | After click | Node highlighted with `ring-2 ring-blue-500 ring-offset-2`; connected edges highlighted |

### 7.2 Edge Interactions

| Interaction | Trigger | Behavior |
|---|---|---|
| **Hover** | Mouse enters edge | Stroke color → blue-500; tooltip shows "A → B (Finish-to-Start)"; 150ms ease |
| **Critical path pulse** | Critical path active | Animated dashed stroke (CSS `stroke-dashoffset` animation, 1.5s loop) |

### 7.3 Graph Layout Animations

| Action | Animation |
|---|---|
| **Initial load** | Nodes fade in sequentially from left to right (50ms stagger); edges draw in after nodes |
| **Add dependency** | New edge animates in (path drawing animation, 400ms); affected node border transitions color |
| **Remove dependency** | Edge fades out (200ms); node border transitions back |
| **Auto-unblock** | Blocked node border transitions from red-dashed to slate in 500ms; status badge updates; brief green flash on node |
| **Fit view** | Smooth pan + zoom using React Flow's built-in `fitView` with `duration: 800` |
| **Node search filter** | Non-matching nodes animate to 30% opacity (200ms); matching nodes brighten |

### 7.4 Panel Interactions

| Action | Animation |
|---|---|
| **Add Predecessor button click** | Combobox opens with `slide-down + fade` (150ms) |
| **Dependency chip add** | New chip slides in from left (200ms, `slide-right + fade-in`) |
| **Dependency chip remove (after confirm)** | Chip slides out to left (200ms, `slide-left + fade-out`) |
| **Remove confirmation dialog** | Modal dialog with overlay fade (200ms) + dialog scale-in (200ms) |

---

## 8. Accessibility

### 8.1 Graph Keyboard Navigation

| Key | Behavior |
|---|---|
| `Tab` | Cycle through graph nodes in document order (left-to-right, top-to-bottom) |
| `Shift+Tab` | Reverse cycle through nodes |
| `Enter` or `Space` | Open Task Detail Drawer for focused node |
| `Arrow Keys` | Pan the graph canvas (10px per keypress) |
| `+` / `-` | Zoom in / zoom out |
| `0` | Reset zoom to 100% |
| `F` | Fit all nodes in view |
| `Escape` | Close Task Detail Drawer; return focus to graph canvas |

### 8.2 ARIA Attributes

| Element | ARIA |
|---|---|
| Graph canvas | `role="application"` `aria-label="Project dependency graph for [project name]"` |
| Each task node | `role="button"` `aria-label="[Task name], status: [status], [critical path status]"` `tabIndex={0}` |
| Blocked task chip | `aria-label="Blocked by [task name], status [status]"` |
| Remove dependency button | `aria-label="Remove dependency with [task name]"` |
| Add predecessor button | `aria-label="Add predecessor task"` |
| Combobox search | `aria-label="Search for predecessor task"` `role="combobox"` `aria-expanded` `aria-autocomplete="list"` |
| Combobox options | `role="option"` `aria-selected` `aria-disabled` (for cycle/self items) |
| Critical path badge | `aria-label="This task is on the critical path"` |
| Loading state | `aria-busy="true"` `aria-label="Loading dependency graph..."` |

### 8.3 Color Contrast

| Element | Ratio | WCAG Target |
|---|---|---|
| Node label on white bg | ≥ 7:1 | AAA |
| Critical border on amber bg | ≥ 4.5:1 | AA |
| Status badge text on badge bg | ≥ 4.5:1 | AA |
| Blocked text on red-50 bg | ≥ 4.5:1 | AA |

### 8.4 Screen Reader Announcements

| Event | Announcement |
|---|---|
| Dependency added | "Dependency added: [Task A] is now a predecessor of [Task B]. Task B is now blocked." |
| Dependency removed | "Dependency removed. [Task B] is no longer blocked by [Task A]." |
| Task unblocked | "Task unblocked: [Task B] is now ready to start." |
| Circular dependency rejected | "Error: Adding this dependency would create a circular dependency cycle." |
| Graph loaded | "[N] tasks and [M] dependencies loaded. Critical path has [K] tasks." |

---

## 9. UI States

### 9.1 Dependency Panel States

| State | Visual |
|---|---|
| **No dependencies** | Empty state illustration (node with no connections); text: "No dependencies defined. Add predecessors or successors to control task execution order." + CTA buttons |
| **Has predecessors only** | Predecessor list visible; successor section shows empty state with "+ Add Successor" |
| **Has successors only** | Successor list visible; predecessor section shows empty state |
| **Task is BLOCKED** | Amber info banner: "🔒 This task is blocked. Complete all predecessors before starting."; all predecessor chips show 🔒 icon except DONE ones (✓ icon) |
| **All predecessors DONE** | Green info banner: "✅ All predecessors complete. This task is ready to start." |
| **Loading dependencies** | Skeleton loader: 2 chip-shaped rectangles animating |
| **Error fetching deps** | "Failed to load dependencies. [Retry]" with retry button |

### 9.2 Dependency Graph States

| State | Visual |
|---|---|
| **No tasks in project** | Full-page empty state: illustration of empty graph; "No tasks added yet. Create tasks to visualize dependencies." |
| **Tasks exist, no dependencies** | All task nodes shown as isolated nodes; banner: "💡 No dependencies defined. Click any task to add dependencies." |
| **Normal — has dependencies, no critical path** | Standard graph with blue edges; no amber highlights |
| **Critical path visible** | Amber nodes and animated edges on critical path; legend visible |
| **Graph loading** | Full canvas skeleton: animated shimmer overlay |
| **Graph load error** | "Failed to load dependency graph. [Retry]" centered in canvas |
| **Node focused (keyboard)** | Focus ring around node; connected edges highlighted in blue |
| **Search active** | Non-matching nodes dimmed; matching nodes in full opacity; search results count shown in toolbar |

---

## 10. Assets & Design Tokens

### 10.1 Icons Used

| Icon | Library | Usage |
|---|---|---|
| `Lock` | Lucide | Blocked state indicators |
| `Zap` | Lucide | Critical path badge |
| `Plus` | Lucide | Add dependency buttons |
| `X` | Lucide | Remove dependency |
| `GitBranch` | Lucide | Dependency tab icon |
| `Network` | Lucide | Graph view tab icon |
| `Download` | Lucide | Export PNG button |
| `Search` | Lucide | Node search field |
| `AlertTriangle` | Lucide | Would-create-cycle warning in search dropdown |
| `CheckCircle2` | Lucide | Done predecessor chip |
| `ZoomIn` / `ZoomOut` | Lucide | Graph zoom controls |
| `Maximize2` | Lucide | Fit view button |

### 10.2 Figma Component References

| Component | Figma Frame |
|---|---|
| TaskNode (all states) | `FluxGrid/TaskProject/Graph/TaskNode` |
| DependencyEdge styles | `FluxGrid/TaskProject/Graph/Edges` |
| DependencyChip | `FluxGrid/TaskProject/DependencyPanel/Chip` |
| BlockedBanner | `FluxGrid/TaskProject/TaskCard/BlockedBanner` |
| AddDependencySearch | `FluxGrid/TaskProject/DependencyPanel/AddSearch` |
| Graph Canvas Layout | `FluxGrid/TaskProject/GraphPage/Layout` |

---

*Document Owner: FluxGrid ERP Design Team*
*Linked Story: TP-3 Task Dependency Management*
