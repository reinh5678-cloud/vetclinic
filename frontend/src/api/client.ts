const TOKEN_KEY = 'vetclinic_token';
const USER_KEY = 'vetclinic_user';
const PERMISSIONS_KEY = 'vetclinic_permissions';

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setSession(token: string, user: unknown, permissions: string[] = []): void {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
  localStorage.setItem(PERMISSIONS_KEY, JSON.stringify(permissions));
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  localStorage.removeItem(PERMISSIONS_KEY);
}

export function getUser<T>(): T | null {
  const raw = localStorage.getItem(USER_KEY);
  return raw ? (JSON.parse(raw) as T) : null;
}

export function getStoredPermissions(): string[] {
  const raw = localStorage.getItem(PERMISSIONS_KEY);
  return raw ? (JSON.parse(raw) as string[]) : [];
}

export function setStoredPermissions(permissions: string[]): void {
  localStorage.setItem(PERMISSIONS_KEY, JSON.stringify(permissions));
}

export async function refreshPermissions(): Promise<string[]> {
  const data = await api<{ permissions: string[] }>('/auth/permissions');
  setStoredPermissions(data.permissions);
  return data.permissions;
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const isFormData = options.body instanceof FormData;
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>),
  };
  if (!isFormData) headers['Content-Type'] = 'application/json';
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(`/api${path}`, { ...options, headers });
  const isLoginRequest = path.startsWith('/auth/login');
  if (res.status === 401 && !isLoginRequest) {
    clearSession();
    window.location.href = '/login';
    throw new Error('Sesión expirada');
  }
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message ?? `Error ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}