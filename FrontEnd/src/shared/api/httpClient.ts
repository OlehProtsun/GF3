export type ValidationErrors = Record<string, string[]>;

export class ApiError extends Error {
  public readonly status: number;
  public readonly details?: unknown;
  public readonly traceId?: string;
  public readonly validationErrors?: ValidationErrors;

  constructor(params: {
    message: string;
    status: number;
    details?: unknown;
    traceId?: string;
    validationErrors?: ValidationErrors;
  }) {
    super(params.message);
    this.name = "ApiError";
    this.status = params.status;
    this.details = params.details;
    this.traceId = params.traceId;
    this.validationErrors = params.validationErrors;
  }
}

type RequestMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

type RequestOptions = {
  method?: RequestMethod;
  body?: unknown;
  headers?: HeadersInit;
  signal?: AbortSignal;
};

type ProblemDetailsResponse = {
  title?: string;
  detail?: string;
  status?: number;
  traceId?: string;
  errors?: Record<string, string[]>;
};

const defaultBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim() || "/api";

function buildUrl(path: string): string {
  if (path.startsWith("http://") || path.startsWith("https://")) {
    return path;
  }

  if (defaultBaseUrl.startsWith("http://") || defaultBaseUrl.startsWith("https://")) {
    return `${defaultBaseUrl.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
  }

  return `${defaultBaseUrl.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
}

async function parseBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json") || contentType.includes("+json")) {
    try {
      return await response.json();
    } catch {
      return undefined;
    }
  }

  const text = await response.text();
  return text.length ? text : undefined;
}

function toApiError(status: number, payload: unknown): ApiError {
  const problem = (payload ?? {}) as ProblemDetailsResponse;
  const fallbackMessage = `Request failed with status ${status}`;

  return new ApiError({
    status,
    message: problem.detail ?? problem.title ?? fallbackMessage,
    details: payload,
    traceId: problem.traceId,
    validationErrors: problem.errors,
  });
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const method = options.method ?? "GET";
  const hasBody = options.body !== undefined;

  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");
  if (hasBody && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(buildUrl(path), {
    method,
    headers,
    signal: options.signal,
    body: hasBody ? JSON.stringify(options.body) : undefined,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const payload = await parseBody(response);

  if (!response.ok) {
    throw toApiError(response.status, payload);
  }

  return payload as T;
}
