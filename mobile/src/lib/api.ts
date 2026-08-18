// API client cho mobile — base URL từ env EXPO_PUBLIC_API_URL (dev: backend .NET :5080)
const API_URL = process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:5080";

export interface ApiEnvelope<T> {
  success: boolean;
  data: T;
  message?: string | null;
  errors?: Record<string, string[]> | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    headers: { "Content-Type": "application/json", ...(init.headers ?? {}) },
    ...init,
  });
  const json = (await res.json()) as ApiEnvelope<T>;
  if (!res.ok || !json.success) {
    throw new Error(json.message ?? `Lỗi HTTP ${res.status}`);
  }
  return json.data;
}

export const apiGet = <T>(path: string, token?: string | null) =>
  request<T>(path, { headers: token ? { Authorization: `Bearer ${token}` } : undefined });

export function apiPost<T>(path: string, body: unknown, token?: string | null) {
  return request<T>(path, {
    method: "POST",
    body: JSON.stringify(body),
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
}

export function apiPut<T>(path: string, body: unknown, token?: string | null) {
  return request<T>(path, {
    method: "PUT",
    body: JSON.stringify(body),
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
}
