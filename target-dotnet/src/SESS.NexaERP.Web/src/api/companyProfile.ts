import { ApiError, api } from './client'
import type { SessionMe } from '../features/auth/SessionContext'
import type {
  CompanyProfileView,
  SaveCompanyProfileRequest,
  SaveWarehouseStateCodeRequest,
  WarehouseStateCodeView,
} from '../types/companyProfile'

// CompanyProfileEndpoints.cs. The group needs only a signed-in employee of the
// selected company (EmployeeScopeEndpointFilter) — there is no page key, so any
// role can read the profile. Both PUTs call RequireRole("update",
// "TECHNICAL_DIRECTOR") in EfCompanyProfileService: the caller needs a current
// TECHNICAL_DIRECTOR assignment (FULL, TEMPORARY or SUPPORT — "update" is not a
// support-denied operation), or the save answers 403.
//
// Errors: 400 {message, errors} with a field dictionary (Gstin, Pan, StateCode,
// PinCode, LegalName, ..., Reason), 409 for a stale Version. Both arrive as the
// standard envelope, so ApiError.errors carries the fields.
const BASE = '/api/v1/company'

/** The role whose assignment the server requires for every company-profile write. */
export const COMPANY_PROFILE_EDITOR_ROLE = 'TECHNICAL_DIRECTOR'

/** True when this session may save the profile or a warehouse state code. Mirrors RequireRole("update", "TECHNICAL_DIRECTOR"). */
export function canEditCompanyProfile(me: SessionMe | null): boolean {
  return (me?.RoleCodes ?? []).some((code) => code.trim().toUpperCase() === COMPANY_PROFILE_EDITOR_ROLE)
}

export function getCompanyProfile(): Promise<CompanyProfileView> {
  return api.get<CompanyProfileView>(`${BASE}/profile`)
}

export function saveCompanyProfile(body: SaveCompanyProfileRequest): Promise<CompanyProfileView> {
  return api.put<CompanyProfileView>(`${BASE}/profile`, body)
}

export async function listWarehouseStateCodes(): Promise<WarehouseStateCodeView[]> {
  const data = await api.get<unknown>(`${BASE}/warehouse-state-codes`)
  return Array.isArray(data) ? (data as WarehouseStateCodeView[]) : []
}

export function saveWarehouseStateCode(warehouseCode: string, body: SaveWarehouseStateCodeRequest): Promise<WarehouseStateCodeView> {
  return api.put<WarehouseStateCodeView>(`${BASE}/warehouse-state-codes/${encodeURIComponent(warehouseCode)}`, body)
}

/** Field errors of a 400, keyed as the server names them (Gstin, Pan, StateCode, PinCode, Reason, ...). */
export function companyFieldErrorsOf(error: unknown): Record<string, string[]> {
  if (error instanceof ApiError && error.status === 400 && error.errors) return error.errors
  return {}
}

// --- Client-side form rules, the same ones EfCompanyProfileService.Validate applies ---

export const GSTIN_PATTERN = /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$/
export const PAN_PATTERN = /^[A-Z]{5}[0-9]{4}[A-Z]$/
export const STATE_CODE_PATTERN = /^[0-9]{2}$/
export const PIN_CODE_PATTERN = /^[1-9][0-9]{5}$/

export interface CompanyProfileFormValues {
  LegalName: string
  TradeName: string
  Gstin: string
  Pan: string
  StateCode: string
  State: string
  AddressLine1: string
  AddressLine2: string
  City: string
  PinCode: string
  Phone: string
  Email: string
  Reason: string
}

/**
 * The checks the server makes, run before sending so the user sees them next
 * to the field instead of after a round trip. Returns one message per field.
 */
export function validateCompanyProfile(values: CompanyProfileFormValues): Record<string, string[]> {
  const errors: Record<string, string[]> = {}
  const fail = (field: string, message: string) => { errors[field] = [message] }
  const required = (field: string, value: string, max: number, label: string) => {
    const text = value.trim()
    if (!text) fail(field, `${label} is required.`)
    else if (text.length > max) fail(field, `${label} must be at most ${max} characters.`)
  }
  const gstin = values.Gstin.trim().toUpperCase()
  const pan = values.Pan.trim().toUpperCase()
  const stateCode = values.StateCode.trim()
  const pin = values.PinCode.trim()

  required('LegalName', values.LegalName, 200, 'Legal name')
  if (gstin.length !== 15) fail('Gstin', 'GSTIN must be exactly 15 characters, for example 33ABCDE1234F1Z5.')
  else if (!GSTIN_PATTERN.test(gstin)) fail('Gstin', 'GSTIN is not in the GST format (2 digits, 5 letters, 4 digits, letter, entity code, Z, check character).')
  if (pan.length !== 10) fail('Pan', 'PAN must be exactly 10 characters, for example ABCDE1234F.')
  else if (!PAN_PATTERN.test(pan)) fail('Pan', 'PAN is not in the PAN format (5 letters, 4 digits, 1 letter).')
  if (!STATE_CODE_PATTERN.test(stateCode)) fail('StateCode', 'State code must be the two-digit GST state code, for example 33.')
  if (GSTIN_PATTERN.test(gstin) && STATE_CODE_PATTERN.test(stateCode) && gstin.slice(0, 2) !== stateCode)
    fail('StateCode', `State code must equal the first two digits of the GSTIN (${gstin.slice(0, 2)}).`)
  if (GSTIN_PATTERN.test(gstin) && PAN_PATTERN.test(pan) && gstin.slice(2, 12) !== pan)
    fail('Pan', `PAN must equal characters 3 to 12 of the GSTIN (${gstin.slice(2, 12)}).`)
  required('State', values.State, 100, 'State')
  required('AddressLine1', values.AddressLine1, 200, 'Address line 1')
  required('City', values.City, 100, 'City')
  if (!PIN_CODE_PATTERN.test(pin)) fail('PinCode', 'PIN code must be six digits and cannot start with 0.')
  if (!values.Reason.trim()) fail('Reason', 'Reason is required; it becomes the audit remark.')
  return errors
}

/** The form's values as the PUT body, trimmed and upper-cased the way the server stores them. */
export function toSaveCompanyProfileRequest(values: CompanyProfileFormValues, version: number): SaveCompanyProfileRequest {
  const opt = (value: string) => (value.trim() ? value.trim() : null)
  return {
    LegalName: values.LegalName.trim(),
    TradeName: opt(values.TradeName),
    Gstin: values.Gstin.trim().toUpperCase(),
    Pan: values.Pan.trim().toUpperCase(),
    StateCode: values.StateCode.trim(),
    State: values.State.trim(),
    AddressLine1: values.AddressLine1.trim(),
    AddressLine2: opt(values.AddressLine2),
    City: values.City.trim(),
    PinCode: values.PinCode.trim(),
    Phone: opt(values.Phone),
    Email: opt(values.Email),
    Version: version,
    Reason: values.Reason.trim(),
  }
}
