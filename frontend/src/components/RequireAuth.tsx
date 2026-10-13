import { type ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { getToken } from '../api/client';
import { useAuth } from '../context/AuthContext';

export default function RequireAuth({ children }: { children: ReactNode }) {
  if (!getToken()) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export function RequirePermission({
  permission,
  children,
}: {
  permission: string;
  children: ReactNode;
}) {
  const { can } = useAuth();
  if (!can(permission)) return <Navigate to="/" replace />;
  return <>{children}</>;
}