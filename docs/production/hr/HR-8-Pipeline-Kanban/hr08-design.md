# Design Specs: Pipeline Kanban Board (HR-8)

## 1. Screen Layout
- **Kanban Board**: Horizontal scrollable layout with columns for each status
- **Columns**: DRAFT | PARSED | ACTIVE | INTERVIEW | HIRED | REJECTED (collapsed) | ARCHIVED (collapsed)
- **Card**: Candidate name, top 3 skills, upload date, match score badge
- **Drag Handle**: GripVertical icon visible on hover

## 2. Interactions
- **Drag & Drop**: Cards can be dragged between columns to change status
- **Drop Zone**: Column highlights green when card is dragged over
- **Invalid Drop**: Toast error if transition not allowed
- **Click Card**: Navigates to candidate detail page

## 3. Activity Log Panel
- Located in candidate detail page sidebar
- Timeline view with icons per action type
- "Add Note" input at top
- Actions: STATUS_CHANGED, ASSIGNED_TO_JOB, REMOVED_FROM_JOB, NOTE_ADDED, CV_UPLOADED, PARSE_COMPLETED, DATA_EDITED

## 4. Manual Role Assignment
- "Assigned Jobs" section in candidate detail sidebar
- "+ Assign" button opens job dropdown (published jobs only)
- X button to unassign
- Badge: "Manual" or "AI" match type
