// Formatting for the dashboards. Deliberately separate from the register
// helpers in features/purchase: the dashboards must never turn a null
// (withheld or unknown) amount into a formatted zero, so these formatters take
// numbers only and the null case is rendered by the <Money> component.

const moneyFormatters = new Map<string, Intl.NumberFormat>()

/** One native-currency amount, e.g. "₹4,999.99" or "US$1,250.50". Never converts currencies. */
export function formatAmount(value: number, currency: string): string {
  let formatter = moneyFormatters.get(currency)
  if (!formatter) {
    try {
      formatter = new Intl.NumberFormat('en-IN', { style: 'currency', currency, minimumFractionDigits: 2, maximumFractionDigits: 2 })
    } catch {
      // Not an ISO code Intl knows: show the code the server sent, unconverted.
      formatter = new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
      moneyFormatters.set(currency, formatter)
      return `${currency} ${formatter.format(value)}`
    }
    moneyFormatters.set(currency, formatter)
  }
  return formatter.format(value)
}

const quantityFormatter = new Intl.NumberFormat('en-IN', { maximumFractionDigits: 3 })

export function formatQuantity(value: number): string {
  return quantityFormatter.format(value)
}

const countFormatter = new Intl.NumberFormat('en-IN')

export function formatCount(value: number): string {
  return countFormatter.format(value)
}

/** "1 doc", "3 docs": a count with its noun in the right number. */
export function countOf(value: number, singular: string, plural = `${singular}s`): string {
  return `${formatCount(value)} ${value === 1 ? singular : plural}`
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

/** DateOnly (YYYY-MM-DD) is already a company-local calendar date: no time-zone shift. */
export function formatDateOnly(value: string): string {
  const [year, month, day] = value.split('-').map(Number)
  if (!year || !month || !day) return value
  return `${day} ${MONTHS[month - 1]} ${year}`
}

/** "Sep 2026" for a monthly bucket key or first-of-month DateOnly. */
export function formatMonth(value: string): string {
  const [year, month] = value.split('-').map(Number)
  if (!year || !month) return value
  return `${MONTHS[month - 1]} ${year}`
}

/** Server timestamp shown in the company reporting time zone the server named. */
export function formatTimestamp(value: string, timeZone: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  try {
    return new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short', timeZone }).format(date)
  } catch {
    return new Intl.DateTimeFormat('en-IN', { dateStyle: 'medium', timeStyle: 'short' }).format(date)
  }
}

export function formatAge(days: number | null): string | null {
  if (days === null) return null
  return days === 1 ? '1 day' : `${formatCount(days)} days`
}
