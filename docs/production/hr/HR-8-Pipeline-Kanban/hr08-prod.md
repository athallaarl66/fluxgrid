# Production Requirements: Pipeline Kanban Board (HR-8)

## 1. Feature Overview
- **Feature Name**: Candidate Pipeline Kanban Board
- **Module**: HR & Payroll (Recruitment Sub-module)
- **User Story**: As an HR Recruiter, I want a visual pipeline board to manage candidate statuses through the hiring workflow.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Provides visual overview of recruitment pipeline. Drag-and-drop reduces clicks for status changes. Activity log ensures audit trail for compliance.
- **ROI Estimation**: Reduces time-to-shortlist by 40% through visual pipeline management.

## 3. Success Metrics
- All status transitions are logged in activity timeline
- Drag-and-drop works for all valid transitions
- Manual role assignment persists correctly

## 4. Acceptance Criteria
- [ ] Kanban board with columns per status
- [ ] Drag-and-drop status transitions with validation
- [ ] Activity log with timeline view per candidate
- [ ] Add note functionality in activity log
- [ ] Manual job assignment (assign/unassign)
- [ ] Bulk assign candidates to job posting
- [ ] "All Applicants" tab in job detail with AI reasoning

## 5. Dependencies
- HR-4 (CV Upload), HR-5 (CV Parsing), HR-6 (Job Matching)
