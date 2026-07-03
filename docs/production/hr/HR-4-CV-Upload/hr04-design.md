# Design Specifications: CV Upload (HR-4)

## 1. Screen Overview
**Page:** Recruitment Dashboard -> CV Upload Modal

## 2. Wireframe Description
```text
=== CV Upload Modal ===
Title: Upload Candidate CVs

[ Drag & Drop Zone ]
(Icon: Cloud Upload)
Drag and drop PDF or DOCX files here
or [Browse Files]
Limit: 5MB per file, max 20 files at once.

[ File Queue ]
- [Icon: PDF] John_Doe_Resume.pdf (1.2MB) [Status: Uploading 50%...]
- [Icon: PDF] Jane_Smith_CV.pdf (2.1MB)   [Status: Queued]
- [Icon: DOC] Invalid_File.jpg (6MB)      [Status: Error - File too large] [X]

[Button: Cancel] [Button: Upload All (Disabled if errors exist)]
```

## 3. Component Hierarchy
- `RecruitmentDashboard`
  - `UploadCvDialog`
    - `DropzoneArea` (react-dropzone)
    - `FileQueueList`
      - `FileItem`
        - `ProgressBar`
        - `DeleteButton`

## 4. UI Components (shadcn/ui)
- `Dialog` for the upload interface.
- `Progress` for individual file upload status.
- `Alert` / `Toast` for overall success or failure messages.

## 5. Visual Guidelines
- **Dropzone Feedback**: The drag-and-drop area must visually change state (e.g., dashed border turns solid green, background dims) when the user hovers a file over it.
- **Micro-interactions**: Use smooth transitions for adding/removing items from the File Queue list.

## 6. Responsive Design
- While mobile users rarely upload CVs in bulk, the "Browse Files" button must work on mobile browsers to allow selecting a PDF from iCloud/Google Drive.

## 7. States & Interactions
- **Direct to S3**: The frontend should request a Presigned URL from the Next.js API, and then PUT the file directly to the storage bucket, updating the UI progress bar using `XMLHttpRequest` or `axios` upload tracking.

## 8. Accessibility
- The Dropzone must be clickable via keyboard.
- Use `aria-valuenow` on the progress bars so screen readers announce upload completion.
