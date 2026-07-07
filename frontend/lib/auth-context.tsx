"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
} from "react";

interface User {
  id: string;
  email: string;
  name: string;
  roles: string[];
}

interface LoginResult {
  ok: boolean;
  error?: string;
  passwordChangeRequired?: boolean;
}

interface AuthContextType {
  user: User | null;
  loading: boolean;
  passwordChangeRequired: boolean;
  login: (username: string, password: string) => Promise<LoginResult>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType>({
  user: null,
  loading: true,
  passwordChangeRequired: false,
  login: async () => ({ ok: false }),
  logout: async () => {},
  refreshUser: async () => {},
});

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [passwordChangeRequired, setPasswordChangeRequired] = useState(false);

  const fetchUser = useCallback(async () => {
    try {
      const res = await fetch("/api/auth/me");
      if (res.ok) {
        const data = await res.json();
        setUser(data.user);
        return;
      }
    } catch {
      // not authenticated
    }
    setUser(null);
  }, []);

  useEffect(() => {
    fetchUser().finally(() => setLoading(false));
  }, [fetchUser]);

  const login = useCallback(async (username: string, password: string): Promise<LoginResult> => {
    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      const data = await response.json();

      if (data.password_change_required) {
        setPasswordChangeRequired(true);
        return { ok: true, passwordChangeRequired: true };
      }

      if (!response.ok) {
        return { ok: false, error: data.message || "Invalid credentials" };
      }

      if (data.token) {
        document.cookie = `token=${data.token}; path=/; max-age=${60 * 60 * 24}; SameSite=Lax`;
      }

      await fetchUser();
      setPasswordChangeRequired(false);
      return { ok: true };
    } catch {
      return { ok: false, error: "Connection error. Please try again." };
    }
  }, [fetchUser]);

  const logout = useCallback(async () => {
    document.cookie = "token=; path=/; max-age=0";
    setUser(null);
    setPasswordChangeRequired(false);
  }, []);

  return (
    <AuthContext.Provider value={{ user, loading, passwordChangeRequired, login, logout, refreshUser: fetchUser }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
