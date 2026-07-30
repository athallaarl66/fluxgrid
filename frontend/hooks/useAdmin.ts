import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";

export interface AdminUser {
  id: string;
  name: string;
  email: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
}

export interface AdminRole {
  id: string;
  name: string;
  description: string | null;
  permissions: string[];
  userCount: number;
}

export interface PermissionInfo {
  permission: string;
  module: string;
  description: string;
}

export function useAdminUsers(params: {
  search?: string;
  role?: string;
  page?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ["admin-users", params],
    queryFn: () => {
      const q = new URLSearchParams();
      if (params.search) q.set("search", params.search);
      if (params.role) q.set("role", params.role);
      if (params.page) q.set("page", String(params.page));
      if (params.pageSize) q.set("pageSize", String(params.pageSize));
      return apiClient<{ users: AdminUser[]; total: number; page: number; pageSize: number }>(
        `/api/admin/users?${q.toString()}`,
      );
    },
  });
}

export function useCreateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { name: string; email: string; password: string; role?: string }) =>
      apiClient("/api/admin/users", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-users"] }),
  });
}

export function useUpdateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { id: string; name: string; email: string; password?: string; role?: string }) =>
      apiClient(`/api/admin/users/${data.id}`, {
        method: "PUT",
        body: JSON.stringify({ name: data.name, email: data.email, password: data.password, role: data.role }),
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-users"] }),
  });
}

export function useDeleteUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient(`/api/admin/users/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-users"] }),
  });
}

export function useAdminRoles() {
  return useQuery<AdminRole[]>({
    queryKey: ["admin-roles"],
    queryFn: () => apiClient<AdminRole[]>("/api/admin/roles"),
  });
}

export function useCreateRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { name: string; description?: string; permissions?: string[] }) =>
      apiClient("/api/admin/roles", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-roles"] }),
  });
}

export function useUpdateRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { id: string; name: string; description?: string; permissions?: string[] }) =>
      apiClient(`/api/admin/roles/${data.id}`, {
        method: "PUT",
        body: JSON.stringify({ name: data.name, description: data.description, permissions: data.permissions }),
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-roles"] }),
  });
}

export function useDeleteRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient(`/api/admin/roles/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-roles"] }),
  });
}

export function usePermissions() {
  return useQuery<PermissionInfo[]>({
    queryKey: ["admin-permissions"],
    queryFn: () => apiClient<PermissionInfo[]>("/api/admin/permissions"),
  });
}
