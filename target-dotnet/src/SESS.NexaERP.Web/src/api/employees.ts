import { api } from './client'
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
}

export function listEmployees(query: EmployeeListQuery): Promise<EmployeeSummary[]> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  return api.get<EmployeeSummary[]>(`${BASE}/?${params.toString()}`)
}

export function getEmployeeLookups(): Promise<EmployeeMasterLookups> {
  return api.get<EmployeeMasterLookups>(`${BASE}/lookups`)
}

/** Debug-only: provisions everything the employee needs to sign in with their own password. */
export function provisionDevLogin(employeeCode: string, password: string, roleCode: string) {
  return api.post<{ EmployeeCode: string; LoginEnabled: boolean; RoleCode: string }>(
    `${BASE}/${encodeURIComponent(employeeCode)}/provision-dev-login`,
    { Password: password, RoleCode: roleCode || null },
  )
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

export function changeApprovalStatus(employeeCode: string, action: ApprovalAction, remarks: string) {
  return api.post<{ EmployeeCode: string; ApprovalStatus: string }>(
    `${BASE}/${encodeURIComponent(employeeCode)}/${action}`,
    { Remarks: remarks },
  )
}

export function setLoginStatus(employeeCode: string, enable: boolean, reason: string) {
  return api.post<{ EmployeeCode: string; LoginEnabled: boolean; Status: string }>(
    `${BASE}/${encodeURIComponent(employeeCode)}/${enable ? 'activate-login' : 'deactivate-login'}`,
    { Reason: reason },
  )
}

export function getEmployeeRoles(employeeCode: string): Promise<EmployeeRoleSummary[]> {
  return api.get<EmployeeRoleSummary[]>(`${BASE}/${encodeURIComponent(employeeCode)}/roles`)
}

export function getEmployeeHistory(employeeCode: string): Promise<EmployeeHistorySummary[]> {
  return api.get<EmployeeHistorySummary[]>(`${BASE}/${encodeURIComponent(employeeCode)}/history`)
}
