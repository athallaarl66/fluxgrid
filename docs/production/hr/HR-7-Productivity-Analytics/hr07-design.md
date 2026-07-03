# Design Specifications: Productivity Analytics (HR-7)

## 1. Screen Overview
**Page:** Workforce Analytics Dashboard

## 2. Wireframe Description
```text
=== Workforce Analytics ===
[Filters: Department (IT) | Date (Last 30 Days) | Compare against (Company Avg)]

[ Metrics Row ]
[ Utilization Rate: 88% ]  [ Avg Tasks/Week: 14 ]  [ Overtime Hours: 120h ]

[ Main Chart Area ]
Title: Utilization vs Overtime Trend
[ Line Chart: 
  - X-Axis: Weeks (Week 1, Week 2...)
  - Y-Axis Left: % Utilization (Blue Line)
  - Y-Axis Right: Overtime Hours (Red Line) 
]

[ Top Performers Table ]
| Name       | Role         | Utilization | Tasks Completed |
| John Doe   | Senior Dev   | 95%         | 42              |
| Jane Smith | QA Engineer  | 92%         | 38              |
```

## 3. Component Hierarchy
- `AnalyticsDashboardPage`
  - `FilterToolbar` (Selects, DateRangePicker)
  - `MetricsGrid`
    - `MetricCard` (Number + Trend indicator e.g., ^ 5%)
  - `ChartsContainer`
    - `LineChart` (Recharts library)
    - `BarChart` (Recharts library)
  - `LeaderboardTable`

## 4. UI Components (shadcn/ui)
- `Card` for wrapping charts and metrics.
- `Select` for filtering.
- Recharts for data visualization (not natively shadcn, but standard ecosystem choice).

## 5. Visual Guidelines
- **Color Palettes for Charts**: Use distinct, color-blind friendly palettes for the charts. Avoid relying purely on Red/Green.
- **Data Empty States**: If the TaskProject module hasn't been used yet, the charts shouldn't look broken. Show a clear "Not Enough Data" graphic with a call-to-action explaining how the feature works.

## 6. Responsive Design
- Dashboards with complex charts are best viewed on desktop. On mobile, charts should become scrollable horizontally, or collapse into simple summary metric cards.

## 7. States & Interactions
- **Tooltip Interactivity**: Hovering over a point on the line chart should show a tooltip with the exact underlying numbers for that specific date/week.

## 8. Accessibility
- All charts must have an alternative table view or descriptive text summarizing the trend, as Canvas/SVG charts are inherently difficult for screen readers to parse.
