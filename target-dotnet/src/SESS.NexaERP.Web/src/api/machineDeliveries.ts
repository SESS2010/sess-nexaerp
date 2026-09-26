import { ApiError, api, authorizedFetch, saveResponseAsFile } from './client'
import type { PagedResponse } from './client'
import { getCompany } from '../auth/authSession'
import type {
  DispatchMachineRequest,
  MachineDeliveryJobOrderCandidate,
  MachineDeliveryView,
  SignMachineDeliveryRequest,
  SignatureContentType,
} from '../types/machineDelivery'

// MachineDeliveryEndpoints.cs. Every route sits on stores.machine-deliveries
// except the evidence download, which is reports.machine-dossier:View.
// Both writes carry IdempotencyKey in the body (not a header).
const BASE = '/api/v1/stores/machine-deliveries'

export function newIdempotencyKey(prefix: string): string {
  return `${prefix}-${crypto.randomUUID()}`
}

export const JOB_PAGE_SIZE_MAX = 200
export const JOB_SEARCH_MAX = 200

/** GET /job-orders — stores.machine-deliveries:Issue. pageSize 1-200, search at most 200 characters. */
export function listDispatchableJobOrders(query: {
  page: number
  pageSize: number
  search?: string
}): Promise<PagedResponse<MachineDeliveryJobOrderCandidate>> {
  const params = new URLSearchParams()
  params.set('page', String(Math.max(1, Math.floor(query.page))))
  params.set('pageSize', String(Math.min(JOB_PAGE_SIZE_MAX, Math.max(1, Math.floor(query.pageSize)))))
  const search = query.search?.trim()
  if (search) params.set('search', search.slice(0, JOB_SEARCH_MAX))
  return api.getPaged<MachineDeliveryJobOrderCandidate>(`${BASE}/job-orders?${params.toString()}`)
}

/** POST / — stores.machine-deliveries:Issue. Returns the DC view. */
export function dispatchMachine(body: DispatchMachineRequest): Promise<MachineDeliveryView> {
  return api.post<MachineDeliveryView>(`${BASE}/`, body)
}

/** POST /{id}/signature — stores.machine-deliveries:Issue. Returns the DC view. */
export function signMachineDelivery(id: string, body: SignMachineDeliveryRequest): Promise<MachineDeliveryView> {
  return api.post<MachineDeliveryView>(`${BASE}/${encodeURIComponent(id)}/signature`, body)
}

/** GET /{id} — stores.machine-deliveries:View. 404 for unknown or other-company ids. */
export function getMachineDelivery(id: string): Promise<MachineDeliveryView> {
  return api.get<MachineDeliveryView>(`${BASE}/${encodeURIComponent(id)}`)
}

/** GET /{id}/signature-evidence — reports.machine-dossier:View. Saves the file itself. */
export async function downloadSignatureEvidence(id: string, fallbackName: string): Promise<void> {
  const response = await authorizedFetch(`${BASE}/${encodeURIComponent(id)}/signature-evidence`)
  await saveResponseAsFile(response, fallbackName)
}

// --- Request classification ---------------------------------------------
// 400 VALIDATION_FAILED: the request itself is wrong; every failing field is
// named in Errors. 409 BUSINESS_RULE_CONFLICT: well-formed, but database state
// refuses it. Anything else (network, 401-renewed, 5xx) is an unknown outcome.

/** True when the server definitely did not commit, so a new attempt needs a new key. */
export function isDefinitiveRefusal(error: unknown): boolean {
  return error instanceof ApiError && (error.status === 400 || error.status === 403 || error.status === 404 || error.status === 409)
}

/** Field errors of a 400, keyed as the server names them (DcNumber, Evidence.ContentType, ...). */
export function fieldErrorsOf(error: unknown): Record<string, string[]> {
  if (error instanceof ApiError && error.status === 400 && error.errors) return error.errors
  return {}
}

export function isBusinessRuleConflict(error: unknown): boolean {
  return error instanceof ApiError && error.status === 409 && error.code !== 'CONCURRENCY_CONFLICT'
}

// --- Signature evidence file ----------------------------------------------

/** Reads the leading bytes the same way the server does (MachineDeliveryRequestValidation.Sign). */
export function sniffContentType(bytes: Uint8Array): SignatureContentType | null {
  const startsWith = (signature: number[]) => signature.every((value, index) => bytes[index] === value)
  if (startsWith([0x25, 0x50, 0x44, 0x46, 0x2d])) return 'application/pdf' // %PDF-
  if (startsWith([137, 80, 78, 71, 13, 10, 26, 10])) return 'image/png'
  if (startsWith([0xff, 0xd8, 0xff])) return 'image/jpeg'
  return null
}

export function toBase64(bytes: Uint8Array): string {
  let binary = ''
  const chunk = 0x8000
  for (let offset = 0; offset < bytes.length; offset += chunk) {
    binary += String.fromCharCode(...bytes.subarray(offset, offset + chunk))
  }
  return btoa(binary)
}

export function formatBytes(size: number): string {
  if (size < 1024) return `${size} bytes`
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`
  return `${(size / (1024 * 1024)).toFixed(2)} MB`
}

export const CONTENT_TYPE_WORDS: Record<SignatureContentType, string> = {
  'application/pdf': 'PDF document',
  'image/png': 'PNG image',
  'image/jpeg': 'JPEG image',
}

// --- Dates ------------------------------------------------------------------
// The server's business day is India Standard Time. DateOnly fields travel as
// yyyy-MM-dd; every timestamp is sent as a UTC instant ending in Z.

const IST_OFFSET_MS = 330 * 60 * 1000

/** The India Standard Time calendar date of an instant, yyyy-MM-dd. */
export function istDate(instant: Date): string {
  return new Date(instant.getTime() + IST_OFFSET_MS).toISOString().slice(0, 10)
}

export function todayIst(): string {
  return istDate(new Date())
}

/** "2026-10-15" → "15 October 2026", with no time-zone shift. */
export function formatDateWords(value: string | null | undefined): string {
  if (!value) return '—'
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  if (!match) return value
  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
  return date.toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' })
}

/** Parses an offset timestamp as an instant and shows it in the operator's local time. */
export function formatInstant(value: string | null | undefined): string {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('en-IN', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

// --- Browser-local list of DCs --------------------------------------------
// The API has no DC list endpoint. The DCs dispatched or opened from this
// browser are remembered per company so a user can reopen one, and so a job
// already known to have a DC is not offered a second dispatch.

export interface RecentMachineDelivery {
  Id: string
  DcNumber: string
  JobOrderId: string
  MachineSerial: string
  CustomerName: string
  SeenAt: string
}

function recentKey(): string {
  return `nexaerp.stores.machine-deliveries.${getCompany() ?? 'none'}`
}

export function recentMachineDeliveries(): RecentMachineDelivery[] {
  try {
    const raw = localStorage.getItem(recentKey())
    const parsed = raw ? (JSON.parse(raw) as unknown) : []
    return Array.isArray(parsed) ? (parsed as RecentMachineDelivery[]) : []
  } catch {
    return []
  }
}

export function rememberMachineDelivery(dc: MachineDeliveryView): void {
  try {
    const kept = recentMachineDeliveries().filter((entry) => entry.Id !== dc.Id)
    const next: RecentMachineDelivery[] = [
      {
        Id: dc.Id,
        DcNumber: dc.DcNumber,
        JobOrderId: dc.JobOrderId,
        MachineSerial: dc.MachineSerial,
        CustomerName: dc.CustomerName,
        SeenAt: new Date().toISOString(),
      },
      ...kept,
    ].slice(0, 50)
    localStorage.setItem(recentKey(), JSON.stringify(next))
  } catch {
    // storage unavailable; the shortcut list is simply lost
  }
}
