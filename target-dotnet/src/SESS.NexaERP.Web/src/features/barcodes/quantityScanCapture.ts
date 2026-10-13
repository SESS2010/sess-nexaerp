/** Keyboard wedge capture for an opted-in quantity field. No clock-based scan guessing. */
export class QuantityScanCapture {
  private buffer = ''
  reset(): void { this.buffer = '' }
  key(key: string, button = false): { prevent: boolean; scan?: string } {
    if (key === 'Enter' || key === 'Tab') {
      if (button && !this.buffer) return { prevent: false }
      const scan = this.buffer
      this.reset()
      return { prevent: true, ...(scan ? { scan } : {}) }
    }
    if (button && key === ' ' && !this.buffer) return { prevent: false }
    if (key.length === 1) { this.buffer += key; return { prevent: true } }
    if (key === 'Backspace') { this.buffer = this.buffer.slice(0, -1); return { prevent: true } }
    if (key === 'Escape') this.reset()
    return { prevent: false }
  }
}