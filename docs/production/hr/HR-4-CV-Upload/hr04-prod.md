# Production Requirements: CV Upload (HR-4)

## 1. Feature Overview
- **Feature Name**: Candidate CV Upload
- **Module**: HR & Payroll (Recruitment Sub-module)
- **User Story**: As an HR Recruiter, I want to upload candidate CVs so that they are stored in the recruitment pipeline.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: The entry point for the recruitment funnel. A streamlined upload process ensures no candidates are lost in email threads. It feeds raw data to the AI parsing engine (HR-5).
- **ROI Estimation**: Centralizes applicant tracking, reducing time spent searching for files by 100%.

## 3. Success Metrics
- 100% of uploaded CVs (PDF/DOCX) are successfully stored in cloud storage (e.g., AWS S3 or equivalent) and linked to a database record.
- Bulk upload capability (up to 20 CVs at once) succeeds without timing out.

## 4. User Persona
- **HR Recruiter**: Uploads CVs received from job fairs, emails, or referrals.

## 5. User Journey
1. **Initiation**: HR Recruiter opens the "Candidates" dashboard and clicks "Upload CV".
2. **File Selection**: User selects 5 PDF files from their local computer.
3. **Validation**: System checks that all files are < 5MB and are PDF/DOCX format.
4. **Upload**: Files are uploaded to secure storage.
5. **Database Entry**: System creates 5 "Candidate" records in `DRAFT` status, attaching the file URLs.
6. **Trigger AI**: (Handled in HR-5) The system queues these draft records for automatic parsing.

## 6. Acceptance Criteria
- [ ] Drag-and-drop file upload UI.
- [ ] File type validation (strictly PDF and DOCX).
- [ ] File size validation (max 5MB per file).
- [ ] Support for bulk upload (multiple files simultaneously).
- [ ] Secure file storage (not publicly accessible).
- [ ] Creation of a baseline Candidate record linked to the file.

## 7. Edge Cases and Constraints
- **Corrupted Files**: If an uploaded PDF is password-protected or corrupted, the system must flag the candidate record with an "Upload Error" status.
- **Duplicate Uploads**: If the exact same file (hash check) is uploaded twice, warn the user.

## 8. Dependencies on Other Modules
- Foundational requirement for **HR-5 (CV Parsing)**.

## 9. Out of Scope
- Public-facing career page for candidates to upload their own CVs (This is strictly an internal HR tool for this iteration).
