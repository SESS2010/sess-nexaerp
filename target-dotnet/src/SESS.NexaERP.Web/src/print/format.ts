// Formatting for printed documents: Indian digit grouping, date-only values,
// instants in IST, and amounts in words (lakh / crore).

const money = new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const quantity = new Intl.NumberFormat('en-IN', { maximumFractionDigits: 3 })

/** 123456.7 → '1,23,456.70' */
export function formatMoney(value: number): string {
  return money.format(value)
}

export function formatQuantity(value: number): string {
  return quantity.format(value)
}

/** Percent without trailing zeros: 9 → '9%', 2.5 → '2.5%'. */
export function formatPercent(value: number): string {
  return `${Number(value.toFixed(2))}%`
}

/** 'yyyy-MM-dd' → 'dd-MM-yyyy'. Parsed as text so no time zone can shift the day. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  return match ? `${match[3]}-${match[2]}-${match[1]}` : value
}

const istFormat = new Intl.DateTimeFormat('en-IN', {
  timeZone: 'Asia/Kolkata',
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hour12: true,
})

/** Any ISO instant (Z or +05:30) → '24-09-2026, 02:45 PM IST'. */
export function formatInstantIst(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  const parts = Object.fromEntries(istFormat.formatToParts(date).map((p) => [p.type, p.value]))
  return `${parts.day}-${parts.month}-${parts.year}, ${parts.hour}:${parts.minute} ${String(parts.dayPeriod).toUpperCase()} IST`
}

/** Rounds to paise without binary drift (1.005 → 1.01). */
export function roundMoney(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100
}

const ONES = [
  '', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten',
  'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen', 'Seventeen', 'Eighteen', 'Nineteen',
]
const TENS = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety']

function belowHundred(n: number): string {
  if (n < 20) return ONES[n]
  return TENS[Math.floor(n / 10)] + (n % 10 ? ` ${ONES[n % 10]}` : '')
}

function belowThousand(n: number): string {
  const hundreds = Math.floor(n / 100)
  const rest = n % 100
  return [hundreds ? `${ONES[hundreds]} Hundred` : '', rest ? belowHundred(rest) : ''].filter(Boolean).join(' ')
}

/** Whole number in the Indian system: 12345678 → 'One Crore Twenty Three Lakh Forty Five Thousand Six Hundred Seventy Eight'. */
export function integerToIndianWords(n: number): string {
  if (n === 0) return 'Zero'
  const crore = Math.floor(n / 10_000_000)
  const lakh = Math.floor((n % 10_000_000) / 100_000)
  const thousand = Math.floor((n % 100_000) / 1000)
  const rest = n % 1000
  return [
    crore ? `${integerToIndianWords(crore)} Crore` : '',
    lakh ? `${belowHundred(lakh)} Lakh` : '',
    thousand ? `${belowHundred(thousand)} Thousand` : '',
    rest ? belowThousand(rest) : '',
  ].filter(Boolean).join(' ')
}

/** 123456.7 → 'Rupees One Lakh Twenty Three Thousand Four Hundred Fifty Six and Seventy Paise Only' */
export function amountInWords(value: number): string {
  const paiseTotal = Math.round(Math.abs(value) * 100)
  const rupees = Math.floor(paiseTotal / 100)
  const paise = paiseTotal % 100
  const words = `Rupees ${integerToIndianWords(rupees)}${paise ? ` and ${belowHundred(paise)} Paise` : ''} Only`
  return value < 0 ? `Minus ${words}` : words
}
