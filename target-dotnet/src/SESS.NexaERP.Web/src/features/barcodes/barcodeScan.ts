import { grnLotCompany } from './grnLotCode.ts'
/** Text-only barcode matching and lifecycle-safe scanner queue. No command or posting API here. */
export interface ScanCandidate {
  lineKey: string
  itemId: string
  itemCode: string
  barcode: string | null
  inventorySerialId: string | null
  storedSerialNumber: string | null
  requiresSerial?: boolean
}
export type ScanResolution =
  | { kind: 'match'; candidate: ScanCandidate }
  | { kind: 'unknown'; message: string }
  | { kind: 'ambiguous'; message: string }

export function scanText(value: unknown): string {
  if (typeof value !== 'string') throw new Error('Barcode must be text. Leading zeros cannot be recovered from a number.')
  const token = value.trim()
  if (!token || token.length > 200 || /[\u0000-\u001f\u007f]/.test(token)) {
    throw new Error('Scan a non-empty item barcode, item code or serial (maximum 200 characters).')
  }
  return token
}

function sameText(left: string | null, right: string): boolean {
  return left !== null && left.trim().toUpperCase() === right.toUpperCase()
}

/** Candidates MUST already be authorized for the current company/document/custody. Never first-match a collision. */
export function resolveScan(candidates: readonly ScanCandidate[], value: unknown, expectedCompany?: string): ScanResolution {
  const token = scanText(value)
  const lotCompany = grnLotCompany(token.toUpperCase())
  if (expectedCompany && lotCompany && lotCompany !== expectedCompany) return { kind: 'unknown', message: 'This GRN lot label belongs to another company. Select the correct company; nothing was added.' }
  const matches = candidates.filter((row) => row.inventorySerialId
    ? sameText(row.storedSerialNumber, token)
    : sameText(row.itemCode, token) || sameText(row.barcode, token))
  const distinct = new Map(matches.map((row) => [row.lineKey + '|' + (row.inventorySerialId ?? ''), row]))
  if (distinct.size === 1) return { kind: 'match', candidate: [...distinct.values()][0] }
  if (distinct.size > 1) return { kind: 'ambiguous', message: 'This scan matches more than one eligible line or identity. Select the line explicitly; nothing was added.' }
  return { kind: 'unknown', message: 'Unknown barcode / item code / serial in this document. Check the label or select the item manually; nothing was added.' }
}

export interface QuantityScan {
  quantity: string
  changed: boolean
  warning: string
}
export function addScanQuantity(current: string, maximum: number, serialized = false): QuantityScan {
  const qty = current.trim() === '' ? 0 : Number(current)
  if (!Number.isFinite(qty) || qty < 0 || !Number.isFinite(maximum) || maximum < 0) {
    return { quantity: current, changed: false, warning: 'Enter a valid quantity and reload the current allowance before scanning.' }
  }
  const next = Math.round((qty + 1) * 1_000_000) / 1_000_000
  if (serialized && (qty !== 0 || maximum < 1)) {
    return { quantity: current, changed: false, warning: 'This physical serial is already selected or is not available. Scan a different serial; quantity stays one.' }
  }
  if (next > maximum) {
    return { quantity: current, changed: false, warning: 'The scan would exceed the current document or custody quantity. Nothing was added.' }
  }
  return { quantity: String(next), changed: true, warning: '' }
}

interface Job { token: string; target: string; epoch: number }
interface Owner { target: string; epoch: number }
export interface ScanQueueOptions<T> {
  lookup: (token: string, target: string) => Promise<T>
  resolved: (value: T, target: string) => void
  warning: (message: string) => void
  busy: (busy: boolean) => void
  currentTarget?: () => string
}

/** Each scanner suffix queues one read; old target/unmount responses and old finish callbacks cannot mutate a new action. */
export class ContextScanQueue<T> {
  private target = ''
  private epoch = 0
  private jobs: Job[] = []
  private owner: Owner | null = null
  private disposed = false
  private options: ScanQueueOptions<T>

  constructor(options: ScanQueueOptions<T>) { this.options = options }
  setTarget(target: string): void {
    if (target === this.target || this.disposed) return
    this.target = target
    this.epoch++
    this.jobs = []
    this.owner = null
    this.options.busy(false)
  }
  enqueue(raw: unknown): boolean {
    if (this.disposed || !this.target) return false
    let token: string
    try { token = scanText(raw) } catch (error) {
      this.options.warning(error instanceof Error ? error.message : 'Invalid scan.')
      return false
    }
    if (this.jobs.length >= 100) {
      this.options.warning('Scanner queue is full. Wait for the pending scans before continuing; this scan was not added.')
      return false
    }
    this.jobs.push({ token, target: this.target, epoch: this.epoch })
    this.pump()
    return true
  }
  dispose(): void {
    this.disposed = true
    this.epoch++
    this.jobs = []
    this.owner = null
  }
  private live(owner: Owner): boolean {
    return !this.disposed && this.owner === owner && owner.epoch === this.epoch
      && owner.target === this.target
      && (!this.options.currentTarget || this.options.currentTarget() === owner.target)
  }
  private pump(): void {
    if (this.disposed || this.owner) return
    const job = this.jobs.shift()
    if (!job) { this.options.busy(false); return }
    const owner: Owner = { target: job.target, epoch: job.epoch }
    this.owner = owner
    this.options.busy(true)
    Promise.resolve().then(() => {
      if (!this.live(owner)) return undefined
      return this.options.lookup(job.token, job.target)
    }).then((value) => {
      if (this.live(owner) && value !== undefined) this.options.resolved(value, job.target)
    }).catch((error: unknown) => {
      if (this.live(owner)) this.options.warning(error instanceof Error ? error.message : 'Scan lookup failed. Check the selected company and document.')
    }).finally(() => {
      if (this.owner !== owner || this.disposed) return
      this.owner = null
      this.pump()
    })
  }
}
