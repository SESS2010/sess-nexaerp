// Lifecycle of one RFQ / quotation / comparison screen instance (Defect #2
// review of c673b01, required changes 1, 2 and 4). The screens and the tests
// drive the same transitions: pick a target, change company/login, unmount.
// An action captures isLive() when it starts; that turns false on unmount, on
// a scope change, or when the target it works on is replaced by another one,
// so its continuation (POST, result, reload) is dropped.
import { RequestGate } from './quotationDraft.ts'

export class ScreenLifecycle {
  private epoch = 0
  private scopeEpoch = 0
  private mounted = true
  private readonly targets = new Map<string, { key: string; epoch: number }>()
  private readonly gates: RequestGate[] = []
  /** Busy flag per action slot, with the token of the action that owns it. */
  private readonly busy = new Map<string, { owner: number; set: (busy: boolean) => void }>()

  /**
   * Binds a slot's busy flag (a React state setter). The flag belongs to one
   * action at a time: a target change, a scope change or unmount releases it,
   * so the next action can start without a reload.
   */
  bindBusy(slot: string, set: (busy: boolean) => void): this {
    const current = this.busy.get(slot)
    if (current) current.set = set
    else this.busy.set(slot, { owner: 0, set })
    return this
  }

  /**
   * Starts one action in a slot: sets its busy flag and returns isLive (scope,
   * target and busy ownership) and finish (releases the flag only while this
   * action still owns it, so a late old finish never frees a newer action).
   */
  startAction(slot: string): { isLive: () => boolean; finish: () => void } {
    const targetLive = this.begin(slot)
    const entry = this.busy.get(slot)
    const owner = ++this.epoch
    if (entry) {
      entry.owner = owner
      entry.set(true)
    }
    return {
      isLive: () => targetLive() && (!entry || entry.owner === owner),
      finish: () => {
        if (entry && entry.owner === owner) {
          entry.owner = 0
          entry.set(false)
        }
      },
    }
  }
  /** Request gates reset (late reads dropped) on every scope change and on unmount. */
  track(...gates: RequestGate[]): this {
    this.gates.push(...gates)
    return this
  }

  /** Mount (again — React StrictMode mounts twice in development). */
  mount(): void {
    this.mounted = true
  }

  /** Navigation away / logout: every action and read of this instance is dropped. */
  unmount(): void {
    this.mounted = false
    this.invalidate()
  }

  /** Company or login changed: drop actions, reads and targets. */
  changeScope(): void {
    this.invalidate()
    this.targets.clear()
  }

  /**
   * The target an action slot works on (invitation, quotation + line, chosen
   * winner, vendor). Returns true only for a different target; the same key
   * again (a refresh) keeps in-flight actions and their retry keys valid.
   */
  setTarget(slot: string, key: string): boolean {
    const current = this.targets.get(slot)
    if (current && current.key === key) return false
    this.targets.set(slot, { key, epoch: ++this.epoch })
    // The action on the previous target is cancelled: free its button.
    this.releaseBusy(slot)
    return true
  }

  targetOf(slot: string): string {
    return this.targets.get(slot)?.key ?? ''
  }

  /** isLive for one action, captured when it starts (optionally bound to a target slot). */
  begin(slot?: string): () => boolean {
    const scope = this.scopeEpoch
    const target = slot ? this.targets.get(slot)?.epoch : undefined
    return () => this.mounted && this.scopeEpoch === scope && (!slot || this.targets.get(slot)?.epoch === target)
  }

  private invalidate(): void {
    this.scopeEpoch = ++this.epoch
    for (const gate of this.gates) gate.reset()
    for (const slot of this.busy.keys()) this.releaseBusy(slot)
  }

  private releaseBusy(slot: string): void {
    const entry = this.busy.get(slot)
    if (!entry || entry.owner === 0) return
    entry.owner = 0
    entry.set(false)
  }
}

/** Target slots used by the screens. */
export const TARGET = {
  invitation: 'invitation',
  verification: 'verification',
  winner: 'winner',
  invite: 'invite',
  /** Comparison approve / reject / request-revision / resubmit (no target to change). */
  approval: 'approval',
} as const

/**
 * Quotation screen: which invitation the typed header/evidence belongs to.
 * Vendor-bound evidence never travels to a different invitation; a refresh of
 * the same invitation keeps the draft. Typing before any invitation is chosen
 * belongs to the first one picked.
 */
export class QuotationScreenController {
  readonly lifecycle: ScreenLifecycle
  private evidenceOwner = ''

  constructor(lifecycle: ScreenLifecycle = new ScreenLifecycle()) {
    this.lifecycle = lifecycle
  }

  /** Picks (or clears) the invitation. Returns true when the header/evidence must be reset. */
  pickInvitation(invitationId: string): boolean {
    this.lifecycle.setTarget(TARGET.invitation, invitationId)
    if (!invitationId) return false
    const reset = this.evidenceOwner !== '' && this.evidenceOwner !== invitationId
    this.evidenceOwner = invitationId
    return reset
  }

  /** The quotation and line being verified; changing either drops an in-flight verification. */
  pickVerification(quotationNumber: string, lineId: string): void {
    this.lifecycle.setTarget(TARGET.verification, `${quotationNumber}\n${lineId}`)
  }

  changeScope(): void {
    this.lifecycle.changeScope()
    this.evidenceOwner = ''
  }

  submitIsLive(): () => boolean {
    return this.lifecycle.begin(TARGET.invitation)
  }

  verifyIsLive(): () => boolean {
    return this.lifecycle.begin(TARGET.verification)
  }

  /** Starts a quotation submit: owns the "Recording…" flag until finish or a different invitation. */
  startSubmit(): { isLive: () => boolean; finish: () => void } {
    return this.lifecycle.startAction(TARGET.invitation)
  }

  /** Starts a technical verification: owns its busy flag until finish or another quotation/line. */
  startVerify(): { isLive: () => boolean; finish: () => void } {
    return this.lifecycle.startAction(TARGET.verification)
  }
}
