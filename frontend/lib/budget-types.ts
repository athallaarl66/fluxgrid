export interface BudgetResponse {
  id: string;
  accountId: string;
  accountCode: string;
  accountName: string;
  periodId: string;
  periodName: string;
  plannedAmount: number;
  notes: string | null;
  tenantId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBudgetRequest {
  accountId: string;
  periodId: string;
  plannedAmount: number;
  notes?: string;
}

export interface UpdateBudgetRequest {
  plannedAmount?: number;
  notes?: string;
}

export interface BudgetVsActualRow {
  accountCode: string;
  accountName: string;
  plannedAmount: number;
  actualAmount: number;
  variance: number;
  variancePercentage: number;
  isFlagged: boolean;
}

export interface PaginatedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
