# Technical Specifications: Job Matching (HR-6)

## 1. System Architecture
- **Backend API**: Next.js Server Actions.
- **AI Service Layer**: Groq API to generate Text Embeddings (e.g., using an embedding model if supported, or via OpenAI API fallback for embeddings specifically).
- **Database**: PostgreSQL (Neon) with the `pgvector` extension installed.

## 2. Database Schema

### Table: `job_postings`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `title` | VARCHAR(200) | NOT NULL | e.g., "Senior React Developer" |
| `description` | TEXT | NOT NULL | Full JD text |
| `embedding` | VECTOR(1536)| | The mathematical representation of the JD |
| `status` | VARCHAR(20) | NOT NULL | DRAFT, PUBLISHED, CLOSED |
| `tenant_id` | UUID | NOT NULL, FK | |

### Table: `candidate_profiles` (Modification)
We add an `embedding` column to the table created in HR-5.
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `embedding` | VECTOR(1536)| | Vector representation of the candidate's parsed skills and experience. |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, text, timestamp } from "drizzle-orm/pg-core";
import { customType } from 'drizzle-orm/pg-core';

// Define a custom vector type for Drizzle (since pgvector isn't natively standard yet)
const vector = customType<{ data: number[] }>({
  dataType() {
    return 'vector(1536)';
  },
  toDriver(value) {
    return `[${value.join(',')}]`;
  },
  fromDriver(value: unknown) {
    // Basic parsing string "[0.1, 0.2]" to number[]
    if (typeof value === 'string') {
      return value.slice(1, -1).split(',').map(Number);
    }
    return [];
  }
});

export const jobPostings = pgTable("job_postings", {
  id: uuid("id").primaryKey().defaultRandom(),
  title: varchar("title", { length: 200 }).notNull(),
  description: text("description").notNull(),
  embedding: vector("embedding"),
  status: varchar("status", { length: 20 }).notNull().default("DRAFT"),
  tenantId: uuid("tenant_id").notNull(),
});
```

## 4. Groq API / Embedding Integration
- **Context**: Groq primarily runs LLMs (Llama, Mixtral) very fast. If Groq does not offer a dedicated *Embedding* endpoint, you may need to use OpenAI `text-embedding-3-small` specifically just for the vector generation, as it is extremely cheap.
- **Data Prep**: To generate a candidate's embedding, concatenate their parsed JSON: `"Skills: React, Node. Experience: 3 years at Google as Frontend Developer."` Send this text to the embedding API.

## 5. API Endpoints

### POST `/api/v1/hr/recruitment/jobs/{id}/publish`
- **Description**: Publishes a job and generates its vector embedding.
- **Action**: Calls Embedding API -> `UPDATE job_postings SET embedding = $1, status = 'PUBLISHED'`.

### GET `/api/v1/hr/recruitment/jobs/{id}/matches`
- **Description**: Finds candidates for this job.
- **Action**: 
  Executes `pgvector` Cosine Distance search (`<=>` operator):
  ```sql
  SELECT candidate_id, 
         1 - (candidate_profiles.embedding <=> job_postings.embedding) AS match_score
  FROM candidate_profiles
  CROSS JOIN job_postings
  WHERE job_postings.id = $1 
    AND candidate_profiles.tenant_id = $2
  ORDER BY match_score DESC
  LIMIT 50;
  ```

## 6. Permissions (RBAC)
- `hr.recruitment.read`: View matches.

## 7. Performance Considerations
- You MUST create an `HNSW` (Hierarchical Navigable Small World) index on the `embedding` columns. Without it, PostgreSQL performs a sequential scan calculating the distance against every single row, which will cripple the database if the tenant has thousands of CVs.
  ```sql
  CREATE INDEX ON candidate_profiles USING hnsw (embedding vector_cosine_ops);
  ```

## 8. Security Considerations
- PII (Name, Email, Phone) MUST NOT be included in the text string sent to the embedding model, both for privacy compliance (GDPR/PDPA) and to prevent the model from capturing bias based on names.

## 9. Error Handling
- If the embedding API is down, fail gracefully when publishing the job, keeping it in `DRAFT`.
