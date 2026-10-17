// UI defaults only. The API's captured operation date remains the authority for effective checks and posting.
const kolkata = new Intl.DateTimeFormat('en-CA', {
  timeZone: 'Asia/Kolkata', year: 'numeric', month: '2-digit', day: '2-digit',
})

export function businessDate(instant: Date = new Date()): string {
  const parts = kolkata.formatToParts(instant)
  const part = (type: string) => parts.find((p) => p.type === type)!.value
  return `${part('year')}-${part('month')}-${part('day')}`
}

// A calendar date is not an instant: UTC arithmetic here avoids the browser's timezone/DST.
export function addBusinessDays(date: string, days: number): string {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || !Number.isInteger(days)) throw new Error('A calendar date and whole days are required.')
  const value = new Date(`${date}T00:00:00Z`)
  if (!Number.isFinite(value.getTime()) || value.toISOString().slice(0, 10) !== date) throw new Error('Invalid calendar date.')
  value.setUTCDate(value.getUTCDate() + days)
  return value.toISOString().slice(0, 10)
}