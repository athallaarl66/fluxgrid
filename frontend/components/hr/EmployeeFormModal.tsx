"use client";

import { useState } from "react";
import { X } from "lucide-react";
import { useEmployeeList, useCreateEmployee, useUpdateEmployee } from "@/hooks/useEmployees";
import { useDepartmentList } from "@/hooks/useDepartments";
import { useToast } from "@/components/ui/toast";
import { Button } from "@/components/ui/button";
import type { EmployeeDetail, CreateEmployeeRequest, UpdateEmployeeRequest } from "@/lib/hr-types";

interface EmployeeFormModalProps {
  onClose: () => void;
  employee?: EmployeeDetail;
}

export function EmployeeFormModal({ onClose, employee }: EmployeeFormModalProps) {
  const { toast } = useToast();
  const { data: deptData } = useDepartmentList();
  const { data: empData } = useEmployeeList({ pageSize: 100 });
  const createMutation = useCreateEmployee();
  const updateMutation = useUpdateEmployee(employee?.id || "");
  const isEditing = !!employee;

  const [form, setForm] = useState({
    firstName: employee?.firstName || "",
    lastName: employee?.lastName || "",
    email: employee?.email || "",
    phone: employee?.phone || "",
    address: employee?.address || "",
    dateOfBirth: employee?.dateOfBirth || "",
    nik: employee?.nik || "",
    emergencyContact: employee?.emergencyContact || "",
    departmentId: employee?.departmentId || "",
    managerId: employee?.managerId || "",
    jobTitle: employee?.jobTitle || "",
    baseSalary: employee?.baseSalary ? String(employee.baseSalary) : "",
    bankName: employee?.bankName || "",
    bankAccount: employee?.bankAccount || "",
    taxId: employee?.taxId || "",
    hireDate: employee?.hireDate?.split("T")[0] || new Date().toISOString().split("T")[0],
  });

  const [managerSearch, setManagerSearch] = useState("");
  const [showManagerDropdown, setShowManagerDropdown] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const departments = deptData || [];
  const employees = empData?.items || [];

  const filteredManagers = employees.filter(
    (e) => {
      const name = `${e.firstName} ${e.lastName}`.toLowerCase();
      return name.includes(managerSearch.toLowerCase()) && e.id !== employee?.id;
    }
  );

  const selectedManagerName = form.managerId
    ? employees.find((e) => e.id === form.managerId)
    : null;

  function setField(key: string, value: string) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);

    try {
      const body: CreateEmployeeRequest | UpdateEmployeeRequest = {
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        phone: form.phone || undefined,
        address: form.address || undefined,
        dateOfBirth: form.dateOfBirth || undefined,
        nik: form.nik || undefined,
        emergencyContact: form.emergencyContact || undefined,
        departmentId: form.departmentId || undefined,
        managerId: form.managerId || undefined,
        jobTitle: form.jobTitle,
        baseSalary: form.baseSalary ? Number(form.baseSalary) : undefined,
        bankName: form.bankName || undefined,
        bankAccount: form.bankAccount || undefined,
        taxId: form.taxId || undefined,
      };

      if (isEditing) {
        await updateMutation.mutateAsync(body as UpdateEmployeeRequest);
        toast("Employee updated", "success");
      } else {
        await createMutation.mutateAsync({
          ...body,
          hireDate: form.hireDate,
        } as CreateEmployeeRequest);
        toast("Employee created", "success");
      }

      onClose();
    } catch {
      toast(isEditing ? "Failed to update employee" : "Failed to create employee", "error");
    } finally {
      setSubmitting(false);
    }
  }

  const fields = [
    { key: "firstName", label: "First Name", type: "text", required: true, colSpan: 1 },
    { key: "lastName", label: "Last Name", type: "text", required: true, colSpan: 1 },
    { key: "email", label: "Email", type: "email", required: true, colSpan: 1 },
    { key: "phone", label: "Phone", type: "tel", colSpan: 1 },
    { key: "nik", label: "NIK (National ID)", type: "text", colSpan: 1 },
    { key: "dateOfBirth", label: "Date of Birth", type: "date", colSpan: 1 },
    { key: "jobTitle", label: "Job Title", type: "text", required: true, colSpan: 1 },
    { key: "baseSalary", label: "Base Salary (Rp)", type: "number", colSpan: 1 },
    { key: "bankName", label: "Bank Name", type: "text", colSpan: 1 },
    { key: "bankAccount", label: "Bank Account", type: "text", colSpan: 1 },
    { key: "taxId", label: "Tax ID (NPWP)", type: "text", colSpan: 1 },
    { key: "address", label: "Address", type: "text", colSpan: 2 },
    { key: "emergencyContact", label: "Emergency Contact", type: "text", colSpan: 2 },
  ];

  if (!isEditing) {
    fields.splice(7, 0, { key: "hireDate", label: "Hire Date", type: "date", required: true, colSpan: 1 });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="relative w-full max-w-2xl max-h-[90vh] overflow-y-auto rounded-xl border border-border bg-card p-6 shadow-lg">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-foreground">
            {isEditing ? "Edit Employee" : "Add Employee"}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="flex size-7 items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer"
          >
            <X className="size-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {fields.map((f) => (
              <div key={f.key} className={f.colSpan === 2 ? "sm:col-span-2" : ""}>
                <label className="text-xs text-muted-foreground">{f.label}</label>
                <input
                  type={f.type}
                  value={(form as Record<string, string>)[f.key]}
                  onChange={(e) => setField(f.key, e.target.value)}
                  required={f.required}
                  className="mt-1 h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                />
              </div>
            ))}

            <div>
              <label className="text-xs text-muted-foreground">Department</label>
              <select
                value={form.departmentId}
                onChange={(e) => setField("departmentId", e.target.value)}
                className="mt-1 h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 cursor-pointer"
              >
                <option value="">No Department</option>
                {departments.map((dept) => (
                  <option key={dept.id} value={dept.id}>{dept.name}</option>
                ))}
              </select>
            </div>

            <div className="relative">
              <label className="text-xs text-muted-foreground">Manager</label>
              <input
                type="text"
                value={selectedManagerName ? `${selectedManagerName.firstName} ${selectedManagerName.lastName}` : managerSearch}
                onChange={(e) => {
                  setManagerSearch(e.target.value);
                  setField("managerId", "");
                  setShowManagerDropdown(true);
                }}
                onFocus={() => setShowManagerDropdown(true)}
                onBlur={() => setTimeout(() => setShowManagerDropdown(false), 200)}
                placeholder="Search manager..."
                className="mt-1 h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
              />
              {showManagerDropdown && filteredManagers.length > 0 && (
                <div className="absolute z-10 mt-1 w-full max-h-40 overflow-y-auto rounded-lg border border-border bg-popover shadow-lg">
                  {filteredManagers.slice(0, 10).map((emp) => (
                    <button
                      key={emp.id}
                      type="button"
                      onMouseDown={() => {
                        setField("managerId", emp.id);
                        setManagerSearch(`${emp.firstName} ${emp.lastName}`);
                        setShowManagerDropdown(false);
                      }}
                      className="w-full px-3 py-1.5 text-left text-sm text-foreground hover:bg-muted transition-colors cursor-pointer"
                    >
                      {emp.firstName} {emp.lastName} — {emp.jobTitle}
                    </button>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button type="button" variant="outline" size="sm" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={submitting}>
              {submitting ? "Saving..." : isEditing ? "Update Employee" : "Create Employee"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
