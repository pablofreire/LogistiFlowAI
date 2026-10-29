/** Error envelope every LogistiFlow endpoint returns on failure. */
export interface ApiErrorBody {
  code: string;
  message: string;
  details?: Record<string, string[]> | null;
}

export interface ApiEnvelope<T> {
  success: boolean;
  data: T | null;
  error: ApiErrorBody | null;
  traceId: string | null;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly details: Record<string, string[]> | null;
  readonly traceId: string | null;

  constructor(status: number, body: ApiErrorBody | null, traceId: string | null) {
    super(body?.message ?? `Request failed with status ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.code = body?.code ?? 'http.error';
    this.details = body?.details ?? null;
    this.traceId = traceId;
  }
}
