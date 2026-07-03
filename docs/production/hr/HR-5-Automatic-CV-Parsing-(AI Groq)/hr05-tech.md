# Technical Specifications: Automatic CV Parsing (HR-5)

## 1. System Architecture
- **Trigger**: Upstash QStash webhook triggered by the `CandidateUploaded` event from HR-4.
- **PDF Extraction**: Node.js library `pdf-parse` (or equivalent serverless-friendly tool).
- **AI Service Layer**: Groq API (Llama 3) for inference.
- **Database**: PostgreSQL (Neon).

## 2. Database Schema

### Table: `candidate_profiles` (1:1 with `candidates`)
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `candidate_id`| UUID | PRIMARY KEY, FK | Reference to `candidates` |
| `first_name` | VARCHAR(100) | | |
| `last_name` | VARCHAR(100) | | |
| `email` | VARCHAR(255) | | |
| `phone` | VARCHAR(50) | | |
| `raw_text` | TEXT | | The unparsed PDF text |
| `tenant_id` | UUID | NOT NULL, FK | |

### Table: `candidate_experience` (1:N)
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `candidate_id`| UUID | NOT NULL, FK | |
| `company_name`| VARCHAR(255) | | |
| `job_title` | VARCHAR(255) | | |
| `start_year` | INT | | |
| `end_year` | INT | | NULL means 'Present' |
| `description` | TEXT | | |

*(Similar tables for `candidate_education` and `candidate_skills`)*

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, text, integer } from "drizzle-orm/pg-core";
import { candidates } from "./hr04";

export const candidateProfiles = pgTable("candidate_profiles", {
  candidateId: uuid("candidate_id").primaryKey().references(() => candidates.id),
  firstName: varchar("first_name", { length: 100 }),
  lastName: varchar("last_name", { length: 100 }),
  email: varchar("email", { length: 255 }),
  phone: varchar("phone", { length: 50 }),
  rawText: text("raw_text"),
  tenantId: uuid("tenant_id").notNull(),
});

export const candidateExperience = pgTable("candidate_experience", {
  id: uuid("id").primaryKey().defaultRandom(),
  candidateId: uuid("candidate_id").references(() => candidates.id).notNull(),
  companyName: varchar("company_name", { length: 255 }),
  jobTitle: varchar("job_title", { length: 255 }),
  startYear: integer("start_year"),
  endYear: integer("end_year"),
  description: text("description"),
});
// Education and Skills omitted for brevity
```

## 4. Groq API Integration (Prompt Engineering)
- **Model**: `llama3-70b-8192` (Preferred for complex JSON schemas).
- **Prompt**: 
  "You are an expert HR data extractor. Extract the following candidate information from the provided raw CV text. 
  You MUST respond ONLY with a valid JSON object matching this exact schema:
  `{ firstName: string, lastName: string, email: string, phone: string, experience: [{ company: string, title: string, startYear: number, endYear: number|null }], education: [{ institution: string, degree: string, year: number }], skills: [string] }`
  Do not include markdown blocks or any other text."
- **JSON Mode**: Enable `response_format: { type: "json_object" }` if supported by the Groq SDK.

## 5. API Endpoints

### POST `/api/v1/hr/recruitment/parse-webhook`
- **Description**: Upstash QStash target endpoint.
- **Action**:
  1. Downloads PDF from S3.
  2. Extracts text via `pdf-parse`.
  3. Sends text to Groq.
  4. Uses Zod to strictly validate the Groq JSON response.
  5. Inserts into relational tables.
  6. Updates `candidates.status` to `PARSED`.

### PUT `/api/v1/hr/recruitment/candidates/{id}/approve`
- **Description**: Recruiter confirms the AI data is correct.
- **Action**: Updates status from `PARSED` to `APPROVED`.

## 6. Permissions (RBAC)
- Background jobs require an API secret token.
- `hr.recruitment.manage`: Required to approve the parsed data.

## 7. Performance Considerations
- **Token Limits**: A very long CV might exceed the context window. Truncate the raw text to the first 4000 tokens before sending to Groq to ensure stability.

## 8. Security Considerations
- The webhook endpoint MUST verify the QStash signature (`@upstash/qstash/nextjs`) to prevent attackers from hitting the endpoint and racking up Groq API costs.

## 9. Error Handling
- **Zod Parsing Failures**: If Groq returns a hallucinated JSON structure, Zod will throw an error. Catch this, increment a retry counter in QStash, and try again. After 3 failures, mark as `PARSE_FAILED`.
