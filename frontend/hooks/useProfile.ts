import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  roles: string[];
}

export function useProfile() {
  return useQuery<UserProfile>({
    queryKey: ["profile"],
    queryFn: () => apiClient<UserProfile>("/api/auth/profile"),
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { name: string; email: string }) =>
      apiClient<UserProfile>("/api/auth/profile", {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["profile"] });
    },
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: {
      username: string;
      oldPassword: string;
      newPassword: string;
      confirmNewPassword: string;
    }) =>
      apiClient<{ token: string; expiresAt: string }>(
        "/api/auth/change-password",
        {
          method: "POST",
          body: JSON.stringify(data),
        },
      ),
  });
}
