/**
 * Client-side format and checksum validation for Indian tax identifiers.
 *
 * - GSTIN (Goods and Services Tax Identification Number): 15 characters.
 *     positions  1-2  : state code (01..38, or 97 / 99 for special registrations)
 *     positions  3-12 : PAN of the registered entity
 *     position   13   : entity / registration sequence within the state (1-9, A-Z)
 *     position   14   : literal "Z" (reserved)
 *     position   15   : check character computed from the first 14 characters
 * - PAN (Permanent Account Number): 10 characters.
 *     positions  1-3  : alphabetic series
 *     position   4    : holder type (A B C F G H L J P T)
 *     position   5    : first letter of surname / entity name
 *     positions  6-9  : sequential digits
 *     position   10   : alphabetic check letter (not verifiable client-side)
 *
 * These helpers only decide whether a value is *well-formed*. Whether a GSTIN
 * or PAN actually exists is the concern of the government registry, not of
 * this module. Nothing here calls the backend.
 */

/** Structural GSTIN pattern (no checksum). Two-digit state code, 10-char PAN, entity code, literal Z, check char. */
export const GSTIN_REGEX = /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$/;

/** Structural PAN pattern (holder type not enforced here; see isValidPan). */
export const PAN_REGEX = /^[A-Z]{5}[0-9]{4}[A-Z]$/;

/** Base-36 alphabet used by the GSTIN check-character algorithm. */
const GSTIN_ALPHABET = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ';

/** Allowed 4th-character holder types in a PAN. */
const PAN_HOLDER_TYPES = 'ABCFGHLJPT';

/** Short helper text for form fields. */
export const GSTIN_HINT =
  '15 characters: 2-digit state code, 10-character PAN, entity code, "Z", check character (e.g. 27AAPFU0939F1ZV).';

/** Short helper text for form fields. */
export const PAN_HINT =
  '10 characters: 5 letters, 4 digits, 1 letter (e.g. AAPFU0939F). The 4th letter is the holder type (A B C F G H L J P T).';

/** Trim surrounding whitespace and upper-case so comparisons and regex tests are case-insensitive. */
export function normalizeGstin(v: string): string {
  return (v ?? '').trim().toUpperCase();
}

/** Trim surrounding whitespace and upper-case so comparisons and regex tests are case-insensitive. */
export function normalizePan(v: string): string {
  return (v ?? '').trim().toUpperCase();
}

/**
 * True when the two-digit state code is one the GST portal issues.
 * 01..38 are state / UT codes; 97 is "Other Territory"; 99 is "Centre Jurisdiction".
 */
function isValidGstStateCode(code: string): boolean {
  if (!/^[0-9]{2}$/.test(code)) return false;
  const n = Number(code);
  return (n >= 1 && n <= 38) || n === 97 || n === 99;
}

/**
 * Compute the expected GSTIN check character for the first 14 characters.
 *
 * Standard algorithm: each character is mapped to its base-36 value, multiplied
 * by an alternating weight (1, 2, 1, 2, ...), and the quotient and remainder of
 * product / 36 are summed. The check value is (36 - sum % 36) % 36, mapped back
 * through the same alphabet.
 *
 * Returns null when the input is not exactly 14 characters of the alphabet.
 */
export function computeGstinCheckChar(first14: string): string | null {
  if (first14.length !== 14) return null;
  let sum = 0;
  for (let i = 0; i < 14; i++) {
    const value = GSTIN_ALPHABET.indexOf(first14[i]);
    if (value < 0) return null;
    const weight = i % 2 === 0 ? 1 : 2;
    const product = value * weight;
    sum += Math.floor(product / 36) + (product % 36);
  }
  const check = (36 - (sum % 36)) % 36;
  return GSTIN_ALPHABET[check];
}

/**
 * Full GSTIN validation: normalises, checks the structural pattern, checks the
 * state code, and verifies the check character.
 */
export function isValidGstin(v: string): boolean {
  const gstin = normalizeGstin(v);
  if (!GSTIN_REGEX.test(gstin)) return false;
  if (!isValidGstStateCode(gstin.slice(0, 2))) return false;
  const expected = computeGstinCheckChar(gstin.slice(0, 14));
  return expected !== null && expected === gstin[14];
}

/**
 * PAN validation: normalises, checks the structural pattern, and requires the
 * 4th character to be a recognised holder type.
 */
export function isValidPan(v: string): boolean {
  const pan = normalizePan(v);
  if (!PAN_REGEX.test(pan)) return false;
  return PAN_HOLDER_TYPES.includes(pan[3]);
}

/**
 * True when both values are present and the PAN embedded in the GSTIN
 * (characters 3-12) does not match the supplied PAN. Empty inputs never
 * count as a mismatch so a form can leave either field blank.
 */
export function gstinPanMismatch(gstin: string, pan: string): boolean {
  const g = normalizeGstin(gstin);
  const p = normalizePan(pan);
  if (!g || !p) return false;
  return g.slice(2, 12) !== p;
}
