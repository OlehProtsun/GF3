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

export class RequestCanceledError extends Error {
  constructor(message = "Request was canceled.") {
    super(message);
    this.name = "RequestCanceledError";
  }
}

type RequestMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
type QueryValue = string | number | boolean | null | undefined;
type ResponseType = "json" | "blob";

type RequestOptions = {
  method?: RequestMethod;
  body?: unknown;
  headers?: HeadersInit;
  signal?: AbortSignal;
  query?: Record<string, QueryValue>;
  responseType?: ResponseType;
};

type ProblemDetailsResponse = {
  title?: string;
  detail?: string;
  status?: number;
  traceId?: string;
  errors?: Record<string, string[]>;
};

function hasErrorName(value: unknown): value is { name: string } {
  return typeof value === "object" && value !== null && "name" in value && typeof (value as { name: unknown }).name === "string";
}

export function isRequestCanceledError(error: unknown): boolean {
  if (error instanceof RequestCanceledError) {
    return true;
  }

  return hasErrorName(error) && error.name === "AbortError";
}

const defaultBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim() || "/api";

function withQueryString(url: string, query?: Record<string, QueryValue>) {
  if (!query) {
    return url;
  }

  const searchParams = new URLSearchParams();

  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === null) {
      return;
    }

    searchParams.set(key, String(value));
  });

  const queryString = searchParams.toString();
  if (!queryString) {
    return url;
  }

  return `${url}${url.includes("?") ? "&" : "?"}${queryString}`;
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  if (path.startsWith("http://") || path.startsWith("https://")) {
    return withQueryString(path, query);
  }

  const base = defaultBaseUrl.replace(/\/$/, "");
  const relativePath = path.replace(/^\//, "");
  return withQueryString(`${base}/${relativePath}`, query);
}

async function parseBody(response: Response, responseType: ResponseType): Promise<unknown> {
  if (responseType === "blob") {
    return response.blob();
  }

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
  const responseType = options.responseType ?? "json";

  const headers = new Headers(options.headers);
  headers.set("Accept", responseType === "blob" ? "*/*" : "application/json");
  if (hasBody && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  let response: Response;

  try {
    response = await fetch(buildUrl(path, options.query), {
      method,
      headers,
      signal: options.signal,
      body: hasBody ? JSON.stringify(options.body) : undefined,
    });
  } catch (error) {
    if (options.signal?.aborted || isRequestCanceledError(error)) {
      throw new RequestCanceledError();
    }

    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const payload = await parseBody(response, responseType);

  if (!response.ok) {
    throw toApiError(response.status, payload);
  }

  return payload as T;
}
