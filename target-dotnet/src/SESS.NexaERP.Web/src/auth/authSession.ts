// The signed-in OIDC session, held in memory only.
//
// Contract rules this module implements (server-frontend-oidc-contract.md):
// - authorization code + S256 PKCE through oidc-client-ts, full-page redirect;
// - state and nonce generated and validated, callback bound to the realm and
//   client that started it;
// - access/refresh/ID tokens never touch localStorage, sessionStorage, URLs or
//   logs. Only the library's short-lived transaction record (state, nonce, PKCE
//   verifier) and the name of the realm that started the redirect use
//   sessionStorage, and both are deleted when the callback completes or fails;
// - a page reload starts a new sign-in (Keycloak's own session may satisfy it);
// - refresh before expiry, single-flight, never through a silent iframe;
// - logout through end_session_endpoint with id_token_hint, the exact
//   post-logout URI and a validated state; local data is cleared first, even
//   when Keycloak cannot be reached.
//
// This lives outside React so the API client can read the token and company
// header without a hook.

import { InMemoryWebStorage, UserManager, WebStorageStateStore } from 'oidc-client-ts'
import type { User } from 'oidc-client-ts'
import {
  OIDC_SCOPES,
  REALMS,
  postLogoutRedirectUri,
  redirectUri,
  safeReturnPath,
} from './oidcConfig'
import type { CompanyCode, RealmConfig, RealmKey } from './oidcConfig'

export interface AuthSnapshot {
  realm: RealmKey | null
  /** Seconds since epoch; null when signed out. */
  expiresAt: number | null
  /** OIDC subject, for display only. The server's session/me is the authority. */
  subject: string | null
  company: CompanyCode | null
  /** Changes on every sign-in and company switch; the workspace is keyed on it. */
  epoch: number
  /** Why the last session ended, for the login page to explain. */
  notice: AuthNotice | null
}

export type AuthNotice =
  | { kind: 'expired' }
  | { kind: 'signed-out' }
  | { kind: 'logout-incomplete'; detail: string }
  | { kind: 'callback-failed'; detail: string }

const PENDING_SIGNIN_KEY = 'nexaerp.oidc.pendingSignIn'
const PENDING_SIGNOUT_KEY = 'nexaerp.oidc.pendingSignOut'
/** Refresh this many seconds before the access token expires. */
const REFRESH_LEAD_SECONDS = 60

let user: User | null = null
let realm: RealmKey | null = null
let company: CompanyCode | null = null
let epoch = 0
let notice: AuthNotice | null = null
let refreshTimer: number | undefined
let refreshInFlight: Promise<boolean> | null = null
let requestController = new AbortController()

const managers = new Map<RealmKey, UserManager>()
const listeners = new Set<() => void>()
let snapshot: AuthSnapshot = buildSnapshot()

function buildSnapshot(): AuthSnapshot {
  return {
    realm,
    expiresAt: user?.expires_at ?? null,
    subject: user?.profile.sub ?? null,
    company,
    epoch,
    notice,
  }
}

function emit(): void {
  snapshot = buildSnapshot()
  listeners.forEach((listener) => listener())
}

export function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function getSnapshot(): AuthSnapshot {
  return snapshot
}

function managerFor(key: RealmKey): UserManager {
  let manager = managers.get(key)
  if (!manager) {
    const config = REALMS[key]
    manager = new UserManager({
      authority: config.authority,
      client_id: config.clientId,
      redirect_uri: redirectUri(),
      post_logout_redirect_uri: postLogoutRedirectUri(),
      response_type: 'code',
      scope: OIDC_SCOPES,
      // Tokens in memory only. The transaction record (state/nonce/verifier)
      // stays in the library's default sessionStorage store for the redirect.
      userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
      automaticSilentRenew: false,
      monitorSession: false,
      loadUserInfo: false,
      filterProtocolClaims: false,
    })
    managers.set(key, manager)
  }
  return manager
}

function randomToken(): string {
  const bytes = new Uint8Array(32)
  crypto.getRandomValues(bytes)
  return Array.from(bytes, (value) => value.toString(16).padStart(2, '0')).join('')
}

function removeSessionItem(key: string): void {
  try {
    sessionStorage.removeItem(key)
  } catch {
    // storage unavailable; nothing was written either
  }
}

/** Cancels every in-flight API request and starts a fresh signal for the next ones. */
function abortRequests(): void {
  requestController.abort()
  requestController = new AbortController()
}

export function requestSignal(): AbortSignal {
  return requestController.signal
}

/** Drops tokens, company and pending requests. Used by sign-out, expiry and realm switch. */
function clearLocalSession(nextNotice: AuthNotice | null): void {
  window.clearTimeout(refreshTimer)
  refreshTimer = undefined
  refreshInFlight = null
  abortRequests()
  user = null
  realm = null
  company = null
  epoch += 1
  notice = nextNotice
  emit()
}

export function clearNotice(): void {
  if (!notice) return
  notice = null
  emit()
}

// --- Sign-in ---------------------------------------------------------------

/** Full-page redirect to the chosen realm. Clears any previous identity first. */
export async function beginSignIn(key: RealmKey, returnTo: string): Promise<void> {
  clearLocalSession(null)
  const manager = managerFor(key)
  await manager.clearStaleState()
  sessionStorage.setItem(PENDING_SIGNIN_KEY, key)
  try {
    await manager.signinRedirect({
      nonce: randomToken(),
      state: { returnTo: safeReturnPath(returnTo) },
    })
  } catch (error) {
    removeSessionItem(PENDING_SIGNIN_KEY)
    throw error
  }
}

function assertIssuedBy(config: RealmConfig, signedIn: User): void {
  const issuer = signedIn.profile.iss
  if (issuer !== config.authority) {
    throw new Error(
      `The sign-in came back from "${issuer}", but this page expects "${config.authority}". The identity server's issuer setting does not match the ERP configuration.`,
    )
  }
  const audience = signedIn.profile.aud
  const audiences = Array.isArray(audience) ? audience : [audience]
  if (!audiences.includes(config.clientId)) {
    throw new Error('The sign-in response was issued for a different application.')
  }
}

/**
 * Completes the redirect. The code and state are removed from the address bar
 * before anything else happens. Returns the relative path to continue to.
 */
export async function completeSignIn(): Promise<string> {
  const callbackUrl = window.location.href
  window.history.replaceState(null, '', window.location.pathname)

  let pending: string | null = null
  try {
    pending = sessionStorage.getItem(PENDING_SIGNIN_KEY)
  } catch {
    pending = null
  }
  removeSessionItem(PENDING_SIGNIN_KEY)
  if (pending !== 'staff' && pending !== 'approvers') {
    throw new Error('This sign-in was not started from this browser tab, or it has already been used. Start again.')
  }

  const config = REALMS[pending]
  const manager = managerFor(pending)
  try {
    // The library rejects a state it did not create, a state created for a
    // different authority or client, and an ID token whose nonce differs.
    const signedIn = await manager.signinCallback(callbackUrl)
    if (!signedIn) throw new Error('The identity server returned no session.')
    assertIssuedBy(config, signedIn)
    if (!signedIn.access_token) throw new Error('The identity server returned no access token.')

    user = signedIn
    realm = pending
    company = null
    epoch += 1
    notice = null
    scheduleRefresh()
    emit()

    const state = signedIn.state as { returnTo?: unknown } | undefined
    return safeReturnPath(state?.returnTo)
  } catch (error) {
    await manager.removeUser().catch(() => undefined)
    clearLocalSession(null)
    throw error
  } finally {
    await manager.clearStaleState().catch(() => undefined)
  }
}

// --- Tokens ----------------------------------------------------------------

/** The ACCESS token only. ID and refresh tokens never leave this module. */
export function getAccessToken(): string | null {
  if (!user || user.expired) return null
  return user.access_token
}

export function isSignedIn(): boolean {
  return user !== null && realm !== null
}

function scheduleRefresh(): void {
  window.clearTimeout(refreshTimer)
  if (!user?.expires_in) return
  const delayMs = Math.max(5, user.expires_in - REFRESH_LEAD_SECONDS) * 1000
  refreshTimer = window.setTimeout(() => {
    void refreshAccessToken()
  }, delayMs)
}

/**
 * Refreshes at the realm's token endpoint. Concurrent callers share one
 * request. On failure the session is cleared and the login page explains it
 * expired. Returns whether a usable access token now exists.
 */
export function refreshAccessToken(): Promise<boolean> {
  if (refreshInFlight) return refreshInFlight
  const key = realm
  const current = user
  if (!key || !current?.refresh_token) {
    if (current) clearLocalSession({ kind: 'expired' })
    return Promise.resolve(false)
  }
  const startedEpoch = epoch
  refreshInFlight = (async () => {
    try {
      // With a refresh token present, signinSilent uses the refresh_token grant
      // and never opens an iframe (no silent_redirect_uri is configured).
      const renewed = await managerFor(key).signinSilent()
      if (epoch !== startedEpoch) return false
      if (!renewed?.access_token) throw new Error('No access token in the refresh response.')
      assertIssuedBy(REALMS[key], renewed)
      user = renewed
      scheduleRefresh()
      emit()
      return true
    } catch {
      if (epoch === startedEpoch) clearLocalSession({ kind: 'expired' })
      return false
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

// --- Company ---------------------------------------------------------------

export function getCompany(): CompanyCode | null {
  return company
}

/**
 * Selects or switches the company. Pending requests are cancelled and the
 * epoch changes, which remounts the workspace so every company-scoped cache
 * is dropped and session/me is fetched again under the new header.
 */
export function selectCompany(next: CompanyCode): void {
  if (!user) return
  abortRequests()
  company = next
  epoch += 1
  emit()
}

// --- Sign-out --------------------------------------------------------------

async function providerReachable(config: RealmConfig): Promise<boolean> {
  const controller = new AbortController()
  const timer = window.setTimeout(() => controller.abort(), 4000)
  try {
    const response = await fetch(`${config.authority}/.well-known/openid-configuration`, {
      signal: controller.signal,
      credentials: 'omit',
    })
    return response.ok
  } catch {
    return false
  } finally {
    window.clearTimeout(timer)
  }
}

/**
 * Clears everything local first, then ends the Keycloak session. When the
 * provider cannot be reached the local sign-out still stands, and the login
 * page says the provider session may still be open.
 */
export async function signOut(): Promise<void> {
  const key = realm
  const idToken = user?.id_token
  if (key) await managerFor(key).removeUser().catch(() => undefined)
  clearLocalSession({ kind: 'signed-out' })
  if (!key || !idToken) {
    window.location.assign('/login')
    return
  }

  const config = REALMS[key]
  if (!(await providerReachable(config))) {
    notice = {
      kind: 'logout-incomplete',
      detail: `You are signed out of the ERP on this computer, but the ${config.label} sign-in server could not be reached, so its session may still be open. Close the browser before leaving this computer.`,
    }
    emit()
    return
  }

  try {
    sessionStorage.setItem(PENDING_SIGNOUT_KEY, key)
    await managerFor(key).signoutRedirect({
      id_token_hint: idToken,
      post_logout_redirect_uri: postLogoutRedirectUri(),
      state: { signedOutAt: Date.now() },
    })
  } catch (error) {
    removeSessionItem(PENDING_SIGNOUT_KEY)
    notice = {
      kind: 'logout-incomplete',
      detail: `You are signed out of the ERP on this computer, but the ${config.label} sign-in server did not accept the sign-out (${error instanceof Error ? error.message : 'unknown error'}). Close the browser before leaving this computer.`,
    }
    emit()
  }
}

/** Validates the state Keycloak returns after logout. */
export async function completeSignOut(): Promise<void> {
  const callbackUrl = window.location.href
  window.history.replaceState(null, '', window.location.pathname)
  let pending: string | null = null
  try {
    pending = sessionStorage.getItem(PENDING_SIGNOUT_KEY)
  } catch {
    pending = null
  }
  removeSessionItem(PENDING_SIGNOUT_KEY)
  if (pending !== 'staff' && pending !== 'approvers') {
    throw new Error('This sign-out was not started from this browser tab.')
  }
  const manager = managerFor(pending)
  try {
    await manager.signoutCallback(callbackUrl)
  } finally {
    await manager.clearStaleState().catch(() => undefined)
  }
}

export function reportCallbackFailure(detail: string): void {
  notice = { kind: 'callback-failed', detail }
  emit()
}

export function reportLogoutIncomplete(detail: string): void {
  notice = { kind: 'logout-incomplete', detail }
  emit()
}
