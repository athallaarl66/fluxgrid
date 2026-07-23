# Production Requirements: Automatic CV Parsing (HR-5)

## 1. Feature Overview
- **Feature Name**: AI-Powered CV Parsing
- **Module**: HR & Payroll (Recruitment Sub-module)
- **User Story**: As an HR Recruiter, I want the system to automatically parse uploaded CVs so that I don't have to manually enter candidate data.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Manual data entry for hundreds of applicants is a massive bottleneck. By using LLMs (Groq API) to extract unstructured text from PDFs into structured JSON, HR can instantly screen, filter, and search candidates based on skills and experience.
- **ROI Estimation**: Saves approximately 15 minutes of manual data entry per CV. For 100 applicants, that's 25 hours of HR time saved instantly.

## 3. Success Metrics
- > 85% accuracy in extracting key fields (Name, Email, Phone, Skills).
- Background parsing completes within 30 seconds per CV.
- System gracefully handles unparseable formats (e.g., scanned images) by prompting for manual entry.

## 4. User Persona
- **HR Recruiter**: Reviews the AI-extracted data for accuracy before finalizing the candidate profile.

## 5. User Journey
1. **Background Trigger**: Immediately after CV Upload (HR-4), the candidate status is `DRAFT`. A background job begins.
2. **Text Extraction**: The system reads the PDF and extracts raw text.
3. **AI Parsing**: The raw text is sent to the Groq API with a strict JSON schema prompt.
4. **Data Mapping**: The JSON response is mapped to the database (Personal Info, Education, Experience, Skills).
5. **Review**: The Recruiter opens the Candidate list, sees a "Parsed - Needs Review" badge.
6. **Validation**: The Recruiter views a split-screen UI (Original PDF on the left, extracted data fields on the right). They correct any mistakes (e.g., misidentified graduation year) and click "Approve Data". After review, Recruiter clicks 'Approve Data' which first saves any edits via PUT /candidates/{id} endpoint, then transitions status to ACTIVE.

## 6. Acceptance Criteria
- [ ] Background job queue for processing CVs asynchronously.
- [ ] PDF text extraction capability.
- [ ] Groq API integration prompting for structured JSON output.
- [ ] Database population for candidate relational tables (Education, Experience, Skills).
- [ ] Split-screen Review UI for human-in-the-loop validation.
- [ ] Fallback mechanism if the Groq API fails or rate limits.
- [ ] PUT /candidates/{id} endpoint for persisting review edits.
- [ ] Activity log entry created on data edit.

## 7. Edge Cases and Constraints
- **Image-based PDFs**: If a PDF is a scanned image, standard text extraction will fail. The system must flag the status as `PARSE_FAILED` and alert the user. OCR is out of scope for this iteration.
- **Hallucinations**: LLMs might hallucinate data not present in the CV. The human-in-the-loop review step is mandatory to mitigate this.

## 8. Dependencies on Other Modules
- Dependent on **HR-4** for the raw PDF file.
- Feeds data into **HR-6 (Job Matching)**.

## 9. Out of Scope
- Direct OCR (Optical Character Recognition) for scanned images. Only text-based PDFs are supported.
