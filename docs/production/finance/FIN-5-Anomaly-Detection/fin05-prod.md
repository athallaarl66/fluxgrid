# Production Requirements: Anomaly Detection (FIN-5)

## 1. Feature Overview
- **Feature Name**: AI Anomaly Detection
- **Module**: Finance - General Ledger & Reporting
- **User Story**: As a CFO, I want AI to detect anomalous transactions so that potential fraud can be identified early.
- **Priority**: Should Have

## 2. Business Value & ROI
- **Business Value**: Internal fraud or simple human error (e.g., typing an extra zero) can severely impact cash flow and reporting. An automated watchdog system continuously scans for statistical outliers and uses AI to explain *why* it's suspicious, acting as an automated internal auditor.
- **ROI Estimation**: Reduces the time auditors spend manually reviewing ledgers by 80%. Potentially saves millions in caught fraud or errors.

## 3. Success Metrics
- System scans newly posted journal entries within 5 minutes of posting (or via daily batch).
- Generates a human-readable summary of the anomaly using Groq API.
- False positive rate is acceptable (< 20% of flagged transactions are deemed "normal" by human reviewers).

## 4. User Persona
- **Internal Auditor / CFO**: Reviews flagged transactions in the Anomaly Dashboard.
- **Finance Staff**: May be asked to provide justification if their entry is flagged.

## 5. User Journey
1. **Transaction Posting**: Finance Staff posts a Journal Entry for "Office Supplies" for Rp 50,000,000.
2. **Background Scan**: A background job detects that the average for this account is Rp 2,000,000. It deviates > 3 Standard Deviations from the mean.
3. **AI Generation**: The system sends the statistical context to the Groq API.
4. **Alert Creation**: An `anomaly` record is created.
5. **Review**: The CFO logs in and sees an alert badge. They open the dashboard, read the AI summary ("This entry is 25x higher than the historical average for Office Supplies. It was posted outside standard business hours..."), and click the link to investigate the entry.
6. **Action**: CFO marks the anomaly as "Resolved - Valid" or "Investigating - Suspicious".

## 6. Acceptance Criteria
- [ ] Statistical engine to detect outliers based on amount and account history (Mean + Standard Deviation).
- [ ] Integration with Groq API to generate plain-text reasoning.
- [ ] Dashboard to view, filter, and resolve anomalies.
- [ ] Notification system for auditors.

## 7. Edge Cases and Constraints
- **Cold Start Problem**: If an account is brand new, there is no historical average. The system should ignore accounts with fewer than 5 historical transactions.
- **Groq API Limits**: If the free tier limit is reached, the system should still flag the transaction statistically but gracefully note: "AI summary unavailable due to rate limits."

## 8. Dependencies on Other Modules
- Dependent on **FIN-2** (Journal Entries).

## 9. Out of Scope
- Real-time blocking of transactions (Anomaly detection happens *after* posting to avoid disrupting workflow).
- Complex Machine Learning anomaly models (Isolation Forests, etc.). We rely on a statistical rule-based trigger + LLM summarization.
