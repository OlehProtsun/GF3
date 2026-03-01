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

type Primitive = string | number | boolean;
type QueryParams = Record<string, Primitive | Primitive[] | null | undefined>;

type ResponseType = "json" | "text" | "blob";

export type RequestOptions = {
  method?: RequestMethod;
  body?: unknown;
  headers?: HeadersInit;
  signal?: AbortSignal;
  query?: QueryParams;
  responseType?: ResponseType;
  credentials?: RequestCredentials;
};

type ProblemDetailsResponse = {
  title?: string;
  detail?: string;
  status?: number;
  traceId?: string;
  errors?: Record<string, string[]>;
};

const defaultBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim() || "/api";

function getAccessToken(): string | null {
  return localStorage.getItem("access_token");
}

function withQuery(path: string, query?: QueryParams): string {
  if (!query) return path;

  const searchParams = new URLSearchParams();
  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === null) return;

    if (Array.isArray(value)) {
      value.forEach((item) => searchParams.append(key, String(item)));
      return;
    }

    searchParams.set(key, String(value));
  });

  const queryString = searchParams.toString();
  if (!queryString) return path;
  return `${path}${path.includes("?") ? "&" : "?"}${queryString}`;
}

function buildUrl(path: string): string {
  if (path.startsWith("http://") || path.startsWith("https://")) {
    return path;
  }

  return `${defaultBaseUrl.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
}

async function parseResponse(response: Response, responseType: ResponseType): Promise<unknown> {
  if (responseType === "blob") return response.blob();
  if (responseType === "text") return response.text();

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
  return new ApiError({
    status,
    message: problem.detail ?? problem.title ?? `Request failed with status ${status}`,
    details: payload,
    traceId: problem.traceId,
    validationErrors: problem.errors,
  });
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const method = options.method ?? "GET";
  const hasBody = options.body !== undefined;
  const responseType = options.responseType ?? "json";

  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  const token = getAccessToken();
  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  let body: BodyInit | undefined;
  if (hasBody) {
    if (options.body instanceof FormData || options.body instanceof Blob || typeof options.body === "string") {
      body = options.body as BodyInit;
    } else {
      if (!headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
      }
      body = JSON.stringify(options.body);
    }
  }

  const response = await fetch(buildUrl(withQuery(path, options.query)), {
    method,
    headers,
    body,
    signal: options.signal,
    credentials: options.credentials,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const payload = await parseResponse(response, responseType);
  if (!response.ok) {
    throw toApiError(response.status, payload);
  }

  return payload as T;
}
