export type ValidationErrors = Record<string, string[]>;
type ValidationErrorPayload = ValidationErrors | string[];

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

export type DownloadedFile = {
  blob: Blob;
  fileName: string | null;
  contentType: string | null;
};

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
  errors?: ValidationErrorPayload;
};

function isFormDataBody(value: unknown): value is FormData {
  return typeof FormData !== "undefined" && value instanceof FormData;
}

function isBlobBody(value: unknown): value is Blob {
  return typeof Blob !== "undefined" && value instanceof Blob;
}

function isUrlSearchParamsBody(value: unknown): value is URLSearchParams {
  return typeof URLSearchParams !== "undefined" && value instanceof URLSearchParams;
}

function isBinaryBody(value: unknown): value is BodyInit {
  return value instanceof ArrayBuffer || ArrayBuffer.isView(value);
}

function buildRequestBody(body: unknown, headers: Headers): BodyInit | undefined {
  if (body === undefined) {
    return undefined;
  }

  if (isFormDataBody(body)) {
    headers.delete("Content-Type");
    return body;
  }

  if (typeof body === "string") {
    if (!headers.has("Content-Type")) {
      headers.set("Content-Type", "text/plain;charset=UTF-8");
    }

    return body;
  }

  if (isBlobBody(body) || isUrlSearchParamsBody(body) || isBinaryBody(body)) {
    return body;
  }

  if (!headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  return JSON.stringify(body);
}

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
let authAccessToken: string | null = null;

export function setAuthAccessToken(token: string | null) {
  authAccessToken = token && token.trim().length > 0 ? token.trim() : null;
}

export function getAuthAccessToken() {
  return authAccessToken;
}

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

export function buildApiUrl(path: string, query?: Record<string, QueryValue>) {
  return buildUrl(path, query);
}

function parseContentDispositionFileName(contentDisposition: string | null): string | null {
  if (!contentDisposition) {
    return null;
  }

  const utf8Match = contentDisposition.match(/filename\*\s*=\s*UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    const encodedValue = utf8Match[1].trim();

    try {
      return decodeURIComponent(encodedValue);
    } catch {
      return encodedValue;
    }
  }

  const asciiMatch = contentDisposition.match(/filename\s*=\s*(?:"([^"]+)"|([^;]+))/i);
  const rawValue = asciiMatch?.[1] ?? asciiMatch?.[2];

  return rawValue ? rawValue.trim().replace(/^["']|["']$/g, "") : null;
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

function normalizeValidationErrors(errors: ValidationErrorPayload | undefined): ValidationErrors | undefined {
  if (!errors) {
    return undefined;
  }

  if (Array.isArray(errors)) {
    const messages = errors.filter((message): message is string => typeof message === "string" && message.trim().length > 0);
    return messages.length > 0 ? { general: messages } : undefined;
  }

  const entries = Object.entries(errors)
    .map(([field, messages]) => [
      field,
      messages.filter((message): message is string => typeof message === "string" && message.trim().length > 0),
    ] as const)
    .filter(([, messages]) => messages.length > 0);

  return entries.length > 0 ? Object.fromEntries(entries) : undefined;
}

function getFirstValidationErrorMessage(validationErrors?: ValidationErrors): string | undefined {
  if (!validationErrors) {
    return undefined;
  }

  return Object.values(validationErrors)
    .flat()
    .find((message) => message.trim().length > 0);
}

function toApiError(status: number, payload: unknown): ApiError {
  const problem = (payload ?? {}) as ProblemDetailsResponse;
  const validationErrors = normalizeValidationErrors(problem.errors);
  const textPayload = typeof payload === "string" ? payload.trim() : "";
  const fallbackMessage = `Request failed with status ${status}`;
  const message =
    problem.detail?.trim() ||
    getFirstValidationErrorMessage(validationErrors) ||
    problem.title?.trim() ||
    textPayload ||
    fallbackMessage;

  return new ApiError({
    status,
    message,
    details: payload,
    traceId: problem.traceId,
    validationErrors,
  });
}

export function getErrorMessage(error: unknown, fallbackMessage = "Something went wrong."): string {
  if (error instanceof ApiError) {
    return getFirstValidationErrorMessage(error.validationErrors) ?? error.message ?? fallbackMessage;
  }

  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return fallbackMessage;
}

async function executeRequest(path: string, options: RequestOptions = {}): Promise<Response> {
  const method = options.method ?? "GET";
  const hasBody = options.body !== undefined;
  const responseType = options.responseType ?? "json";

  const headers = new Headers(options.headers);
  headers.set("Accept", responseType === "blob" ? "*/*" : "application/json");

  if (authAccessToken && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${authAccessToken}`);
  }

  const requestBody = hasBody ? buildRequestBody(options.body, headers) : undefined;

  let response: Response;

  try {
    response = await fetch(buildUrl(path, options.query), {
      method,
      headers,
      signal: options.signal,
      body: requestBody,
    });
  } catch (error) {
    if (options.signal?.aborted || isRequestCanceledError(error)) {
      throw new RequestCanceledError();
    }

    throw error;
  }

  return response;
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const response = await executeRequest(path, options);

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    const errorPayload = await parseBody(response, "json");
    throw toApiError(response.status, errorPayload);
  }

  const responseType = options.responseType ?? "json";
  const payload = await parseBody(response, responseType);
  return payload as T;
}

export async function requestFile(path: string, options: Omit<RequestOptions, "responseType"> = {}): Promise<DownloadedFile> {
  const response = await executeRequest(path, { ...options, responseType: "blob" });

  if (response.status === 204) {
    return {
      blob: new Blob(),
      fileName: null,
      contentType: response.headers.get("content-type"),
    };
  }

  if (!response.ok) {
    const errorPayload = await parseBody(response, "json");
    throw toApiError(response.status, errorPayload);
  }

  return {
    blob: await response.blob(),
    fileName: parseContentDispositionFileName(response.headers.get("content-disposition")),
    contentType: response.headers.get("content-type"),
  };
}
