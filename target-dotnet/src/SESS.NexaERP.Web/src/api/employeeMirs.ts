import { api } from './client'
import type { CreateMaterialIssueRequest, UpdateMaterialIssueRequest, MaterialIssueRequestPage, MaterialIssueRequestView,
  MaterialIssueTransitionRequest, MaterialIssueView, CreateMaterialReturn, MaterialReturnPage, MaterialReturnView,
  OutstandingEngineerCustodyView } from '../types/materialIssue'

const BASE = '/api/v1/employee/my-mirs'
export interface PortalItem { Id: string; ItemCode: string; Name: string; UomId: string; UomCode: string; Barcode?: string | null }
export interface PortalReference { Id: string; Code: string; Name: string }
export interface PortalSpareLine { Id: string; ItemId: string; PoNumber: string; LineNumber: number; Description: string; Quantity: number }
export interface PortalReferences { DepartmentId: string; DepartmentCode: string; Jobs: PortalReference[]; Customers: PortalReference[] }
export interface PortalIssue { Issue: MaterialIssueView; Items: PortalItem[]; ReturnableQuantities: Record<string, number> }
export const listOwnMirs = (page = 1, number = '', status = '') =>
  api.get<MaterialIssueRequestPage>(`${BASE}/?${new URLSearchParams({ page: String(page), pageSize: '25', number, status })}`)
export const getOwnMir = (id: string) => api.get<MaterialIssueRequestView>(`${BASE}/${encodeURIComponent(id)}`)
export const createOwnMir = (JobOrMachine: string, Request: CreateMaterialIssueRequest) =>
  api.post<MaterialIssueRequestView>(`${BASE}/`, { JobOrMachine, Request })
export const updateOwnMir = (id: string, JobOrMachine: string, Request: UpdateMaterialIssueRequest) =>
  api.put<MaterialIssueRequestView>(`${BASE}/${encodeURIComponent(id)}`, { JobOrMachine, Request })
export const submitOwnMir = (id: string, body: MaterialIssueTransitionRequest) =>
  api.post<MaterialIssueRequestView>(`${BASE}/${encodeURIComponent(id)}/submit`, body)
export const searchPortalItems = (query: string, scan = false) => api.get<PortalItem[]>(`${BASE}/items?${new URLSearchParams({ query, scan: String(scan) })}`)
export const getPortalReferences = () => api.get<PortalReferences>(`${BASE}/references`)
export const listOwnCustody = () => api.get<OutstandingEngineerCustodyView[]>(`${BASE}/custody`)
export const getOwnIssue = (id: string) => api.get<PortalIssue>(`${BASE}/issues/${encodeURIComponent(id)}`)
export const listOwnReturns = (page = 1) => api.get<MaterialReturnPage>(`${BASE}/returns?page=${page}&pageSize=25`)
export const getOwnReturn = (id: string) => api.get<MaterialReturnView>(`${BASE}/returns/${encodeURIComponent(id)}`)
export const declareOwnReturn = (id: string, body: CreateMaterialReturn) =>
  api.post<MaterialReturnView>(`${BASE}/issues/${encodeURIComponent(id)}/returns`, body)

export const getPortalSpareLines = (customerId: string) => api.get<PortalSpareLine[]>(`${BASE}/spare-po-lines?${new URLSearchParams({ customerId })}`)
