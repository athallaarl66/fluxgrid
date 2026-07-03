---
name: Industrial Modern ERP
colors:
  surface: '#fdf8f5'
  surface-dim: '#ddd9d6'
  surface-bright: '#fdf8f5'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f7f3f0'
  surface-container: '#f1edea'
  surface-container-high: '#ece7e4'
  surface-container-highest: '#e6e2df'
  on-surface: '#1c1b1a'
  on-surface-variant: '#49473e'
  inverse-surface: '#31302e'
  inverse-on-surface: '#f4f0ed'
  outline: '#7a776d'
  outline-variant: '#cac6bb'
  surface-tint: '#625f4b'
  primary: '#625f4b'
  on-primary: '#ffffff'
  primary-container: '#f6f0d7'
  on-primary-container: '#706d59'
  inverse-primary: '#ccc7af'
  secondary: '#546434'
  on-secondary: '#ffffff'
  secondary-container: '#d4e7ab'
  on-secondary-container: '#586838'
  tertiary: '#556341'
  on-tertiary: '#ffffff'
  tertiary-container: '#e6f6ca'
  on-tertiary-container: '#63714f'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#e8e3ca'
  primary-fixed-dim: '#ccc7af'
  on-primary-fixed: '#1e1c0d'
  on-primary-fixed-variant: '#4a4735'
  secondary-fixed: '#d7eaae'
  secondary-fixed-dim: '#bbce94'
  on-secondary-fixed: '#141f00'
  on-secondary-fixed-variant: '#3d4c1f'
  tertiary-fixed: '#d9e8be'
  tertiary-fixed-dim: '#bdcca3'
  on-tertiary-fixed: '#141f05'
  on-tertiary-fixed-variant: '#3e4b2c'
  background: '#fdf8f5'
  on-background: '#1c1b1a'
  surface-variant: '#e6e2df'
typography:
  headline-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '600'
    lineHeight: '1.2'
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.3'
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '600'
    lineHeight: '1.4'
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.5'
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.4'
  body-sm:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: '1.4'
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: '1'
    letterSpacing: 0.02em
  label-sm:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '600'
    lineHeight: '1'
  data-mono:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: '1'
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  unit: 4px
  xs: 4px
  sm: 8px
  md: 12px
  lg: 16px
  xl: 24px
  gutter: 12px
  container-padding: 20px
---

## Brand & Style

This design system is built for heavy industry environments where reliability and information density are paramount. The "Industrial-Modern" aesthetic moves away from cold, sterile grays toward a functional, parchment-based palette that reduces eye strain during long shifts while maintaining a technical, engineering-grade feel.

The style is a hybrid of **Minimalism** and **Structured Utility**. It prioritizes data clarity and systematic organization over decorative flair. The emotional response should be one of stability, precision, and operational readiness.

**Key Principles:**
- **Information Density:** High-density layouts inspired by technical blueprints and modern developer tools.
- **Functional Warmth:** Utilizing a cream base to improve legibility and provide a premium, durable feel.
- **Precision:** Sharp alignment, consistent line weights, and clear visual hierarchies.

## Colors

The palette is rooted in Earth-toned technicality. The primary background (#F6F0D7) acts as a high-readability canvas, while the Sage and Olive accents provide a sophisticated, calm method of categorization and action-indication.

- **Surface:** The primary parchment color is used for the main application background. Use a slightly darkened version (#EFE9CF) for sidebar containers.
- **Accents:** Use the Light Sage (#C5D89D) for primary actions and highlighted states.
- **Typography:** The Deep Olive (#89986D) serves as the primary text color, ensuring high contrast without the harshness of pure black.
- **Feedback:** Use semantic red and green but slightly desaturated to align with the industrial palette.

## Typography

The design system utilizes **Inter** for its exceptional legibility and systematic feel. For an ERP, typography must prioritize "scannability."

- **Data Tables:** Use `body-sm` for table rows to maximize visible data. Enable tabular numbers (`tnum`) for all numerical columns to ensure vertical alignment of digits.
- **Hierarchy:** Use `label-md` in uppercase for section headers and table column headers to create a distinct visual break from content.
- **Mobile:** Scale headlines down by 20% on mobile devices, but maintain body font sizes to preserve accessibility in high-glare industrial environments.

## Layout & Spacing

This design system uses a strict **4px grid** to achieve high information density. The layout philosophy is a **Fluid-Fixed Hybrid**: sidebars are fixed-width to ensure navigation stability, while data modules expand to fill the horizontal viewport.

- **Density:** Elements should be tightly packed with `spacing-sm` (8px) between related controls and `spacing-md` (12px) for general gutters.
- **Grid:** Use a 12-column system for dashboard layouts. In data-heavy views, prefer a simple flex-stack or CSS grid to allow for "unlimited" horizontal scrolling on tables.
- **Breakpoints:**
  - Mobile: < 768px (Single column, hidden sidebars)
  - Tablet: 768px - 1200px (Collapsed sidebars, 2-column dashboards)
  - Desktop: > 1200px (Expanded sidebars, full multi-column layout)

## Elevation & Depth

To maintain the "Industrial Modern" look, this design system avoids heavy drop shadows. Depth is communicated through **Tonal Layering** and **Low-Contrast Outlines**.

- **Surfaces:** Use a 1px solid border (#E5DEBF) for all containers instead of shadows.
- **Active State:** When an element is raised (e.g., a modal or a dragged Kanban card), use a very subtle, tight ambient shadow: `0 2px 4px rgba(137, 152, 109, 0.1)`.
- **Z-Index Strategy:**
  - Level 0: Background (#F6F0D7)
  - Level 1: Cards/Modules (#FFFFFF)
  - Level 2: Popovers/Tooltips (White with Border)
  - Level 3: Modals/Overlays (Stronger border, dimmed background)

## Shapes

The shape language is "Soft-Technical." We use minimal rounding to suggest modern software without losing the rigid, disciplined feel of engineering tools.

- **Standard Elements:** Buttons and input fields use a 0.25rem (4px) radius.
- **Containers:** Large modules or cards use 0.5rem (8px).
- **Status Badges:** Use a "Pill" shape (fully rounded) only for status indicators (e.g., "Active," "Pending") to make them instantly recognizable against the rectangular grid.

## Components

### Data Tables
- **Header:** Sticky headers with `label-sm` text. Use a #9CAB84 bottom border (2px).
- **Rows:** Alternating zebra stripes are not required; use 1px subtle borders between rows. Row height should be fixed at 36px for high density.
- **Filtering:** Inline "Filter Bar" directly above the table using `spacing-sm`.

### Buttons
- **Primary:** Background #C5D89D, Text #89986D (Bold). No shadow, 1px border of #9CAB84.
- **Secondary/Ghost:** Transparent background, #89986D border.
- **State:** On hover, darken the background by 5%. On press, 1px inset shadow.

### Input Fields
- **Styling:** White background, 1px border (#E5DEBF). Focus state uses a 1px solid #9CAB84 ring.
- **Labels:** Always top-aligned, never floating, using `label-sm`.

### Kanban Cards
- **Structure:** Clean white background, 1px border. Top color-strip (2px) indicates priority (Red/Yellow/Green).
- **Density:** Compact text with `body-sm`.

### AI Insight Modules
- **Visual:** Use a subtle gradient background from #F6F0D7 to #C5D89D.
- **Iconography:** Use a specific "Spark" icon in #89986D to denote AI-generated suggestions or automated optimizations.
