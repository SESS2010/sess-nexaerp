// Thin fetch wrapper. The API requires a JWT bearer token (permanent OIDC design);
// until the identity provider is wired, a development token can be obtained via the
// header sign-in box and is attached to every request.
//
// Wire contract (enforced globally by the API): PascalCase JSON properties, and
// error responses use the standard envelope
// { Type, Title, Status, Code, Detail, TraceId, Errors }.

const TOKEN_STORAGE_KEY = 'nexaerp.dev.bearerToken'

export function getStoredToken(): string {
  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY) ?? ''
  } catch {
    return ''
  }
}

export function setStoredToken(token: string): void {
  try {
    if (token) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token)
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY)
    }
  } catch {
    // storage unavailable; requests will simply go out unauthenticated
  }
}

const IDENTITY_STORAGE_KEY = 'nexaerp.dev.identity'

export function getStoredIdentity(): { employeeCode: string; organizationId: string } | null {
  try {
    const raw = localStorage.getItem(IDENTITY_STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

const COMPANY_STORAGE_KEY = 'nexaerp.dev.lastCompany'

/** Company chosen at the last sign-in; survives sign-out so the next sign-in lands in the same company. */
export function getLastCompany(): string {
  try {
    return localStorage.getItem(COMPANY_STORAGE_KEY) ?? ''
  } catch {
    return ''
  }
}

export function setLastCompany(organizationId: string): void {
  try {
    localStorage.setItem(COMPANY_STORAGE_KEY, organizationId)
  } catch {
    // storage unavailable; the user picks again next time
  }
}

export function setStoredIdentity(identity: { employeeCode: string; organizationId: string } | null): void {
  try {
    if (identity) {
      localStorage.setItem(IDENTITY_STORAGE_KEY, JSON.stringify(identity))
    } else {
      localStorage.removeItem(IDENTITY_STORAGE_KEY)
    }
  } catch {
    // storage unavailable; the top bar just won't show the name
  }
}

export class ApiError extends Error {
  readonly status: number
  readonly code?: string
  readonly traceId?: string

  constructor(status: number, message: string, code?: string, traceId?: string) {
    super(message)
    this.status = status
    this.code = code
    this.traceId = traceId
  }
}

interface StandardErrorEnvelope {
  Type?: string
  Title?: string
  Status?: number
  Code?: string
  Detail?: string
  TraceId?: string
  Errors?: Record<string, string[]>
  message?: string
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)
  headers.set('Accept', 'application/json')
  if (init?.body) {
    headers.set('Content-Type', 'application/json')
  }
  const token = getStoredToken()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(path, { ...init, headers })
  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`
    let code: string | undefined
    let traceId: string | undefined
    let detailFromServer = false
    try {
      const body = (await response.json()) as StandardErrorEnvelope
      if (body) {
        detailFromServer = Boolean(body.Detail || body.message)
        message = body.Detail || body.Title || body.message || message
        code = body.Code
        traceId = body.TraceId
        if (body.Errors && Object.keys(body.Errors).length > 0) {
          const details = Object.entries(body.Errors)
            .map(([field, errors]) => `${field}: ${errors.join('; ')}`)
            .join(' | ')
          if (details) message = `${message} — ${details}`
        }
      }
    } catch {
      // non-JSON error body; keep the status text
    }
    if (response.status === 401) {
      message = 'Not signed in, or the session expired. Please sign in again.'
      // Expired/invalid token (e.g. the API restarted): send the user back to
      // the login page instead of showing dead screens.
      setStoredToken('')
      setStoredIdentity(null)
      if (!window.location.pathname.startsWith('/login')) {
        window.location.assign('/login')
      }
    }
    if (response.status === 403 && !detailFromServer) {
      // Only when the server gave no reason. Since main 9e97fbf a refusal
      // names the awaited approver ("Creator self-approval is prohibited",
      // "awaiting SESS-01 (TECHNICAL_DIRECTOR)") and that text must reach the user.
      message = 'Permission denied for this page action.'
    }
    throw new ApiError(response.status, message, code, traceId)
  }

  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
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
