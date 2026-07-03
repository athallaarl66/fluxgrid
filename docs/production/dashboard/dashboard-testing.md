# Testing Scenarios: Dashboard (dashboard)

## 1. Test Strategy Overview
- **Scope**: End‑to‑end UI rendering, navigation, API integration, performance, security, accessibility.
- **Approach**: Playwright E2E tests covering happy path, edge cases, and negative scenarios.

## 2. Test Cases (Given/When/Then)
1. **Happy Path – Render Dashboard**
   - **Given** a logged‑in user with `Dashboard:Read` permission
   - **When** the user navigates to `/dashboard`
   - **Then** the page renders a responsive grid with four module cards and real‑time metric placeholders.
2. **Navigation – Module Card Click**
   - **Given** the dashboard is displayed
   - **When** the user clicks the **WMS** card
   - **Then** the browser navigates to `/wms` and the URL changes accordingly.
3. **Permission Restriction – Hidden Card**
   - **Given** a user lacking `Finance:Read` permission
   - **When** the dashboard loads
   - **Then** the **Finance** card is not rendered.
4. **Unauthenticated Access**
   - **Given** an unauthenticated visitor
   - **When** they request `/dashboard`
   - **Then** they are redirected to the login page.
5. **API Failure – Fallback UI**
   - **Given** the backend `/api/dashboard` returns 500
   - **When** the dashboard loads
   - **Then** a fallback placeholder with a “Retry” button is shown.
6. **Performance – Load Time**
   - **Given** a normal network condition
   - **When** the dashboard page is requested
   - **Then** the page load time (TTI) is < 300 ms (P95).
7. **Accessibility – Keyboard Navigation**
   - **Given** a keyboard‑only user
   - **When** they tab through the dashboard
   - **Then** each module card receives focus and can be activated via `Enter`.
8. **Responsive – Mobile Layout**
   - **Given** a viewport width of 375 px
   - **When** the dashboard is rendered
   - **Then** the module cards stack vertically with adequate spacing.
9. **Security – CSP Header**
   - **Given** the response from `/dashboard`
   - **When** the browser inspects the headers
   - **Then** a strict Content‑Security‑Policy is present.
10. **Error Handling – Retry Logic**
    - **Given** the API fails initially
    - **When** the user clicks the “Retry” button
    - **Then** the dashboard re‑fetches data and displays metrics upon success.

## 3. Edge Cases & Constraints
- Empty metric data (all zeros) – UI should show `0` or `N/A` without breaking layout.
- Extremely long module names – truncate with ellipsis.
- Slow network (>= 3 s) – loading skeleton should appear.

## 4. Performance Testing Requirements
- Simulate 100 concurrent users accessing `/dashboard` – verify average response < 400 ms.
- Measure front‑end TTI under throttled 3G.

## 5. Security Testing Requirements
- Verify JWT token is required (401 on missing/invalid token).
- Test RBAC enforcement for each card.
- Ensure no sensitive data is exposed in the metric payload.

## 6. Accessibility Testing Requirements
- Run axe‑core analysis – no violations of WCAG 2.1 AA.
- Test screen‑reader navigation (NVDA/VoiceOver).

## 7. Acceptance Criteria Mapping
| Acceptance Criteria | Covered By Test |
|---------------------|-----------------|
| Dashboard renders responsive grid | 1,8 |
| Card navigation works | 2 |
| Permission based visibility | 3 |
| Unauthenticated redirect | 4 |
| API failure fallback UI | 5 |
| Load time <300 ms | 6 |
| Keyboard navigation | 7 |
| CSP header present | 9 |
| Retry logic works | 10 |

## 8. Test Data Requirements
- Mock users with varying RBAC permissions.
- Mock API responses for metrics (static JSON).
- Network fault simulation (abort, timeout).
