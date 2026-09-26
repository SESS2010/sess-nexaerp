// Small, dependency-free charts for the Purchase dashboard. Every bar is drawn
// to scale against the largest value in the same chart, and every chart holds
// ONE currency: currencies are never added or drawn on a shared axis.

import type { ReactNode } from 'react'

const compactFormatters = new Map<string, Intl.NumberFormat>()

/** Short axis label in Indian units, e.g. "₹1.2L", "₹3.4Cr". Exact figures are in tooltips and tables. */
export function formatCompactAmount(value: number, currency: string): string {
  let formatter = compactFormatters.get(currency)
  if (!formatter) {
    try {
      formatter = new Intl.NumberFormat('en-IN', { style: 'currency', currency, notation: 'compact', maximumFractionDigits: 1 })
    } catch {
      formatter = new Intl.NumberFormat('en-IN', { notation: 'compact', maximumFractionDigits: 1 })
    }
    compactFormatters.set(currency, formatter)
  }
  return formatter.format(value)
}

export interface HBar {
  key: string
  label: ReactNode
  /** Plain-text label for the tooltip and screen readers. */
  labelText: string
  /** null: no bar (e.g. withheld); `display` explains why. */
  value: number | null
  display: ReactNode
  displayText: string
  color?: string
  note?: ReactNode
  onClick?: () => void
  clickHint?: string
  selected?: boolean
}

/**
 * Horizontal bars, longest = largest absolute value. Negative values (for
 * example net reversals) are drawn red with a minus sign in the figure.
 */
export function HBarChart({ bars, caption, emptyText = 'Nothing to chart.' }: { bars: HBar[]; caption: string; emptyText?: string }) {
  const max = Math.max(0, ...bars.map((bar) => (bar.value === null ? 0 : Math.abs(bar.value))))
  if (bars.length === 0) return <p className="text-[12.5px] text-ink-faint">{emptyText}</p>
  return (
    <figure className="m-0">
      <figcaption className="sr-only">{caption}</figcaption>
      <ul className="m-0 list-none p-0 flex flex-col gap-1.5">
        {bars.map((bar) => {
          const width = bar.value === null || max === 0 ? 0 : (Math.abs(bar.value) / max) * 100
          const color = bar.value !== null && bar.value < 0 ? '#dc2626' : bar.color ?? '#1d4ed8'
          const body = (
            <>
              <span className="min-w-0 truncate text-[13px]" title={bar.labelText}>{bar.label}</span>
              <span className="relative h-4 rounded bg-slate-100" aria-hidden="true">
                <span className="absolute inset-y-0 left-0 rounded" style={{ width: `${Math.max(width, bar.value ? 1.5 : 0)}%`, background: color }} />
              </span>
              <span className="text-right text-[13px] whitespace-nowrap">{bar.display}</span>
            </>
          )
          const gridStyle = { gridTemplateColumns: 'minmax(0, 34%) minmax(0, 1fr) auto' }
          const aria = `${bar.labelText}: ${bar.displayText}`
          return (
            <li key={bar.key}>
              {bar.onClick ? (
                <button type="button" onClick={bar.onClick} title={bar.clickHint ? `${aria}. ${bar.clickHint}` : aria} aria-label={aria}
                  className={`grid w-full items-center gap-3 rounded-md border-0 bg-transparent px-1.5 py-1 text-left cursor-pointer hover:bg-accent-soft ${bar.selected ? 'bg-accent-soft ring-1 ring-blue-300' : ''}`}
                  style={gridStyle}>
                  {body}
                </button>
              ) : (
                <div className="grid items-center gap-3 px-1.5 py-1" style={gridStyle} title={aria}>{body}</div>
              )}
              {bar.note && <div className="pl-1.5 text-[11.5px] text-ink-faint">{bar.note}</div>}
            </li>
          )
        })}
      </ul>
    </figure>
  )
}

export interface Column {
  key: string
  label: string
  value: number
  displayText: string
  selected?: boolean
  onClick?: () => void
}

/**
 * Vertical columns sharing one zero line, for a monthly trend in one currency.
 * The axis shows zero and the largest (and, if any, the most negative) value.
 */
export function ColumnChart({ columns, currency, caption, clickHint }: { columns: Column[]; currency: string; caption: string; clickHint?: string }) {
  const max = Math.max(0, ...columns.map((column) => column.value))
  const min = Math.min(0, ...columns.map((column) => column.value))
  const span = max - min || 1
  const zeroFromTop = (max / span) * 100
  const height = 170

  return (
    <figure className="m-0">
      <figcaption className="sr-only">{caption}</figcaption>
      <div className="flex gap-2">
        <div className="relative shrink-0 text-right text-[11px] text-ink-faint" style={{ width: 64, height }} aria-hidden="true">
          {max > 0 && <span className="absolute right-0 -translate-y-1/2" style={{ top: 0 }}>{formatCompactAmount(max, currency)}</span>}
          <span className="absolute right-0 -translate-y-1/2" style={{ top: `${zeroFromTop}%` }}>0</span>
          {min < 0 && <span className="absolute right-0 -translate-y-1/2" style={{ top: '100%' }}>{formatCompactAmount(min, currency)}</span>}
        </div>
        <div className="relative flex-1" style={{ height }}>
          <div className="absolute inset-x-0 border-t border-dashed border-slate-200" style={{ top: 0 }} aria-hidden="true" />
          <div className="absolute inset-x-0 border-t border-slate-300" style={{ top: `${zeroFromTop}%` }} aria-hidden="true" />
          <div className="absolute inset-0 grid gap-1.5" style={{ gridTemplateColumns: `repeat(${columns.length}, minmax(0, 1fr))` }}>
            {columns.map((column) => {
              const barHeight = (Math.abs(column.value) / span) * 100
              const top = column.value >= 0 ? zeroFromTop - barHeight : zeroFromTop
              const color = column.value < 0 ? '#dc2626' : column.selected ? '#1e40af' : '#3b82f6'
              const aria = `${column.label}: ${column.displayText}`
              return (
                <button key={column.key} type="button" onClick={column.onClick} disabled={!column.onClick}
                  aria-label={aria} aria-pressed={column.selected} title={clickHint ? `${aria}. ${clickHint}` : aria}
                  className={`relative h-full rounded-sm border-0 bg-transparent p-0 ${column.onClick ? 'cursor-pointer hover:bg-slate-100' : ''} ${column.selected ? 'bg-accent-soft' : ''}`}>
                  <span className="absolute inset-x-[12%] rounded-sm" style={{ top: `${top}%`, height: `${Math.max(barHeight, column.value !== 0 ? 0.8 : 0)}%`, background: color }} />
                </button>
              )
            })}
          </div>
        </div>
      </div>
      <div className="flex gap-2 mt-1" aria-hidden="true">
        <div className="shrink-0" style={{ width: 64 }} />
        <div className="flex-1 grid gap-1.5" style={{ gridTemplateColumns: `repeat(${columns.length}, minmax(0, 1fr))` }}>
          {columns.map((column) => (
            <span key={column.key} className={`truncate text-center text-[11px] ${column.selected ? 'font-semibold text-accent' : 'text-ink-soft'}`}>{column.label}</span>
          ))}
        </div>
      </div>
    </figure>
  )
}

/** A small legend chip for severity colours used in a chart. */
export function LegendDot({ color, children }: { color: string; children: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-[11.5px] text-ink-soft">
      <span className="inline-block size-2.5 rounded-sm" style={{ background: color }} aria-hidden="true" />
      {children}
    </span>
  )
}
