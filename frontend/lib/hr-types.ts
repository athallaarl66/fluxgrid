export type EmployeeStatus = "ACTIVE" | "ON_LEAVE" | "TERMINATED";

export interface Employee {
  id: string;
  employeeNo: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  jobTitle: string;
  departmentId: string | null;
  departmentName: string | null;
  managerId: string | null;
  managerName: string | null;
  status: EmployeeStatus;
  hireDate: string;
}

export interface EmployeeDetail extends Employee {
  address: string | null;
  dateOfBirth: string | null;
  nik: string | null;
  emergencyContact: string | null;
  baseSalary: number | null;
  bankName: string | null;
  bankAccount: string | null;
  taxId: string | null;
  terminationDate: string | null;
  tenantId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  address?: string;
  dateOfBirth?: string;
  nik?: string;
  emergencyContact?: string;
  departmentId?: string;
  managerId?: string;
  jobTitle: string;
  baseSalary?: number;
  bankName?: string;
  bankAccount?: string;
  taxId?: string;
  hireDate: string;
}

export interface UpdateEmployeeRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
  phone?: string;
  address?: string;
  dateOfBirth?: string;
  nik?: string;
  emergencyContact?: string;
  departmentId?: string;
  managerId?: string;
  jobTitle?: string;
  baseSalary?: number;
  bankName?: string;
  bankAccount?: string;
  taxId?: string;
}

export interface Department {
  id: string;
  name: string;
  parentId: string | null;
  parentName: string | null;
  isActive: boolean;
  tenantId: string;
  employeeCount?: number;
}

export interface CreateDepartmentRequest {
  name: string;
  parentId?: string;
}

export interface UpdateDepartmentRequest {
  name?: string;
  parentId?: string | null;
  isActive?: boolean;
}

export interface OrgChartNode {
  id: string;
  employeeNo: string;
  firstName: string;
  lastName: string;
  jobTitle: string;
  departmentId: string | null;
  departmentName: string | null;
  managerId: string | null;
  children: OrgChartNode[];
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export type PayrollRunStatus = "DRAFT" | "FINALIZED";

export interface PayrollRun {
  id: string;
  periodName: string;
  startDate: string;
  endDate: string;
  status: PayrollRunStatus;
  totalGross: number | null;
  totalNet: number | null;
  processedBy: string;
  tenantId: string;
  createdAt: string;
}

export interface PayrollRecord {
  id: string;
  runId: string;
  employeeId: string;
  employeeNo: string;
  employeeName: string;
  baseSalary: number | null;
  overtimePay: number | null;
  latenessDeduction: number | null;
  grossPay: number | null;
  taxDeduction: number | null;
  netPay: number | null;
  tenantId: string;
}

export interface PayrollRunDetail {
  run: PayrollRun;
  totalRecords: number;
  records: PayrollRecord[];
}

export interface CreatePayrollRequest {
  periodName: string;
  startDate: string;
  endDate: string;
}
