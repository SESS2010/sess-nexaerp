/** Short-lived, memory-only GET cache. Context includes company/identity/authority/document. Never caches a command. */
export class ScanReadCache<T> {
  private rows = new Map<string, { expires: number; read: Promise<T> }>()
  private readonly now: () => number
  private readonly ttlMs: number
  constructor(now: () => number = Date.now, ttlMs = 10_000) { this.now = now; this.ttlMs = ttlMs }
  read(key: string, load: () => Promise<T>): Promise<T> {
    const row = this.rows.get(key)
    if (row && row.expires > this.now()) return row.read
    this.rows.delete(key)
    if (this.rows.size >= 64) this.rows.delete(this.rows.keys().next().value!)
    const next = { expires: this.now() + this.ttlMs, read: Promise.resolve().then(load) }
    this.rows.set(key, next)
    next.read.catch(() => { if (this.rows.get(key) === next) this.rows.delete(key) })
    return next.read
  }
  clear(): void { this.rows.clear() }
}