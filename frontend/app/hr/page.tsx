import { Users, GitBranch, DollarSign, FileText } from "lucide-react";
import Link from "next/link";

const LINKS = [
  { href: "/hr/employees", label: "Employees", icon: Users, desc: "Manage employee records" },
  { href: "/hr/recruitment", label: "Recruitment", icon: FileText, desc: "Candidate CV upload & management" },
  { href: "/hr/org-chart", label: "Org Chart", icon: GitBranch, desc: "Organizational structure" },
  { href: "/hr/payroll", label: "Payroll", icon: DollarSign, desc: "Payroll processing & reports" },
];

export default function HRPage() {
  return (
    <div className="p-5 space-y-5 animate-fade-in">
      <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">Human Resources</h1>
      <p className="text-sm text-muted-foreground">Select a module to get started</p>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {LINKS.map((l) => (
          <Link
            key={l.href}
            href={l.href}
            className="flex flex-col gap-3 rounded-xl border border-border bg-card p-5 hover:bg-muted/40 transition-colors"
          >
            <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
              <l.icon className="size-5 text-accent-foreground" />
            </div>
            <div>
              <p className="text-sm font-semibold text-foreground">{l.label}</p>
              <p className="text-xs text-muted-foreground mt-0.5">{l.desc}</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
