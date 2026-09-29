// Contracts from SESS.NexaERP.Application/Stores/InventoryPeriodContracts.cs.
// Wire is PascalCase. DateOnly travels as "yyyy-MM-dd"; DateTimeOffset as an
// ISO instant.

/** advance.financial_periods."Status" for PeriodType INVENTORY. */
export type InventoryPeriodStatus = 'OPEN' | 'CLOSED'

/** advance.inventory_period_events."Action". */
export type InventoryPeriodAction = 'OPENED' | 'CLOSED'

export interface InventoryPeriodDecisionView {
  Id: string
  Action: InventoryPeriodAction | string
  PeriodVersion: number
  ActorEmployeeId: string
  RoleAssignmentId: string
  RoleAssignmentType: string
  Reason: string
  RecordedAt: string
  RecordedBy: string
}

export interface InventoryPeriodView {
  Id: string
  Code: string
  Name: string
  /** Inclusive, yyyy-MM-dd. */
  StartDate: string
  /** Inclusive, yyyy-MM-dd. */
  EndDate: string
  Status: InventoryPeriodStatus | string
  Version: number
  CreatedAt: string
  CreatedBy: string
  ClosedAt: string | null
  Decisions: InventoryPeriodDecisionView[]
  /** True when the server returned the retained result of an earlier identical command. */
  Replayed?: boolean
}

/** POST / — OpenInventoryPeriodRequest. */
export interface OpenInventoryPeriodRequest {
  /** 1-30 characters; the server upper-cases it. */
  Code: string
  /** 1-150 characters. */
  Name: string
  StartDate: string
  EndDate: string
  /** 1-2000 characters. */
  Reason: string
  /** 1-100 characters. */
  IdempotencyKey: string
}

/** POST /{id}/close — CloseInventoryPeriodRequest. */
export interface CloseInventoryPeriodRequest {
  /** The period's current Version, as last read. */
  Version: number
  Reason: string
  IdempotencyKey: string
}
