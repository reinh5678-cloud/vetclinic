import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  api,
  clearSession,
  getStoredPermissions,
  getToken,
  getUser,
  refreshPermissions,
  setSession,
} from '../api/client';

export interface User {
  id: number;
  username: string;
  fullName: string;
  email: string;
  role: string;
}

interface AuthContextValue {
  user: User | null;
  permissions: string[];
  can: (permission: string) => boolean;
  login: (username: string, password: string) => Promise<void>;
  loginByPin: (pin: string) => Promise<void>;
  logout: () => void;
  updateUser: (user: User) => void;
  loading: boolean;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => getUser<User>());
  const [permissions, setPermissions] = useState<string[]>(() => getStoredPermissions());
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (getToken()) {
      refreshPermissions()
        .then((perms) => setPermissions(perms))
        .catch(() => setPermissions([]));
    }
    setLoading(false);
  }, []);

  const login = useCallback(async (username: string, password: string) => {
    const res = await api<{ token: string; user: User; permissions: string[] }>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
    setSession(res.token, res.user, res.permissions);
    setUser(res.user);
    setPermissions(res.permissions);
  }, []);

  const loginByPin = useCallback(async (pin: string) => {
    const res = await api<{ token: string; user: User; permissions: string[] }>('/auth/login-pin', {
      method: 'POST',
      body: JSON.stringify({ pin }),
    });
    setSession(res.token, res.user, res.permissions);
    setUser(res.user);
    setPermissions(res.permissions);
  }, []);

  const logout = useCallback(() => {
    clearSession();
    setUser(null);
    setPermissions([]);
  }, []);

  const updateUser = useCallback(
    (next: User) => {
      const token = getToken();
      if (token) setSession(token, next, permissions);
      setUser(next);
    },
    [permissions],
  );

  const can = useCallback(
    (permission: string) => permissions.includes(permission),
    [permissions],
  );

  const value = useMemo(
    () => ({ user, permissions, can, login, loginByPin, logout, updateUser, loading }),
    [user, permissions, can, login, loginByPin, logout, updateUser, loading],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth debe usarse dentro de AuthProvider');
  return ctx;
}