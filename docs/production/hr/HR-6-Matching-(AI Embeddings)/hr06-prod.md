# Production Requirements: Job Matching (HR-6)

## 1. Feature Overview
- **Feature Name**: AI Job Matching
- **Module**: HR & Payroll (Recruitment Sub-module)
- **User Story**: As an HR Recruiter, I want AI to match candidates to open job positions so that I can quickly shortlist the best talent.
- **Priority**: Must Have

## 2. Business Value & ROI
- **Business Value**: Manually cross-referencing a candidate's CV against 10 open job descriptions is slow and prone to human bias. By using Vector Embeddings (pgvector), the system can mathematically rank how well a candidate's skills and experience match a specific job requirement in milliseconds.
- **ROI Estimation**: Reduces time-to-hire by 30% by instantly surfacing top-tier candidates who might otherwise be buried in a large applicant pool.

## 3. Success Metrics
- Match scores (0-100%) are generated for every candidate against an open job position.
- High-scoring candidates (e.g., >80%) empirically perform better in human-led interviews.
- Semantic matching works (e.g., matching "ReactJS" in the CV with "Frontend Development" in the Job Description, not just exact keyword matches).

## 4. User Persona
- **HR Recruiter / Hiring Manager**: Reviews the ranked shortlist and decides who to invite for an interview.

## 5. User Journey
1. **Job Creation**: HR creates a new Job Posting (e.g., "Senior Software Engineer") and writes a detailed requirement list.
2. **AI Indexing**: The system converts the Job Description into a mathematical vector (embedding) using Groq API and saves it.
3. **Candidate Pool**: 50 parsed candidates (from HR-5) exist in the database. Their skills/experience are also converted to embeddings.
4. **Matching**: The Recruiter opens the "Senior Software Engineer" job page and clicks "Find Matches".
5. **Ranking**: The system performs a vector similarity search (Cosine Distance) and returns a list of candidates ranked by match percentage. The 'All Applicants' tab shows both AI-matched and manually assigned candidates with AI reasoning.
6. **Shortlisting**: The Recruiter reviews the top 5 candidates and clicks "Shortlist".

## 6. Acceptance Criteria
- [ ] CRUD for Job Postings.
- [ ] Generation of Vector Embeddings for Job Postings and Candidate Profiles via Groq API.
- [ ] Use `pgvector` in PostgreSQL for fast similarity search.
- [ ] Display a ranked list of candidates per job with a match score (0-100%).
- [ ] Semantic matching capability (understanding context, not just keywords).
- [ ] Manual role assignment: assign candidates to jobs manually (score=1.0).
- [ ] All Applicants tab with AI vs Manual badge, sort, filter.
- [ ] Shortlist/Reject actions from All Applicants tab.

## 7. Edge Cases and Constraints
- **Bias Mitigation**: AI matching should strictly look at skills and experience, blinding itself to Name, Gender (if inferred), or Age to prevent discriminatory shortlisting.
- **Cost**: Generating embeddings costs API tokens. Only generate embeddings when a Job is published or a Candidate is "Approved", not on every minor draft edit.

## 8. Dependencies on Other Modules
- Dependent on structured data from **HR-5 (CV Parsing)**.

## 9. Out of Scope
- Automated rejection emails to low-scoring candidates (Recruiters must manually reject).
