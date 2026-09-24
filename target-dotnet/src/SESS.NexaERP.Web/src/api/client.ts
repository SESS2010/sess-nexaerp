// Thin fetch wrapper. Every call to the API goes through authorizedFetch, which
// attaches the in-memory OIDC access token and the selected company header,
// refreshes once on 401, and turns an error response into an ApiError that
// keeps the whole standard envelope.
//
// Wire contract (enforced globally by the API): PascalCase JSON properties, and
// error responses use the standard envelope
// { Type, Title, Status, Code, Detail, TraceId, Errors }.
// Authentication contract: target-dotnet/docs/installation/server-frontend-oidc-contract.md

import { getAccessToken, getCompany, isSignedIn, refreshAccessToken, requestSignal } from '../auth/authSession'

export const COMPANY_HEADER = 'X-NexaERP-Company'

export interface StandardErrorEnvelope {
  Type?: string
  Title?: string
  Status?: number
  Code?: string
  Detail?: string
  TraceId?: string
  Errors?: Record<string, string[]>
  message?: string
  /** Endpoint-specific extras (e.g. AdministratorActionRequired) survive here. */
  [extra: string]: unknown
}

export class ApiError extends Error {
  readonly status: number
  readonly code?: string
  readonly traceId?: string
  readonly title?: string
  readonly type?: string
  readonly errors?: Record<string, string[]>
  /** The error body exactly as the server sent it, when it was JSON. */
  readonly envelope?: StandardErrorEnvelope

  constructor(status: number, message: string, code?: string, traceId?: string, envelope?: StandardErrorEnvelope) {
    super(message)
    this.status = status
    this.code = code
    this.traceId = traceId
    this.envelope = envelope
    this.title = envelope?.Title
    this.type = envelope?.Type
    this.errors = envelope?.Errors
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  let message = `${response.status} ${response.statusText}`
  let envelope: StandardErrorEnvelope | undefined
  try {
    envelope = (await response.json()) as StandardErrorEnvelope
  } catch {
    // non-JSON error body; keep the status text
  }
  if (envelope) {
    message = envelope.Detail || envelope.Title || envelope.message || message
    if (envelope.Errors && Object.keys(envelope.Errors).length > 0) {
      const details = Object.entries(envelope.Errors)
        .map(([field, errors]) => `${field}: ${errors.join('; ')}`)
        .join(' | ')
      if (details) message = `${message} — ${details}`
    }
  }
  return new ApiError(response.status, message, envelope?.Code, envelope?.TraceId, envelope)
}

/** Tokens go only to this ERP origin: a relative path, never an absolute or protocol-relative URL. */
function isErpPath(path: string): boolean {
  return path.startsWith('/') && !path.startsWith('//')
}

function isSafeMethod(method: string): boolean {
  return method === 'GET' || method === 'HEAD'
}

function sessionExpired(): never {
  // The session is already cleared; RequireAuth sends the user to sign-in.
  throw new ApiError(401, 'Your sign-in has expired. Please sign in again.', 'AUTHENTICATION_REQUIRED')
}

/**
 * fetch() for ERP API paths. Adds Authorization (access token only) and
 * X-NexaERP-Company, and cancels with the session when the user signs out or
 * switches company. Resolves only for a 2xx response; otherwise throws ApiError.
 *
 * On 401 the token is refreshed once. A safe read is then retried once. A
 * write is never replayed silently: the user is told to check whether it took
 * effect, and retrying reuses the caller's own idempotency key.
 */
export async function authorizedFetch(path: string, init?: RequestInit): Promise<Response> {
  if (!isErpPath(path)) throw new Error(`Refusing to send ERP credentials to ${path}.`)
  const method = (init?.method ?? 'GET').toUpperCase()

  const send = (): Promise<Response> => {
    const headers = new Headers(init?.headers)
    const token = getAccessToken()
    if (token) headers.set('Authorization', `Bearer ${token}`)
    const company = getCompany()
    if (company) headers.set(COMPANY_HEADER, company)
    return fetch(path, { ...init, headers, signal: init?.signal ?? requestSignal() })
  }

  let response = await send()
  if (response.status === 401 && isSignedIn()) {
    const refreshed = await refreshAccessToken()
    if (!refreshed) sessionExpired()
    if (isSafeMethod(method)) {
      response = await send()
    } else {
      throw new ApiError(
        401,
        'Your sign-in had expired and has now been renewed. This action may not have been carried out: check the record before trying again.',
        'AUTHENTICATION_REQUIRED',
      )
    }
  }
  if (!response.ok) throw await toApiError(response)
  return response
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  headers.set('Accept', 'application/json')
  if (typeof init?.body === 'string') {
    headers.set('Content-Type', 'application/json')
  }
  const response = await authorizedFetch(path, { ...init, headers })
  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}

/** Saves a downloaded response as a file, preferring the server's Content-Disposition name. */
export async function saveResponseAsFile(response: Response, fallbackName: string): Promise<void> {
  const disposition = response.headers.get('content-disposition') ?? ''
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)/i.exec(disposition)
  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = match?.[1] ? decodeURIComponent(match[1]) : fallbackName
  anchor.click()
  URL.revokeObjectURL(url)
}

/**
 * Normalise a list response to the paged envelope regardless of what the server
 * actually sent. A list page must never call .map on a value straight off the
 * wire: a bare array, a missing Items, or a renamed field all return 200 and
 * would otherwise white-screen the render (EmployeeListPage, 12 Sep). Here the
 * wrong shape degrades to an empty page instead.
 */
export function toPagedResponse<T>(data: unknown): PagedResponse<T> {
  if (Array.isArray(data)) {
    return { TotalCount: data.length, PageNumber: 1, PageSize: data.length, Items: data as T[] }
  }
  if (data && typeof data === 'object') {
    const envelope = data as Partial<PagedResponse<T>>
    const items = Array.isArray(envelope.Items) ? envelope.Items : []
    return {
      TotalCount: typeof envelope.TotalCount === 'number' ? envelope.TotalCount : items.length,
      PageNumber: typeof envelope.PageNumber === 'number' ? envelope.PageNumber : 1,
      PageSize: typeof envelope.PageSize === 'number' ? envelope.PageSize : items.length,
      Items: items,
    }
  }
  return { TotalCount: 0, PageNumber: 1, PageSize: 0, Items: [] }
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  /** GET a list endpoint; the result is always a well-formed PagedResponse. */
  getPaged: async <T>(path: string): Promise<PagedResponse<T>> => toPagedResponse<T>(await request<unknown>(path)),
  // `headers` carries per-call requirements such as the Stores Idempotency-Key.
  post: <T>(path: string, body: unknown, headers?: Record<string, string>) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body), headers }),
  put: <T>(path: string, body: unknown, headers?: Record<string, string>) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body), headers }),
}

export interface PagedResponse<T> {
  TotalCount: number
  PageNumber: number
  PageSize: number
  Items: T[]
}
