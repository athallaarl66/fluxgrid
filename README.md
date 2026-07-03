# FluxGrid ERP

> Modular Monolith ERP untuk industri berat: Mining, Oil & Gas, Logistics, Manufacturing.
> Dirancang sebagai Mini ERP (à la Odoo/SAP) dengan arsitektur production-grade.

**Author:** AI Engineer  
**Tech Stack:** .NET 8 (C#) + Next.js (TypeScript) + shadcn/ui + TanStack Query

---

## Quick Start

```bash
# Backend
cd backend
dotnet restore
dotnet run

# Frontend
cd frontend
npm install
npm run dev
```

Atau dengan Docker:

```bash
docker-compose up
```

- Frontend:
- Backend API:

---

## Monorepo Structure

```
fluxgrid/
├── frontend/          # Next.js App (TypeScript)
├── backend/           # .NET 8 Web API
├── docs/
│   └── features/      # PRD, TDD, API Contract
├── docker-compose.yml
└── README.md
```

---

## Modules

| Module             | Description                                           |
| ------------------ | ----------------------------------------------------- |
| **WMS**            | Warehouse Management — inbound/outbound, stock ledger |
| **Finance**        | General Ledger — double-entry, P&L, balance sheet     |
| **HR**             | Employee data, attendance, payroll, recruitment AI    |
| **Task & Project** | Kanban board, time tracking, dependency graph         |

Dokumentasi detail: [`docs/features/`](./docs/features/)

- [PRD — Product Requirements](./docs/features/PRD.md)
- [TDD — Technical Design](./docs/features/TECHNICAL.md)
- [API Contract](./docs/features/API-CONTRACT.md)

---

_FluxGrid ERP v2.0 — Mini ERP for industries where data integrity isn't optional._
