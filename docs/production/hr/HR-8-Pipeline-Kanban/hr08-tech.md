# Technical Specs: Pipeline Kanban Board (HR-8)

## 1. Database
- **candidate_activity_logs** table: id, candidate_id, action, performed_by, details (text), created_at
- **candidate_job_matches** table: id, candidate_id, job_id, score, is_manual, created_at
- Indexes: (candidate_id, created_at) on activity_logs, unique (candidate_id, job_id) on job_matches

## 2. API Endpoints
- `PUT /api/v1/hr/recruitment/candidates/{id}/status` — Change status with transition validation
- `GET /api/v1/hr/recruitment/candidates/{id}/activities` — Get activity log with pagination
- `POST /api/v1/hr/recruitment/candidates/{id}/activities` — Add note
- `POST /api/v1/hr/recruitment/candidates/{id}/jobs` — Assign to job (score=1.0, is_manual=true)
- `DELETE /api/v1/hr/recruitment/candidates/{id}/jobs/{jobId}` — Unassign from job
- `GET /api/v1/hr/recruitment/candidates/{id}/jobs` — List assigned jobs
- `POST /api/v1/hr/recruitment/candidates/bulk-assign` — Bulk assign candidates to job

## 3. Frontend
- **@dnd-kit/core** for drag-and-drop (PointerSensor, closestCenter collision)
- **KanbanBoard.tsx** — DndContext with columns and cards
- **KanbanColumn.tsx** — useDroppable, status label, candidate count
- **KanbanCard.tsx** — useDraggable, candidate info, drag handle
- **ActivityTimeline.tsx** — Timeline with icons per action type
- **AllApplicantsTable.tsx** — Match table with AI/Manual badge, sort, filter

## 4. State Machine
- DRAFT → PARSED (auto), PARSE_FAILED (auto)
- PARSED → ACTIVE (manual), REJECTED (manual)
- ACTIVE → INTERVIEW (manual), REJECTED (manual), ARCHIVED (manual)
- INTERVIEW → HIRED (manual), REJECTED (manual), ARCHIVED (manual)
- Any → ARCHIVED (manual)
