// Address lines for printed documents.
//
// Masters and the company profile often carry the city and PIN inside the
// free-text address as well ("..., Coimbatore - 641 021"). Appending the
// separate City / PinCode fields again printed the PIN twice (F27), so the
// "city – pin" line keeps only the parts the address text does not already
// show.

import type { PrintAddress } from './types'

function digitsOnly(value: string): string {
  return value.replace(/\D/g, '')
}

/** Whether the PIN already appears in the text, ignoring spaces ("641 021" = "641021"). */
export function containsPin(text: string, pin: string): boolean {
  const wanted = digitsOnly(pin)
  if (!wanted) return false
  // Each digit of the PIN in order, at most one space between them, and no
  // digit either side: "641 021" matches, "Plot 641" and "6410219" do not.
  return new RegExp(`(?<![0-9])${wanted.split('').join(' ?')}(?![0-9])`).test(text)
}

function containsWord(text: string, word: string): boolean {
  const escaped = word.trim().replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  return escaped !== '' && new RegExp(`(^|[^A-Za-z])${escaped}([^A-Za-z]|$)`, 'i').test(text)
}

/** Address lines; the "city – pin" line only with the parts the lines do not already show. */
export function addressLines(address: PrintAddress): string[] {
  const text = address.lines.join(' ')
  const city = address.city && !containsWord(text, address.city) ? address.city : ''
  const pin = address.pin && !containsPin(text, address.pin) ? address.pin : ''
  const cityPin = [city, pin].filter(Boolean).join(' – ')
  return cityPin ? [...address.lines, cityPin] : [...address.lines]
}
