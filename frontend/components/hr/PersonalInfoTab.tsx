"use client";

import { useState } from "react";
import { useAuth } from "@/lib/auth-context";
import type { EmployeeDetail } from "@/lib/hr-types";
import { useUpdateEmployee } from "@/hooks/useEmployees";
import { useToast } from "@/components/ui/toast";
import { Button } from "@/components/ui/button";

interface PersonalInfoTabProps {
  employee: EmployeeDetail;
}

export function PersonalInfoTab({ employee }: PersonalInfoTabProps) {
  const { user } = useAuth();
  const { toast } = useToast();
  const updateMutation = useUpdateEmployee(employee.id);
  const canEdit = user?.roles?.includes("HR:EmployeeManage");

  const [form, setForm] = useState({
    firstName: employee.firstName,
    lastName: employee.lastName,
    nik: employee.nik || "",
    phone: employee.phone || "",
    email: employee.email,
    address: employee.address || "",
    dateOfBirth: employee.dateOfBirth || "",
    emergencyContact: employee.emergencyContact || "",
  });

  const [editing, setEditing] = useState(false);

  async function handleSave() {
    try {
      await updateMutation.mutateAsync(form);
      toast("Personal info updated", "success");
      setEditing(false);
    } catch {
      toast("Failed to update personal info", "error");
    }
  }

  const fields: { key: string; label: string; type: string; required?: boolean }[] = [
    { key: "firstName", label: "First Name", type: "text", required: true },
    { key: "lastName", label: "Last Name", type: "text", required: true },
    { key: "nik", label: "NIK (National ID)", type: "text" },
    { key: "phone", label: "Phone", type: "tel" },
    { key: "email", label: "Email", type: "email", required: true },
    { key: "address", label: "Address", type: "text" },
    { key: "dateOfBirth", label: "Date of Birth", type: "date" },
    { key: "emergencyContact", label: "Emergency Contact", type: "text" },
  ];

  if (!editing) {
    return (
      <div className="space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {fields.map((f) => (
            <div key={f.key}>
              <p className="text-xs text-muted-foreground">{f.label}</p>
              <p className="text-sm text-foreground mt-0.5">
                {form[f.key as keyof typeof form] || "—"}
              </p>
            </div>
          ))}
        </div>
        {canEdit && (
          <Button size="sm" variant="outline" onClick={() => setEditing(true)}>
            Edit Personal Info
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {fields.map((f) => (
          <div key={f.key}>
            <label className="text-xs text-muted-foreground">{f.label}</label>
            <input
              type={f.type}
              value={form[f.key as keyof typeof form]}
              onChange={(e) => setForm((prev) => ({ ...prev, [f.key]: e.target.value }))}
              required={f.required}
              className="mt-1 h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
            />
          </div>
        ))}
      </div>
      <div className="flex items-center gap-2">
        <Button size="sm" onClick={handleSave} disabled={updateMutation.isPending}>
          {updateMutation.isPending ? "Saving..." : "Save"}
        </Button>
        <Button size="sm" variant="outline" onClick={() => setEditing(false)}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
