# Testing Scenarios: Job Matching (HR-6)

## 1. Test Strategy Overview
Testing semantic search requires evaluating whether the mathematical vector comparisons return logically sound human results. It relies heavily on database-level pgvector indexing.

## 2. Test Cases

### TC-01: Exact Skill Match (Happy Path)
- **Given** a Job Posting requiring "React, Typescript, Next.js".
- **When** the recruiter clicks "Find Matches".
- **Then** Candidate A (whose CV explicitly lists React, Typescript, Next.js) appears at the top of the list.
- **And** their match score is > 90%.

### TC-02: Semantic Match (Happy Path)
- **Given** a Job Posting requiring "Frontend Development".
- **When** the recruiter searches.
- **Then** Candidate B (whose CV lists "React, HTML, CSS" but never explicitly says "Frontend") still scores highly (e.g., > 80%).
- **And** Candidate C (whose CV is entirely "Backend, PostgreSQL, Java") scores poorly (e.g., < 30%).

### TC-03: Minimum Threshold Filtering
- **Given** a job with a minimum match threshold set to 50%.
- **When** the matching algorithm runs across 100 candidates.
- **Then** the UI only displays the 15 candidates who scored 50% or higher.

### TC-04: Embedding Generation Failure (Negative Testing)
- **Given** the Groq API is down or rate-limited.
- **When** HR attempts to publish a new Job Posting (which triggers embedding generation).
- **Then** the job saves in `DRAFT` status.
- **And** the UI alerts the user: "Failed to generate AI search index. Please try publishing again later."

### TC-05: Bias Verification (Manual QA)
- **Given** two identical CVs, where one has the name "John Doe" and the other "Jane Smith".
- **When** both are matched against a job.
- **Then** their match scores must be exactly identical (down to the decimal point). (Note: This is ensured by stripping PII before sending text to the embedding model).

## 3. Performance Testing
- `pgvector` Cosine distance searches can be slow if doing a full table scan on millions of rows. Ensure an `hnsw` (Hierarchical Navigable Small World) index is created on the embedding column so the search over 10,000 candidates returns in < 500ms.

## 4. Security & Access Testing
- Candidates from Tenant A must never be matched or shown in Tenant B's job searches (Ensure `tenant_id` is applied as a strict `WHERE` clause before the vector search).
