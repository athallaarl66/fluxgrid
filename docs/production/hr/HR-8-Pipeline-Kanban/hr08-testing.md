# Test Cases: Pipeline Kanban Board (HR-8)

## TC-01: Drag Valid Transition
- **Given** a candidate in PARSED status
- **When** user drags card to ACTIVE column
- **Then** candidate status changes to ACTIVE, activity log entry created

## TC-02: Drag Invalid Transition
- **Given** a candidate in DRAFT status
- **When** user drags card to HIRED column
- **Then** error toast shown, status unchanged

## TC-03: Add Activity Note
- **Given** a candidate detail page
- **When** user types a note and clicks send
- **Then** note appears in activity timeline

## TC-04: Manual Job Assignment
- **Given** a candidate in ACTIVE status
- **When** user clicks "+ Assign" and selects a job
- **Then** job appears in "Assigned Jobs" section, activity log entry created

## TC-05: Unassign Job
- **Given** a candidate assigned to a job
- **When** user clicks X on the job assignment
- **Then** job removed from list, activity log entry created

## TC-06: All Applicants Tab
- **Given** a job with both AI-matched and manually assigned candidates
- **When** user opens "All Applicants" tab
- **Then** table shows all candidates with AI/Manual badge, sortable by score
