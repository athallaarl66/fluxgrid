# Technical Specifications: CV Upload (HR-4)

## 1. System Architecture
- **Frontend**: Next.js Client Components (react-dropzone).
- **Backend**: Next.js API Routes for generating Presigned URLs and saving DB records.
- **Storage**: AWS S3 or equivalent (Cloudflare R2, Supabase Storage) via presigned URLs.
- **Database**: PostgreSQL (Neon).

## 2. Database Schema

### Table: `candidates`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `file_url` | TEXT | NOT NULL | S3 Object Key or URL |
| `file_hash` | VARCHAR(64) | UNIQUE | SHA-256 hash to prevent duplicates |
| `status` | VARCHAR(20) | NOT NULL | DRAFT, PARSED, PARSE_FAILED, REJECTED, HIRED |
| `uploaded_by`| UUID | FK | Reference to users |
| `tenant_id` | UUID | NOT NULL, FK | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, text, timestamp, uniqueIndex } from "drizzle-orm/pg-core";

export const candidates = pgTable("candidates", {
  id: uuid("id").primaryKey().defaultRandom(),
  fileUrl: text("file_url").notNull(),
  fileHash: varchar("file_hash", { length: 64 }).notNull(),
  status: varchar("status", { length: 20 }).notNull().default("DRAFT"),
  uploadedBy: uuid("uploaded_by").notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
}, (table) => {
  return {
    tenantHashIdx: uniqueIndex("tenant_hash_idx").on(table.tenantId, table.fileHash),
  };
});
```

## 4. API Endpoints

### POST `/api/v1/hr/recruitment/upload-url`
- **Description**: Requests a presigned URL for direct-to-S3 upload.
- **Request Body**: `filename`, `file_type`, `file_hash`.
- **Action**: 
  1. Validates `file_type` (application/pdf).
  2. Checks if `file_hash` exists in `candidates` for this tenant.
  3. Returns a presigned URL valid for 5 minutes.

### POST `/api/v1/hr/recruitment/candidates`
- **Description**: Confirms upload success and creates the DB record.
- **Request Body**: `file_url`, `file_hash`.
- **Action**: 
  1. Inserts into `candidates` with status `DRAFT`.
  2. Dispatches `CandidateUploaded` event.

## 5. Domain Events
- **Raised**: `CandidateUploaded` -> Consumed by the CV Parsing engine (HR-5) to trigger the background extraction job.
- **Consumed**: None.

## 6. Permissions (RBAC)
- `hr.recruitment.manage`: Required to upload CVs.

## 7. Performance Considerations
- Direct-to-S3 uploads bypass the Next.js API server entirely, preventing Vercel function timeouts (which are strict on free tiers, often 10s or 60s) when uploading 10MB PDFs.

## 8. Security Considerations
- **Content-Type Enforcement**: The S3 presigned URL must strictly enforce the `ContentType` header so attackers cannot upload an `.exe` file under a presigned URL generated for a `.pdf`.
- **Private Buckets**: The S3 bucket must block all public access. The UI must request short-lived presigned GET URLs to view the CV later.

## 9. Error Handling
- If S3 upload succeeds but the DB insert fails (e.g., race condition on `file_hash`), the frontend must gracefully inform the user.

## 10. Integration
- The `CandidateUploaded` event is immediately queued to Upstash QStash to trigger the heavy AI parsing job in the background.
