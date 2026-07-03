# Design Specifications: Automatic CV Parsing (HR-5)

## 1. Screen Overview
**Page:** Human-in-the-loop Validation Screen (Split-screen review)

## 2. Wireframe Description
```text
=== Candidate Validation: John Doe ===
[Status: PARSED - NEEDS REVIEW] [Button: Approve Data] [Button: Reject Candidate]

+-----------------------------------+-----------------------------------+
|             [PDF Viewer]          |         [Editable Data]           |
|                                   |                                   |
|  JOHN DOE                         |  First Name: [John              ] |
|  Software Engineer                |  Last Name:  [Doe               ] |
|  john@email.com                   |  Email:      [john@email.com    ] |
|                                   |  Phone:      [+62812345678      ] |
|  EXPERIENCE                       |                                   |
|  - Google (2020-2023)             |  Experience:                      |
|    Frontend Dev...                |  [1] Company: [Google           ] |
|                                   |      Role:    [Frontend Dev     ] |
|  EDUCATION                        |      Years:   [2020] to [2023   ] |
|  - MIT (2016-2020)                |      [Delete]                     |
|                                   |  [+ Add Experience]               |
|                                   |                                   |
|  SKILLS                           |  Skills:                          |
|  React, Typescript, Node.js       |  [React] [x] [Typescript] [x]     |
+-----------------------------------+-----------------------------------+
```

## 3. Component Hierarchy
- `CandidateValidationPage`
  - `HeaderBar`
  - `SplitPaneContainer`
    - `PdfViewerPane` (Uses `react-pdf` to render the original file)
    - `DataFormPane` (Zod validated React Hook Form)
      - `PersonalSection`
      - `ExperienceFieldArray`
      - `EducationFieldArray`
      - `SkillsInput` (Tokenized tags)

## 4. UI Components (shadcn/ui)
- `Resizable` (Split pane component) to allow users to drag the middle divider.
- `Form` with deep nested `useFieldArray` for repeating sections like Experience.
- `Badge` (Destructive variant for tags in the Skills section).
- `ScrollArea` for both panes independently.

## 5. Visual Guidelines
- **AI Sparkles**: The "Approve Data" workflow must visually indicate that this data was AI-generated and requires the user's explicit blessing before entering the permanent database. Use a tooltip stating "Extracted via AI".
- **Density**: The form pane must be extremely compact to fit on a standard 1080p laptop screen alongside the PDF. Use small text inputs.

## 6. Responsive Design
- Split-screen is impossible on mobile. On mobile devices, the UI should stack vertically (PDF on top, form on bottom) or hide the PDF behind a "View Original" modal button.

## 7. States & Interactions
- **Scrolling Sync**: (Optional Enhancement) When clicking on the "Experience" section in the form, automatically scroll the PDF viewer down slightly.
- **Save State**: Auto-save the form as drafts in case the recruiter closes the tab mid-review.

## 8. Accessibility
- Ensure the PDF viewer provides a text layer, not just a canvas image rendering, so users with screen readers can still read the original CV.
