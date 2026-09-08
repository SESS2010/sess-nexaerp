import { api } from './client'
import type {
  AcceptMaterialReturn,
  CreateMaterialIssue,
  CreateMaterialIssueRequest,
  CreateMaterialReturn,
  MaterialIssueExcessDecisionRequest,
  MaterialIssueRequestPage,
  MaterialIssueRequestView,
  MaterialIssueTransitionRequest,
  MaterialIssueView,
  MaterialReturnPage,
  MaterialReturnView,
  OutstandingEngineerCustodyView,
  UpdateMaterialIssueRequest,
} from '../types/materialIssue'

// MaterialIssueEndpoints.cs. Every command carries IdempotencyKey in the body.
const REQUESTS = '/api/v1/stores/material-issue-requests'
const EXCESS = '/api/v1/stores/material-issue-excess'
const ISSUES = '/api/v1/stores/material-issues'
const RETURNS = '/api/v1/stores/material-returns'

export interface MaterialIssueRequestListQuery {
  page: number
  pageSize: number
  /** Substring of the MIR number; the server upper-cases it. */
  number?: string
  status?: string
}

export function listMaterialIssueRequests(query: MaterialIssueRequestListQuery): Promise<MaterialIssueRequestPage> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.number) params.set('number', query.number)
  if (query.status) params.set('status', query.status)
  return api.get<MaterialIssueRequestPage>(`${REQUESTS}/?${params.toString()}`)
}

export function getMaterialIssueRequest(id: string): Promise<MaterialIssueRequestView> {
  return api.get<MaterialIssueRequestView>(`${REQUESTS}/${encodeURIComponent(id)}`)
}

export function createMaterialIssueRequest(body: CreateMaterialIssueRequest): Promise<MaterialIssueRequestView> {
  return api.post<MaterialIssueRequestView>(`${REQUESTS}/`, body)
}

export function updateMaterialIssueRequest(id: string, body: UpdateMaterialIssueRequest): Promise<MaterialIssueRequestView> {
  return api.put<MaterialIssueRequestView>(`${REQUESTS}/${encodeURIComponent(id)}`, body)
}

export type MaterialIssueRequestTransition = 'submit' | 'approve' | 'reject' | 'cancel'

/**
 * DRAFT → submit → SUBMITTED → approve/reject → APPROVED/REJECTED; cancel from
 * DRAFT or SUBMITTED. Approve and reject need STORES_MANAGER or
 * PRODUCTION_MANAGER and refuse the requester's own MIR; cancel needs STORES_MANAGER.
 */
export function transitionMaterialIssueRequest(
  id: string,
  transition: MaterialIssueRequestTransition,
  body: MaterialIssueTransitionRequest,
): Promise<MaterialIssueRequestView> {
  return api.post<MaterialIssueRequestView>(`${REQUESTS}/${encodeURIComponent(id)}/${transition}`, body)
}

/** Technical Director decides a customer-facing excess line. */
export function decideMaterialIssueExcess(
  lineId: string,
  body: MaterialIssueExcessDecisionRequest,
): Promise<MaterialIssueRequestView> {
  return api.post<MaterialIssueRequestView>(`${EXCESS}/${encodeURIComponent(lineId)}/decision`, body)
}

export function getMaterialIssue(id: string): Promise<MaterialIssueView> {
  return api.get<MaterialIssueView>(`${ISSUES}/${encodeURIComponent(id)}`)
}

export function issueMaterialFromRequest(requestId: string, body: CreateMaterialIssue): Promise<MaterialIssueView> {
  return api.post<MaterialIssueView>(`${ISSUES}/from-request/${encodeURIComponent(requestId)}`, body)
}

export function listOutstandingCustody(employeeId?: string, notificationDue?: boolean): Promise<OutstandingEngineerCustodyView[]> {
  const params = new URLSearchParams()
  if (employeeId) params.set('employeeId', employeeId)
  if (notificationDue !== undefined) params.set('notificationDue', String(notificationDue))
  const suffix = params.toString()
  return api.get<OutstandingEngineerCustodyView[]>(`${ISSUES}/outstanding-custody${suffix ? `?${suffix}` : ''}`)
}

export interface MaterialReturnListQuery {
  page: number
  pageSize: number
  materialIssueId?: string
  status?: string
}

export function listMaterialReturns(query: MaterialReturnListQuery): Promise<MaterialReturnPage> {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.materialIssueId) params.set('materialIssueId', query.materialIssueId)
  if (query.status) params.set('status', query.status)
  return api.get<MaterialReturnPage>(`${RETURNS}/?${params.toString()}`)
}

export function getMaterialReturn(id: string): Promise<MaterialReturnView> {
  return api.get<MaterialReturnView>(`${RETURNS}/${encodeURIComponent(id)}`)
}

export function createMaterialReturn(materialIssueId: string, body: CreateMaterialReturn): Promise<MaterialReturnView> {
  return api.post<MaterialReturnView>(`${RETURNS}/from-issue/${encodeURIComponent(materialIssueId)}`, body)
}

export function acceptMaterialReturn(id: string, body: AcceptMaterialReturn): Promise<MaterialReturnView> {
  return api.post<MaterialReturnView>(`${RETURNS}/${encodeURIComponent(id)}/accept`, body)
}
