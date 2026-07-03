export type AccountType = "ASSET" | "LIABILITY" | "EQUITY" | "REVENUE" | "EXPENSE";

export interface AccountResponse {
  id: string;
  code: string;
  name: string;
  parentId: string | null;
  type: AccountType;
  isActive: boolean;
  children: AccountResponse[];
}

export interface CreateAccountRequest {
  code: string;
  name: string;
  parentId?: string | null;
  type?: string;
  isActive?: boolean;
}

export interface UpdateAccountRequest {
  code?: string;
  name?: string;
  parentId?: string | null;
  type?: string;
  isActive?: boolean;
}

export const ACCOUNT_TYPES: { value: AccountType; label: string }[] = [
  { value: "ASSET", label: "Asset" },
  { value: "LIABILITY", label: "Liability" },
  { value: "EQUITY", label: "Equity" },
  { value: "REVENUE", label: "Revenue" },
  { value: "EXPENSE", label: "Expense" },
];

export function flattenTree(accounts: AccountResponse[], level = 0, parentPath = ""): FlatAccount[] {
  return accounts.flatMap((acc) => {
    const path = parentPath ? `${parentPath} > ${acc.name}` : acc.name;
    const flat: FlatAccount = { ...acc, level, path };
    const children = acc.children
      ? flattenTree(acc.children, level + 1, path)
      : [];
    return [flat, ...children];
  });
}

export interface FlatAccount extends AccountResponse {
  level: number;
  path: string;
}

export interface AccountOption {
  id: string;
  code: string;
  name: string;
  type: AccountType;
}
