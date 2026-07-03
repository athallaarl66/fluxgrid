# Technical Specifications: Task Dependency Management (TP-3)

## 1. System Architecture
- **Frontend**: Visual dependency graph rendered using a specialized library (e.g., `reactflow` or `d3.js`).
- **Backend**: Next.js API Routes processing topological sort and Critical Path Method (CPM) calculations.
- **Database**: PostgreSQL (Neon) with Drizzle ORM representing dependencies as a Directed Acyclic Graph (DAG) in a relational format.
- **Event Bus**: MediatR pattern to trigger unblocking when a predecessor completes.

## 2. Database Schema

### Table: `task_dependencies`
| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `predecessor_id` | UUID | NOT NULL, FK | Task that must finish first |
| `successor_id` | UUID | NOT NULL, FK | Task that is blocked |
| `tenant_id` | UUID | NOT NULL, FK | Multi-tenancy isolation |
| `created_at` | TIMESTAMP | DEFAULT NOW() | |

*(Additional columns like `estimated_hours` added to the `tasks` table to calculate critical path).*

## 3. Drizzle ORM Schema Snippet
```typescript
import { pgTable, uuid, timestamp, uniqueIndex } from "drizzle-orm/pg-core";
import { tasks } from "./tasks";

export const taskDependencies = pgTable("task_dependencies", {
  id: uuid("id").primaryKey().defaultRandom(),
  predecessorId: uuid("predecessor_id").references(() => tasks.id).notNull(),
  successorId: uuid("successor_id").references(() => tasks.id).notNull(),
  tenantId: uuid("tenant_id").notNull(),
  createdAt: timestamp("created_at").defaultNow().notNull(),
}, (table) => {
  return {
    uniqueDependency: uniqueIndex("unique_dependency_idx").on(table.predecessorId, table.successorId)
  }
});
```

## 4. Graph Algorithm & Critical Path Method
- **Cycle Detection**: Use Kahn's algorithm or DFS (Depth First Search) upon adding a dependency. If a cycle is detected, the API returns a 400 Bad Request.
- **Critical Path**: Calculate the longest path from start tasks (no predecessors) to end tasks (no successors) using the `estimated_hours` (or duration) of each task.

## 5. API Endpoints

### POST `/api/v1/task/tasks/{id}/dependencies`
- **Description**: Add a new predecessor to a task.
- **Request Body**:
```json
{
  "predecessorId": "uuid"
}
```
- **Response**: 201 Created or 400 (if circular dependency detected).

### DELETE `/api/v1/task/tasks/{id}/dependencies/{predecessorId}`
- **Description**: Remove a dependency.

### GET `/api/v1/task/projects/{id}/dependency-graph`
- **Description**: Returns all tasks and their edges, including critical path flags.
- **Response**:
```json
{
  "nodes": [{ "id": "uuid", "title": "Task 1", "isCritical": true }],
  "edges": [{ "source": "uuid", "target": "uuid" }]
}
```

## 6. Domain Events
- **Raised**:
  - `DependencyAdded` (Predecessor, Successor)
  - `DependencyRemoved`
  - `TaskBlocked`
  - `TaskUnblocked`
- **Consumed**:
  - `TaskStatusChanged` (if status becomes 'Done', check if it unblocks successors).

## 7. Permissions (RBAC)
- `task.manage_dependencies`: Required to add/remove dependencies.
- `project.view`: Required to view the graph.

## 8. Performance Considerations
- **Graph Caching**: For large projects, cache the calculated critical path and graph structure in Redis.
- **Memoization**: On the frontend, memoize graph node renders to prevent lag when panning/zooming.

## 9. Security Considerations
- Ensure a user cannot create a dependency between tasks belonging to different tenants or projects.

## 10. Error Handling Strategy
- Return explicit error messages for circular dependencies: `"Cannot add dependency: This would create a circular loop (Task A -> Task B -> Task A)."`

## 11. Seed Data Examples
```sql
INSERT INTO task_dependencies (predecessor_id, successor_id, tenant_id) VALUES 
('task-1', 'task-2', 'tenant-1'), -- Task 1 blocks Task 2
('task-2', 'task-3', 'tenant-1');
```

## 12. Deployment Considerations
- Ensure `reactflow` (or chosen graph UI library) is properly bundled.
