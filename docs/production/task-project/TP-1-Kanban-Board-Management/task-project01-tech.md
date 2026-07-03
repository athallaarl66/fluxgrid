# Technical Specifications: Kanban Board Management (TP-1)

## 1. System Architecture
- **Frontend**: Next.js 15 Server Components for initial load, Client Components for drag-and-drop interactions (using a library like `@hello-pangea/dnd` or `dnd-kit`).
- **Backend**: API Routes in Next.js implementing Clean Architecture.
- **Database**: PostgreSQL (Neon) with Drizzle ORM.
- **Caching**: Upstash Redis for caching project board state to reduce database queries on load.
- **Event Bus**: MediatR pattern for Domain Events to broadcast task status changes to other modules.

## 2. Database Schema

### Table: `projects`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `name` | VARCHAR(255) | NOT NULL | Project name |
| `description` | TEXT | | Project description |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | |

### Table: `task_columns`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `project_id` | UUID | NOT NULL, FK | Reference to `projects` |
| `name` | VARCHAR(100) | NOT NULL | Column name (e.g., Todo, Doing) |
| `position` | INTEGER | NOT NULL | Ordering index |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |

### Table: `tasks`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `project_id` | UUID | NOT NULL, FK | Reference to `projects` |
| `column_id` | UUID | NOT NULL, FK | Current status column |
| `title` | VARCHAR(255) | NOT NULL | Task title |
| `description` | TEXT | | Task description |
| `assignee_id` | UUID | FK | Reference to `users` |
| `priority` | VARCHAR(50) | NOT NULL | Enum: LOW, MEDIUM, HIGH |
| `position` | INTEGER | NOT NULL | Ordering index within column |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | |

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, varchar, text, integer, timestamp } from "drizzle-orm/pg-core";

export const projects = pgTable("projects", {
  id: uuid("id").primaryKey().defaultRandom(),
  name: varchar("name", { length: 255 }).notNull(),
  description: text("description"),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
  updatedAt: timestamp("updated_at").defaultNow().notNull(),
});

export const taskColumns = pgTable("task_columns", {
  id: uuid("id").primaryKey().defaultRandom(),
  projectId: uuid("project_id").references(() => projects.id).notNull(),
  name: varchar("name", { length: 100 }).notNull(),
  position: integer("position").notNull(),
  tenantId: uuid("tenant_id").notNull(),
});

export const tasks = pgTable("tasks", {
  id: uuid("id").primaryKey().defaultRandom(),
  projectId: uuid("project_id").references(() => projects.id).notNull(),
  columnId: uuid("column_id").references(() => taskColumns.id).notNull(),
  title: varchar("title", { length: 255 }).notNull(),
  description: text("description"),
  assigneeId: uuid("assignee_id"), // Refers to external users table
  priority: varchar("priority", { length: 50 }).notNull().default('MEDIUM'),
  position: integer("position").notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
  updatedAt: timestamp("updated_at").defaultNow().notNull(),
});
```

## 4. API Endpoints

### GET `/api/v1/task/projects/{id}/kanban`
- **Description**: Fetch all columns and tasks for a project.
- **Response**:
```json
{
  "projectId": "uuid",
  "columns": [
    {
      "id": "uuid",
      "name": "To Do",
      "tasks": [
        {
          "id": "uuid",
          "title": "Task 1",
          "priority": "HIGH",
          "assigneeId": "uuid"
        }
      ]
    }
  ]
}
```

### PUT `/api/v1/task/tasks/{id}/status`
- **Description**: Update task's column and position (drag-and-drop).
- **Request Body**:
```json
{
  "targetColumnId": "uuid",
  "newPosition": 1
}
```

## 5. Domain Events
- **Raised**:
  - `TaskCreated` (Task ID, Project ID, Assignee ID)
  - `TaskStatusChanged` (Task ID, Old Column, New Column)
  - `TaskAssigned` (Task ID, Assignee ID)
- **Consumed**:
  - `EmployeeTerminated` (HR) -> Unassign from tasks.

## 6. Permissions (RBAC)
- `project.view`: View the kanban board.
- `task.create`: Create new tasks.
- `task.update`: Move tasks, change assignees, edit details.
- `task.delete`: Delete tasks.
- `project.manage`: Create/edit columns.

## 7. Performance Considerations
- Use optimistic UI updates for drag-and-drop to make the interface feel instantaneous.
- Background sync to the database to ensure state consistency without blocking the UI.
- Use `position` field as a float or with gaps (e.g., 1000, 2000) to avoid re-indexing all tasks during reordering.

## 8. Security Considerations
- **Row-Level Security (RLS)**: Enforce tenant isolation so a tenant cannot fetch tasks from another project/tenant.
- **Input Validation**: Sanitize task titles and descriptions to prevent XSS.

## 9. Error Handling Strategy
- If drag-and-drop API call fails, revert the optimistic UI update and show a toast notification.
- Use standard HTTP status codes (400 for bad position/column, 403 for unauthorized).

## 10. Seed Data Examples
```sql
INSERT INTO projects (id, name, tenant_id) VALUES ('proj-1', 'ERP Implementation', 'tenant-1');
INSERT INTO task_columns (id, project_id, name, position, tenant_id) VALUES 
('col-1', 'proj-1', 'To Do', 1, 'tenant-1'),
('col-2', 'proj-1', 'In Progress', 2, 'tenant-1');
INSERT INTO tasks (id, project_id, column_id, title, priority, position, tenant_id) VALUES 
('task-1', 'proj-1', 'col-1', 'Setup Database', 'HIGH', 1, 'tenant-1');
```

## 11. Deployment Considerations
- Requires database migration for new tables.
- No special infrastructure required beyond the standard Next.js + Neon DB setup.
