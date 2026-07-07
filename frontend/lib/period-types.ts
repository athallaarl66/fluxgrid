// Period type definitions for the Finance Period Closing feature

export enum PeriodStatus {
  OPEN = "OPEN",
  CLOSED = "CLOSED",
}

/**
 * Represents a financial accounting period.
 * Mirrors the backend PeriodDto shape.
 */
export interface Period {
  /** Unique identifier */
  id: string;
  /** Human‑readable label, e.g. "2024‑01" */
  name: string;
  /** ISO start date (inclusive) */
  startDate: string;
  /** ISO end date (inclusive) */
  endDate: string;
  /** Current status */
  status: PeriodStatus;
}
