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
  return api.post<{ employeeCode: string; approvalStatus: string }>(
    `${BASE}/${encodeURIComponent(employeeCode)}/${action}`,
    { remarks },
  )
}

export function setLoginStatus(employeeCode: string, enable: boolean, reason: string) {
  return api.post<{ employeeCode: string; loginEnabled: boolean; status: string }>(
    `${BASE}/${encodeURIComponent(employeeCode)}/${enable ? 'activate-login' : 'deactivate-login'}`,
    { reason },
  )
}

export function getEmployeeRoles(employeeCode: string): Promise<EmployeeRoleSummary[]> {
  return api.get<EmployeeRoleSummary[]>(`${BASE}/${encodeURIComponent(employeeCode)}/roles`)
}

export function getEmployeeHistory(employeeCode: string): Promise<EmployeeHistorySummary[]> {
  return api.get<EmployeeHistorySummary[]>(`${BASE}/${encodeURIComponent(employeeCode)}/history`)
}
