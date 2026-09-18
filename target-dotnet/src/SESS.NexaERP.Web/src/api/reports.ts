// Company reports (CompanyReportEndpoints.cs, EfCompanyReportService.cs).
// One engine, ten definitions (ReportDefinitions.cs): the server returns the
// column list with the data, every summary row carries a `group` object that
// is the exact drill-through selection for its details, and the Excel export
// is the summary with totals. Access is decided per report by the SQL
// (report_grants), so the catalogue only lists what this session may open.

import { api, getStoredToken } from './client'

const BASE = '/api/v1/reports'

export interface ReportDescriptor {
  Key: string
  Title: string
  /** Movement roll-forward and vendor purchases take a from date; the rest are "as of" the to date. */
  UsesPeriod: boolean
  ContainsCommercialValues: boolean
  Coverage: string | null
  /** Pending approvals: the to date must be today. */
  CurrentOnly: boolean
}

export interface ReportColumn {
  Key: string
  Label: string
  /** text | number | date */
  Type: string
  /** Set on summary measure columns that can be drilled through on their own. */
  Metric: string | null
}

/** Rows are the server's JSON objects keyed by column key, plus `group` on summaries. */
export type ReportRow = Record<string, unknown> & { group?: Record<string, string | null> }

export interface ReportPage {
  Key: string
  Title: string
  CompanyCode: string
  GeneratedAt: string
  FromDate: string | null
  ToDate: string
  Mode: 'summary' | 'details'
  Page: number
  PageSize: number
  TotalRows: number
  TotalSourceRows: number
  Columns: ReportColumn[]
  Rows: ReportRow[]
  Totals: ReportRow[]
  Coverage: string | null
  TimeZone: string
}

export interface ReportQuery {
  fromDate?: string | null
  toDate?: string | null
  mode: 'summary' | 'details'
  /** JSON object of drill-through dimensions — a summary row's `group`. */
  selection?: string | null
  metric?: string | null
  page: number
  pageSize: number
}

function params(query: Partial<ReportQuery>): string {
  const p = new URLSearchParams()
  if (query.fromDate) p.set('fromDate', query.fromDate)
  if (query.toDate) p.set('toDate', query.toDate)
  if (query.mode) p.set('mode', query.mode)
  if (query.selection) p.set('selection', query.selection)
  if (query.metric) p.set('metric', query.metric)
  if (query.page) p.set('page', String(query.page))
  if (query.pageSize) p.set('pageSize', String(query.pageSize))
  return p.toString()
}

export function listReports(): Promise<ReportDescriptor[]> {
  return api.get<ReportDescriptor[]>(`${BASE}/`)
}

export function getReport(key: string, query: ReportQuery): Promise<ReportPage> {
  return api.get<ReportPage>(`${BASE}/${encodeURIComponent(key)}?${params(query)}`)
}

/** GET /reports/{key}/excel — summary with totals; the file name is `{key}-{toDate}.xlsx`. */
export async function downloadReportExcel(key: string, query: Pick<ReportQuery, 'fromDate' | 'toDate' | 'selection'>): Promise<void> {
  const token = getStoredToken()
  const response = await fetch(`${BASE}/${encodeURIComponent(key)}/excel?${params(query)}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  if (!response.ok) {
    let message = `Export failed (${response.status})`
    try {
      const body = await response.json()
      message = body.Detail || body.message || message
    } catch { /* keep default */ }
    throw new Error(message)
  }
  const disposition = response.headers.get('content-disposition') ?? ''
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)/i.exec(disposition)
  const url = URL.createObjectURL(await response.blob())
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = match?.[1] ? decodeURIComponent(match[1]) : `${key}.xlsx`
  anchor.click()
  URL.revokeObjectURL(url)
}
