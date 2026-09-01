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
    try {
      const body = (await response.json()) as StandardErrorEnvelope
      if (body) {
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
    if (response.status === 403) {
      message = 'Permission denied for this page action.'
    }
    throw new ApiError(response.status, message, code, traceId)
  }

  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}

export const api = {
  get: <T>(path: string) => request<T>(path),
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
