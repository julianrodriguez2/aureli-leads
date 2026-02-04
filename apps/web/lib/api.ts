const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export class ApiError extends Error {
  status: number;
  code: string;
  traceId?: string;
  details?: unknown;

  constructor(status: number, message: string, code = "error", traceId?: string, details?: unknown) {
    super(message);
    this.status = status;
    this.code = code;
    this.traceId = traceId;
    this.details = details;
  }
}

type ApiFetchOptions = RequestInit & {
  parseAs?: "json" | "text";
};

type ApiErrorPayload = {
  error?: {
    code?: string;
    message?: string;
    details?: unknown;
    traceId?: string;
  };
};

export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const { parseAs = "json", ...init } = options;
  const headers = new Headers(init.headers);
  const hasBody = init.body !== undefined && init.body !== null;

  if (hasBody && !headers.has("Content-Type") && !(init.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers,
    credentials: "include",
    cache: "no-store"
  });

  if (!response.ok) {
    let message = `API error: ${response.status}`;
    let code = "error";
    let traceId = response.headers.get("X-Correlation-Id") ?? undefined;
    let details: unknown;

    try {
      const text = await response.text();
      if (text) {
        const parsed = JSON.parse(text) as ApiErrorPayload;
        if (parsed?.error) {
          code = parsed.error.code ?? code;
          message = parsed.error.message ?? message;
          details = parsed.error.details;
          traceId = parsed.error.traceId ?? traceId;
        }
      }
    } catch {
      // ignore parse errors
    }

    throw new ApiError(response.status, message, code, traceId, details);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  if (parseAs === "text") {
    return (await response.text()) as T;
  }

  return (await response.json()) as T;
}
