/** In-memory key for one logical approval; never persist identity or payload. */
export class ItemActionIntent {
  private fingerprint: string | undefined
  private key: string | undefined
  constructor(private readonly newKey = () => `item-approve-${crypto.randomUUID()}`) {}
  keyFor(parts: readonly unknown[]): string {
    const fingerprint = JSON.stringify(parts)
    if (fingerprint !== this.fingerprint || !this.key) {
      this.fingerprint = fingerprint
      this.key = this.newKey()
    }
    return this.key
  }
  clear(): void { this.fingerprint = undefined; this.key = undefined }
}
