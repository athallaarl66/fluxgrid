# Technical Specifications: Anomaly Detection (FIN-5)

## 1. System Architecture
- **Trigger**: A daily cron job (Vercel Cron) or event-driven hook on `JournalEntryPosted`. (Batch job via Upstash QStash is preferred to prevent blocking synchronous requests).
- **Statistical Engine**: A PostgreSQL query/view calculating Mean and Standard Deviation per account.
- **AI Service Layer**: Calls the Groq API (Llama 3 70B or Mixtral) to generate human-readable summaries for flagged transactions.
- **Database**: PostgreSQL (Neon) to store the anomalies.

## 2. Database Schema

### Table: `financial_anomalies`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | |
| `entry_id` | UUID | NOT NULL, FK | Reference to `journal_entries` |
| `account_id` | UUID | NOT NULL, FK | Reference to `chart_of_accounts` |
| `amount` | DECIMAL | NOT NULL | The anomalous amount |
| `historical_mean`| DECIMAL| NOT NULL | For context |
| `deviation_score`| DECIMAL| NOT NULL | Number of standard deviations (e.g., 3.5) |
| `ai_summary` | TEXT | | LLM generated explanation |
| `status` | VARCHAR(20) | NOT NULL | PENDING, RESOLVED_VALID, RESOLVED_SUSPICIOUS |
| `resolution_notes`| TEXT | | |
| `resolved_by` | UUID | FK | Reference to users |
| `tenant_id` | UUID | NOT NULL, FK | |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, decimal, text, timestamp } from "drizzle-orm/pg-core";
import { journalEntries } from "./fin02";
import { chartOfAccounts } from "./fin01";

export const financialAnomalies = pgTable("financial_anomalies", {
  id: uuid("id").primaryKey().defaultRandom(),
  entryId: uuid("entry_id").references(() => journalEntries.id).notNull(),
  accountId: uuid("account_id").references(() => chartOfAccounts.id).notNull(),
  amount: decimal("amount").notNull(),
  historicalMean: decimal("historical_mean").notNull(),
  deviationScore: decimal("deviation_score").notNull(),
  aiSummary: text("ai_summary"),
  status: varchar("status", { length: 20 }).notNull().default("PENDING"),
  resolutionNotes: text("resolution_notes"),
  resolvedBy: uuid("resolved_by"),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
});
```

## 4. Groq API Integration (Prompt Engineering)
- **Role**: `system` -> "You are an AI financial auditor. Review the following transaction data against its historical context and provide a brief, professional summary explaining why it was flagged as anomalous. Do not provide advice, just summarize the mathematical anomaly clearly."
- **Data Payload**: "Account: Office Supplies. Transaction Amount: 50,000,000. Historical Mean: 2,000,000. StdDev: 500,000. Date Posted: Sunday, 3 AM."
- **Output Format**: Text/String.

## 5. API Endpoints

### POST `/api/v1/cron/finance-anomaly-scan`
- **Description**: Triggered by Vercel Cron. Enqueues scanning tasks to Upstash QStash.

### POST `/api/v1/finance/anomalies/process`
- **Description**: QStash worker endpoint. Runs the statistical check. If `Amount > (Mean + 3 * StdDev)`, calls Groq API and inserts into `financial_anomalies`.

### GET `/api/v1/finance/anomalies`
- **Description**: Fetch pending anomalies for the dashboard.

### PUT `/api/v1/finance/anomalies/{id}/resolve`
- **Description**: Resolve an anomaly.
- **Request Body**: `status` (RESOLVED_VALID, etc.), `resolution_notes`.

## 6. Permissions (RBAC)
- `finance.audit.read`: View anomalies.
- `finance.audit.manage`: Resolve anomalies.

## 7. Performance Considerations
- Calculating statistical baseline (Mean/StdDev) across millions of rows daily is heavy. 
- **Optimization**: Maintain a materialized view `account_statistics` that updates nightly, containing `mean` and `stddev` per account. The anomaly scan then only does a simple `JOIN` and `WHERE amount > (mean + 3 * stddev)`.

## 8. Security Considerations
- Protect the cron endpoint using `CRON_SECRET`.
- LLM prompt injection is low risk here since the input is purely numerical system data, not user-generated text.

## 9. Error Handling
- **Groq Rate Limits (429)**: Use a resilient queue (Upstash) to retry with exponential backoff. If max retries reached, save the anomaly with `ai_summary` = "Statistical anomaly. AI summary unavailable."
