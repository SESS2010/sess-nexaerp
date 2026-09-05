import { api, type PagedResponse } from './client'
import type {
  CreateEmployeeRequest,
  EmployeeDetail,
  EmployeeHistorySummary,
  EmployeeMasterLookups,
  EmployeeRoleSummary,
  EmployeeSummary,
  UpdateEmployeeRequest,
} from '../types/employee'

const BASE = '/api/v1/employees'

export type ApprovalAction = 'submit' | 'approve' | 'reject' | 'revise'

export interface EmployeeListQuery {
  page: number
  pageSize: number
  search?: string
  status?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

/** Paged since main b0b2a91: TotalCount/PageNumber/PageSize/Items, each row carrying its Version. */
export function listEmployees(query: EmployeeListQuery): Promise<PagedResponse<EmployeeSummary>> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  if (query.sortBy) params.set('sortBy', query.sortBy)
  if (query.sortDirection) params.set('sortDirection', query.sortDirection)
  return api.get<PagedResponse<EmployeeSummary>>(`${BASE}/?${params.toString()}`)
}

export function getEmployeeLookups(): Promise<EmployeeMasterLookups> {
  return api.get<EmployeeMasterLookups>(`${BASE}/lookups`)
}


export function createEmployeeLookup(kind: 'departments' | 'skills' | 'designations', code: string, name: string) {
  return api.post<{ Id: string; Code: string; Name: string }>(`${BASE}/lookups/${kind}`, { Code: code, Name: name })
}

export function getEmployee(employeeCode: string): Promise<EmployeeDetail> {
  return api.get<EmployeeDetail>(`${BASE}/${encodeURIComponent(employeeCode)}`)
}

export function createEmployee(body: CreateEmployeeRequest): Promise<EmployeeDetail> {
  return api.post<EmployeeDetail>(`${BASE}/`, body)
}

export function updateEmployee(employeeCode: string, body: UpdateEmployeeRequest): Promise<EmployeeDetail> {
  return api.put<EmployeeDetail>(`${BASE}/${encodeURIComponent(employeeCode)}`, body)
}

/**
 * Every state change carries the Version the screen read. The server answers
 * 409 "Stale employee version" when someone else changed the record first;
 * the caller reloads and the user decides again on the current state.
 */
export function changeApprovalStatus(employeeCode: string, action: ApprovalAction, remarks: string, version: number) {
  return api.post<{ EmployeeCode: string; ApprovalStatus: string; Version: number }>(
    `${BASE}/${encodeURIComponent(employeeCode)}/${action}`,
    { Remarks: remarks, Version: version },
  )
}

export function setLoginStatus(employeeCode: string, enable: boolean, reason: string, version: number) {
  return api.post<{ EmployeeCode: string; LoginEnabled: boolean; Status: string; Version: number }>(
    `${BASE}/${encodeURIComponent(employeeCode)}/${enable ? 'activate-login' : 'deactivate-login'}`,
    { Reason: reason, Version: version },
  )
}

export function getEmployeeRoles(employeeCode: string): Promise<EmployeeRoleSummary[]> {
  return api.get<EmployeeRoleSummary[]>(`${BASE}/${encodeURIComponent(employeeCode)}/roles`)
}

export function getEmployeeHistory(employeeCode: string): Promise<EmployeeHistorySummary[]> {
  return api.get<EmployeeHistorySummary[]>(`${BASE}/${encodeURIComponent(employeeCode)}/history`)
}
