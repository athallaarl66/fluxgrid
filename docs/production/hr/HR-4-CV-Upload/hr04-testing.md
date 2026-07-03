# Testing Scenarios: CV Upload (HR-4)

## 1. Test Strategy Overview
Testing for file uploads focuses on boundary conditions (file sizes, unsupported types), security (preventing malicious payloads), and concurrency (bulk uploading).

## 2. Test Cases

### TC-01: Single Valid PDF Upload (Happy Path)
- **Given** an HR Recruiter on the upload page.
- **When** they drag and drop a 2MB PDF file and click "Upload".
- **Then** the UI shows a success progress bar.
- **And** a draft Candidate record is created in the database with the S3 URL.

### TC-02: Invalid File Type (Negative Testing)
- **Given** the upload UI.
- **When** the user attempts to upload an `.exe`, `.jpg`, or `.txt` file.
- **Then** the frontend immediately rejects the file (red border) before uploading.
- **And** if bypassed via API, the backend returns a 415 Unsupported Media Type error.

### TC-03: File Size Exceeded (Negative Testing)
- **Given** the upload UI.
- **When** the user attempts to upload a 10MB PDF.
- **Then** the frontend immediately rejects it.
- **And** if bypassed, the backend returns a 413 Payload Too Large error.

### TC-04: Bulk Upload Success
- **Given** the user selects 5 valid PDF files.
- **When** they click "Upload All".
- **Then** the system successfully uploads all 5 files concurrently.
- **And** 5 individual candidate records are created in the database.

### TC-05: Duplicate File Hash Check
- **Given** a PDF that was previously uploaded.
- **When** the user uploads the exact same file again.
- **Then** the backend calculates the SHA-256 hash, detects a match, and returns a warning: "This file has already been uploaded."

## 3. Performance Testing
- Uploading 10 files (2MB each) simultaneously should not block the main Node.js event loop or cause memory spikes. Use streaming uploads direct to S3 (Presigned URLs) rather than buffering in the Next.js server memory.

## 4. Security & Access Testing
- Storage buckets must be private. The URLs saved in the database must require authentication to view.
- Ensure strict MIME-type checking on the backend, not just trusting the file extension, to prevent malicious script uploads masquerading as PDFs.
