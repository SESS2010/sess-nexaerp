/** UI intent only: canonical employee, role, company and independent duties are checked by the server. */
export function openingValueReviewBody(version: number, reason: string, independentMd: boolean, absenceConfirmed: boolean) {
  if (!reason.trim()) throw new Error('A value-review reason is required.')
  if (independentMd && !absenceConfirmed) throw new Error('Confirm ALFATHIMA is unavailable before independent MD review.')
  return { Version: version, Reason: reason.trim(), ...(independentMd ? { IndependentMdReview: true } : {}) }
}