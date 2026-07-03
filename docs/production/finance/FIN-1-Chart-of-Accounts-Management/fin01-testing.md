# Testing Scenarios: Chart of Accounts Management (FIN-1)

## 1. Test Strategy Overview
Testing for COA focuses heavily on hierarchy validation, structural integrity (preventing deletion of active accounts), and strict uniqueness constraints per tenant.

## 2. Test Cases

### TC-01: Create Valid Account (Happy Path)
- **Given** a CFO navigating to the COA settings
- **When** they create a new account "1110 - Cash in Bank" under parent "1100 - Current Assets" with type "Asset"
- **Then** the account is saved successfully.
- **And** it appears correctly in the hierarchical tree view.

### TC-02: Prevent Duplicate Account Code (Negative Testing)
- **Given** an existing account with code "1110"
- **When** a user attempts to create a new account with code "1110" in the same tenant
- **Then** the system throws a validation error: "Account code must be unique".

### TC-03: Type Mismatch Prevention (Negative Testing)
- **Given** a parent account of type "Asset"
- **When** a user attempts to create a child account under it, but selects type "Expense"
- **Then** the system rejects the creation, enforcing that child accounts must inherit or match the parent's core type.

### TC-04: Prevent Deletion of Used Account (Negative Testing)
- **Given** an account "1110" that has at least one Journal Entry associated with it
- **When** the CFO attempts to delete the account
- **Then** the system blocks the deletion with error: "Cannot delete account with transaction history. Deactivate it instead."

### TC-05: Deactivation Status Propagation
- **Given** a parent account with 3 child accounts
- **When** the CFO deactivates the parent account
- **Then** the system should either block the action OR prompt the user that all 3 child accounts will also be deactivated.

### TC-06: Prevent Circular Hierarchy
- **Given** Account A is the parent of Account B
- **When** a user attempts to edit Account A and set its parent to Account B
- **Then** the system detects the cycle and rejects the update.

### TC-07: Audit Trail Verification
- **Given** the CFO changes the name of account "1110" from "Bank BCA" to "Bank BCA Main"
- **When** the transaction commits
- **Then** an entry is created in `audit_logs` capturing the old_value, new_value, and the actor's ID.

## 3. Performance Testing
- Loading the full COA tree (potentially 1,000+ accounts) must render in under 1 second. Ensure the API returns a flat list and the frontend builds the tree, or use recursive CTEs in PostgreSQL.

## 4. Security & Access Testing
- Role `Finance:Admin` can create/edit/delete.
- Role `Finance:Read` can only view the COA list.
- Cross-tenant testing: Tenant A cannot see or query Tenant B's COA.
