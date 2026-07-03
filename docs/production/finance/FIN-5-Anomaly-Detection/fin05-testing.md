# Testing Scenarios: Anomaly Detection (FIN-5)

## 1. Test Strategy Overview
Testing involves simulating transactions that breach statistical thresholds and ensuring the background job correctly flags them, invokes the Groq API, and handles API rate limits gracefully.

## 2. Test Cases

### TC-01: Detect Amount Anomaly (Happy Path)
- **Given** an account "Office Supplies" with an historical average transaction amount of 1,000,000 and standard deviation of 200,000.
- **When** a user posts a new transaction for 10,000,000.
- **And** the anomaly detection batch job runs.
- **Then** the transaction is flagged as an anomaly.
- **And** an AI summary is generated explaining the deviation.

### TC-02: Ignore Normal Transaction
- **Given** the same "Office Supplies" account.
- **When** a user posts a transaction for 1,100,000.
- **And** the batch job runs.
- **Then** the transaction is NOT flagged.

### TC-03: Cold Start Ignore
- **Given** a newly created account with only 2 historical transactions.
- **When** a massive transaction is posted.
- **Then** the statistical engine ignores it until a minimum threshold of historical data (e.g., 5 transactions) is reached to prevent false positives.

### TC-04: Groq API Rate Limit Fallback (Negative Testing)
- **Given** a statistically anomalous transaction.
- **When** the batch job calls the Groq API, but the API returns a 429 Rate Limit error.
- **Then** the transaction is still flagged in the database.
- **And** the AI summary field is populated with a fallback message: "Statistical anomaly detected. AI summary unavailable due to rate limits."

### TC-05: Resolve Anomaly Workflow
- **Given** a flagged anomaly in the dashboard.
- **When** the CFO clicks "Mark as Resolved" and selects "Valid Transaction".
- **Then** the anomaly status is updated to "Resolved".
- **And** it is removed from the active alerts view.

## 3. Performance Testing
- The statistical aggregation query must be optimized. It shouldn't calculate the mean/stddev from scratch across millions of rows every time. Use a rolling average or materialized view for baseline statistics.

## 4. Security & Access Testing
- Only users with `finance.audit.read` or `finance.admin` can view the anomalies dashboard.
- The cron endpoint triggering the batch job must be secured via a secret token.
