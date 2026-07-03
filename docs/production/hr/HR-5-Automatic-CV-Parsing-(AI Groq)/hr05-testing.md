# Testing Scenarios: Automatic CV Parsing (HR-5)

## 1. Test Strategy Overview
Testing the AI parser is highly non-deterministic because LLM outputs can vary. The strategy relies on testing the pipeline (Upload -> Extract -> JSON validation) and ensuring the system handles unexpected LLM outputs gracefully via Zod schema parsing.

## 2. Test Cases

### TC-01: Standard Format CV Parsing (Happy Path)
- **Given** a standard PDF CV containing clear sections (Contact, Education, Experience).
- **When** the background job processes the file.
- **Then** the Groq API returns a valid JSON matching the schema.
- **And** the Candidate record status updates to `PARSED`.
- **And** the `candidate_education` and `candidate_experience` tables are populated accurately.

### TC-02: Missing Information Handling
- **Given** a CV that completely lacks an "Education" section.
- **When** the background job processes it.
- **Then** the JSON returned has an empty array for `education`.
- **And** the system does not crash or throw a null pointer exception.

### TC-03: Invalid LLM Output (Negative Testing)
- **Given** the Groq API accidentally returns plain text instead of structured JSON (hallucination).
- **When** the backend attempts to parse the response via `JSON.parse()`.
- **Then** the parsing fails.
- **And** the job retry mechanism kicks in.
- **And** after max retries, the status is set to `PARSE_FAILED`.

### TC-04: Image-based PDF (Negative Testing)
- **Given** a PDF that is just a scanned JPEG of a resume.
- **When** the text extraction library runs.
- **Then** the extracted text length is < 50 characters.
- **And** the system immediately flags it as `PARSE_FAILED` without wasting a Groq API call.

### TC-05: Rate Limit Handling
- **Given** the Groq API free tier limit is reached (HTTP 429).
- **When** the parsing job runs.
- **Then** the job is returned to the Upstash queue with an exponential backoff delay.
- **And** the candidate status remains `DRAFT` (or `PROCESSING`) until retried.

## 3. Performance Testing
- Ensure the background job queue can handle 20 concurrent parsing tasks without causing memory leaks in the Vercel Serverless environment.

## 4. Security & Access Testing
- The extracted data must be sanitized before insertion into the DB to prevent any SQL injection if the LLM hallucinated malicious payloads (ORM handles this).
- Prevent prompt injection attacks: Do not allow candidate text to override the strict system prompt instructions.
