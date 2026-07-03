# Design Specifications: Dashboard (dashboard)

## 1. Screen Overview & Layout
- **Main View**: Full‑screen landing page displaying a responsive grid of module cards.
- **Header**: Fixed top bar with logo, user avatar, and global notification icon.
- **Grid**: 2‑column layout on desktop, 1‑column on mobile, spacing `gap-6`.
- **Footer**: Small copyright line and link to help docs.

## 2. Wireframe Description (text based)
```
+----------------------+----------------------+
|   WMS Card          |   Finance Card       |
|  Icon + Name       |  Icon + Name        |
|  Metric: Orders    |  Metric: Revenue    |
+----------------------+----------------------+
|   HR Card           |   Projects Card      |
|  Icon + Name       |  Icon + Name        |
|  Metric: Absences  |  Metric: Tasks      |
+----------------------+----------------------+
```
- Each card is a **shadcn/ui Card** component with hover shadow and subtle glass‑morphism background (`bg-white/80` with `backdrop-blur`).
- Cards contain:
  - Module icon (svg from `/public/icons/`)
  - Module name
  - One key KPI (placeholder value)
  - Clickable area (`cursor-pointer`).

## 3. Component Hierarchy
- `DashboardPage`
  - `Header`
  - `DashboardGrid`
    - `ModuleCard` (x4)
  - `Footer`

## 4. UI Components
- **Card** – `shadcn/ui` Card with custom `bg-gradient-to-r`.
- **Grid** – Tailwind CSS `grid grid-cols-2 md:grid-cols-2 gap-6`.
- **Icon** – Inline SVG, size `24px`.
- **MetricBadge** – Small badge with animated count (`animate-count-up`).

## 5. Visual Guidelines
- **Color Palette** – Use existing brand colors (`primary-600`, `secondary-500`). Cards use light glass background with `border border-gray-200/50`.
- **Typography** – `Inter` heading `text-2xl font-bold`, body `text-sm`.
- **Spacing** – 24 px padding inside cards, 16 px margin between cards.
- **Micro‑animations** – Hover elevation `transform scale-105` + `shadow-lg`, badge count animation on load.

## 6. Responsive Design
- **Desktop (≥768px)** – 2‑column grid, icons 32 px.
- **Tablet (≥640px && <768px)** – 2‑column grid, reduced padding.
- **Mobile (<640px)** – Single‑column stack, full‑width cards.

## 7. Accessibility
- ARIA `role="button"` on cards, `aria-label="Navigate to WMS module"`.
- Keyboard focus visible (`focus-visible:ring-2`), `Enter` activates navigation.
- Contrast ratio ≥ 4.5:1 for text on glass background.

## 8. UI States
| State | Visual Cue |
|------|------------|
| **Default** | Light glass card, subtle shadow. |
| **Hover** | `scale-105`, `shadow-xl`, background gradient shift. |
| **Focus** | `ring-2 ring-primary-500`. |
| **Disabled (no permission)** | Card opacity `0.4`, tooltip `You lack permission`. |
| **Loading** | Skeleton placeholder (`animate-pulse`). |
| **Error** | Red accent border, retry button overlay.

## 9. Interaction Flow
1. Page loads → fetch metrics via `/api/dashboard`.
2. While loading → show skeleton cards.
3. On success → populate KPI badges.
4. User hovers → elevation animation.
5. Click → router push to module route.
6. If API fails → show fallback with retry button.

---
