"use client";

import { Search, LayoutGrid, Table, Plus } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import type { Department } from "@/lib/hr-types";

interface EmployeeToolbarProps {
  search: string;
  onSearchChange: (value: string) => void;
  statusFilter: string;
  onStatusFilterChange: (value: string) => void;
  departmentFilter: string;
  onDepartmentFilterChange: (value: string) => void;
  viewMode: "table" | "grid";
  onViewModeChange: (mode: "table" | "grid") => void;
  departments: Department[];
  onAddEmployee: () => void;
}

export function EmployeeToolbar({
  search,
  onSearchChange,
  statusFilter,
  onStatusFilterChange,
  departmentFilter,
  onDepartmentFilterChange,
  viewMode,
  onViewModeChange,
  departments,
  onAddEmployee,
}: EmployeeToolbarProps) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="relative flex-1 min-w-[200px] max-w-sm">
        <Search className="absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          placeholder="Search by name, NIK, or email..."
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          className="h-8 w-full rounded border-border bg-card pl-8 text-sm"
        />
      </div>

      <select
        value={statusFilter}
        onChange={(e) => onStatusFilterChange(e.target.value)}
        className="h-8 rounded border border-border bg-card px-2 text-xs text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
      >
        <option value="">All Status</option>
        <option value="ACTIVE">Active</option>
        <option value="ON_LEAVE">On Leave</option>
        <option value="TERMINATED">Terminated</option>
      </select>

      <select
        value={departmentFilter}
        onChange={(e) => onDepartmentFilterChange(e.target.value)}
        className="h-8 rounded border border-border bg-card px-2 text-xs text-foreground focus:border-ring focus:ring-1 focus:ring-ring cursor-pointer"
      >
        <option value="">All Departments</option>
        {departments.map((dept) => (
          <option key={dept.id} value={dept.id}>{dept.name}</option>
        ))}
      </select>

      <div className="flex items-center rounded-lg border border-border overflow-hidden">
        <button
          type="button"
          onClick={() => onViewModeChange("table")}
          className={`flex size-7 items-center justify-center cursor-pointer transition-colors ${
            viewMode === "table"
              ? "bg-muted text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
          title="Table view"
        >
          <Table className="size-3.5" />
        </button>
        <button
          type="button"
          onClick={() => onViewModeChange("grid")}
          className={`flex size-7 items-center justify-center cursor-pointer transition-colors ${
            viewMode === "grid"
              ? "bg-muted text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
          title="Grid view"
        >
          <LayoutGrid className="size-3.5" />
        </button>
      </div>

      <Button size="sm" onClick={onAddEmployee}>
        <Plus className="size-3.5" />
        Add Employee
      </Button>
    </div>
  );
}
