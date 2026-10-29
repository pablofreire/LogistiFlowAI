import { notifySessionExpired, readSession } from '../auth/session';
import { ApiError, type ApiEnvelope } from './envelope';

export interface RequestOptions {
  method?: 'GET' | 'POST';
  body?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
}

export interface ApiResult<T> {
  data: T;
  status: number;
  headers: Headers;
}

/**
 * Typed fetch over the gateway. Adds the bearer token, unwraps the ApiResponse envelope and turns failures
 * into ApiError. A 401 on an authenticated call ends the session.
 */
export async function request<T>(path: string, options: RequestOptions = {}): Promise<ApiResult<T>> {
  const session = readSession();
  const headers: Record<string, string> = { Accept: 'application/json', ...options.headers };
  if (session) {
    headers.Authorization = `Bearer ${session.accessToken}`;
  }

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(path, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
  });

  const envelope = (await response.json().catch(() => null)) as ApiEnvelope<T> | null;
  if (!response.ok || !envelope?.success) {
    if (response.status === 401 && session) {
      notifySessionExpired();
    }

    throw new ApiError(response.status, envelope?.error ?? null, envelope?.traceId ?? null);
  }

  return { data: envelope.data as T, status: response.status, headers: response.headers };
}

export function toQueryString(parameters: Record<string, string | number | boolean | null | undefined>): string {
  const search = new URLSearchParams();
  Object.entries(parameters).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') {
      search.set(key, String(value));
    }
  });

  const text = search.toString();
  return text ? `?${text}` : '';
}
