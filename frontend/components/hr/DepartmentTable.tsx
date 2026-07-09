"use client";

import type { Department } from "@/lib/hr-types";

interface DepartmentTableProps {
  departments: Department[];
  onEdit: (dept: Department) => void;
  onDelete: (dept: Department) => void;
}

type TreeNode = Department & { children: TreeNode[] };

function buildTree(depts: Department[]): (Department & { depth: number })[] {
  const map = new Map<string, TreeNode>();
  const roots: TreeNode[] = [];

  for (const d of depts) {
    map.set(d.id, { ...d, children: [] });
  }

  for (const d of depts) {
    const node = map.get(d.id)!;
    if (d.parentId && map.has(d.parentId)) {
      map.get(d.parentId)!.children.push(node);
    } else {
      roots.push(node);
    }
  }

  function flatten(nodes: TreeNode[], depth: number, result: (Department & { depth: number })[]) {
    for (const n of nodes) {
      result.push({ ...n, depth });
      flatten(n.children, depth + 1, result);
    }
    return result;
  }

  return flatten(roots, 0, []);
}

export function DepartmentTable({ departments, onEdit, onDelete }: DepartmentTableProps) {
  const tree = buildTree(departments);

  if (tree.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center">
        <p className="text-sm font-medium text-foreground">No departments</p>
        <p className="text-xs text-muted-foreground mt-1">Create your first department to get started</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b-2 border-[#9CAB84] bg-[#F6F0D7]">
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Department</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Employees</th>
            <th className="h-8 px-2 text-left text-[11px] font-semibold text-[#89986D]">Status</th>
            <th className="h-8 px-2 text-right text-[11px] font-semibold text-[#89986D]">Actions</th>
          </tr>
        </thead>
        <tbody>
          {tree.map((dept) => (
            <tr key={dept.id} className="border-b border-border hover:bg-muted/40">
              <td className="h-9 px-2 text-xs text-foreground" style={{ paddingLeft: `${12 + dept.depth * 20}px` }}>
                <span className="font-medium">{dept.name}</span>
                {dept.parentName && (
                  <span className="text-muted-foreground ml-1 text-[10px]">
                    — {dept.parentName}
                  </span>
                )}
              </td>
              <td className="h-9 px-2 text-xs text-muted-foreground tabular-nums">
                {dept.employeeCount ?? 0}
              </td>
              <td className="h-9 px-2">
                <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-medium ${
                  dept.isActive
                    ? "bg-emerald-50 text-emerald-700"
                    : "bg-muted text-muted-foreground"
                }`}>
                  {dept.isActive ? "Active" : "Inactive"}
                </span>
              </td>
              <td className="h-9 px-2 text-right">
                <div className="flex items-center justify-end gap-1">
                  <button
                    type="button"
                    onClick={() => onEdit(dept)}
                    className="h-7 rounded border border-border px-2 text-[11px] text-muted-foreground hover:text-foreground hover:bg-muted transition-colors cursor-pointer"
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    onClick={() => onDelete(dept)}
                    className="h-7 rounded border border-border px-2 text-[11px] text-destructive hover:bg-destructive/10 transition-colors cursor-pointer"
                  >
                    Delete
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
