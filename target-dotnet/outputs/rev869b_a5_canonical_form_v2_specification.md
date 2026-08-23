# REV869B A5 canonical form version 2

## 1. Status and scope

This document defines the byte-level canonical form used by the REV869B A5 Purchase action parameters and unsigned plans. It is sufficient for an implementation that does not use .NET or System.Text.Json to reproduce the same bytes.

The canonical form version is the unsigned integer `2`. Every canonical parameter root and every unsigned plan root contains `"canonicalFormVersion":2`.

## 2. Document encoding and structure

- Encode the complete document as UTF-8 without a byte-order mark.
- Emit one JSON value with no leading or trailing bytes.
- Emit no insignificant whitespace, indentation or line breaks.
- Object properties use fixed literal wire names matching `[a-z][A-Za-z0-9]*`.
- The first character is an ASCII lowercase letter. Every remaining character is an ASCII
  letter or digit. Underscore, hyphen, dot, punctuation and non-ASCII are forbidden.
- Emit property names as literal ASCII bytes without `\u` escaping. Production uses a
  code-owned encoder whose allowlist is exactly `a-z`, `A-Z` and `0-9`; it does not use
  a runtime-default encoder. The same encoder instance governs parameter serialization and
  unsigned-plan writing. Contract and manual plan names are statically checked against the
  grammar, and a golden vector rejects any encoder that escapes a permitted character.
- Values do not consult the property-name encoder. All string, enum, identifier, number,
  date and nested canonical-JSON values use the canonical raw-value routines in sections 4
  through 7. Boolean and null tokens contain no encoder-controlled content.
- Sort object properties in ascending ordinal order by property name. For the current ASCII names, this is ascending unsigned UTF-8 byte order.
- Preserve array element order exactly as supplied. Do not sort arrays.
- Emit every nullable property. Emit JSON `null` when it has no value.
- Reject duplicate, unknown, missing-required or incorrectly cased properties.
- Reject comments, trailing commas and inputs deeper than 32 JSON containers.
- A received value is canonical only when parsing and canonical-v2 reserialization reproduce byte-identical input.

The unsigned plan property order is:

1. `actionId`
2. `canonicalFormVersion`
3. `organization`
4. `parameters`
5. `planId`
6. `target`

The `parameters` value is the complete canonical-v2 parameter object, including its own `canonicalFormVersion`.

## 3. Normative action schemas

### 3.1 Schema notation and common rules

The tables below are normative and complete for canonical form v2:

- Position is one-based emitted property order. No unlisted property is permitted.
- `required` means the property must be present and must not be JSON `null`.
- `required, nullable` means the property must be present and may be JSON `null`.
- Every root parameter record contains `canonicalFormVersion` with the unsigned integer value `2`.
- `string` means the canonical string form in section 4. Values are not trimmed, case-folded or rewritten.
- `Guid`, `uint`, `decimal`, `DateTimeOffset`, `DateOnly` and enum types use sections 5 through 7.
- `array<T>` is a required JSON array whose element order is caller order. Elements are
  non-null canonical `T` objects; reject a null element before serialization and after parsing.
- Every `idempotencyKey` is a required string preserved byte-for-byte as logical string content.
- Other than the constraints stated below, A5-1 applies no business validation to free-text strings,
  decimal values, dates, identifiers or array cardinality.

### 3.2 Action-to-schema index

| Action ID | Root schema |
|---|---|
| `RFQ_CREATE` | `A5RfqCreateParameters` |
| `RFQ_VENDOR_INVITE` | `A5RfqVendorInviteParameters` |
| `QUOTATION_REVISION_SUBMIT` | `A5QuotationRevisionSubmitParameters` |
| `QUOTATION_TECHNICAL_VERIFY` | `A5QuotationTechnicalVerifyParameters` |
| `COMPARISON_CREATE` | `A5ComparisonCreateParameters` |
| `COMPARISON_RECOMMEND` | `A5ComparisonRecommendParameters` |
| `COMPARISON_APPROVE` | `A5ComparisonApprovalParameters` |
| `COMPARISON_REJECT` | `A5ComparisonApprovalParameters` |
| `COMPARISON_REVISION_REQUEST` | `A5ComparisonApprovalParameters` |
| `COMPARISON_RESUBMIT` | `A5ComparisonApprovalParameters` |
| `PO_CREATE` | `A5PurchaseOrderCreateParameters` |
| `PO_SUBMIT` | `A5PurchaseOrderSubmitParameters` |
| `PO_ISSUE` | `A5PurchaseOrderIssueParameters` |
| `PO_AMEND` | `A5PurchaseOrderAmendParameters` |
| `PO_REVISE_REJECTED` | `A5PurchaseOrderReviseRejectedParameters` |
| `PO_APPROVE` | `A5PurchaseOrderApprovalParameters` |
| `PO_REJECT` | `A5PurchaseOrderApprovalParameters` |
| `PO_CANCEL` | `A5PurchaseOrderCancelParameters` |
| `MATERIAL_FOLLOW_UP_TRANSITION` | `A5MaterialFollowUpTransitionParameters` |

### 3.3 Nested records

#### `A5RfqSourceLineParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `purchaseRequirementHandoffId` | `Guid` | required | Canonical lowercase D form |
| 2 | `quantity` | `decimal` | required | Canonical decimal form |

#### `A5QuotationLineParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `discountValue` | `decimal` | required | Canonical decimal form |
| 2 | `freight` | `decimal` | required | Canonical decimal form |
| 3 | `hsnSacCode` | `string` | required | Well-formed UTF-16 source |
| 4 | `insurance` | `decimal` | required | Canonical decimal form |
| 5 | `otherCharges` | `decimal` | required | Canonical decimal form |
| 6 | `packingForwarding` | `decimal` | required | Canonical decimal form |
| 7 | `placeOfSupplyStateCode` | `string` | required | Well-formed UTF-16 source |
| 8 | `promisedDeliveryDate` | `DateOnly` | required | Fixed date form |
| 9 | `quantity` | `decimal` | required | Canonical decimal form |
| 10 | `requestForQuotationLineId` | `Guid` | required | Canonical lowercase D form |
| 11 | `roundOff` | `decimal` | required | Canonical decimal form |
| 12 | `supplierStateCode` | `string` | required | Well-formed UTF-16 source |
| 13 | `unitRate` | `decimal` | required | Canonical decimal form |
| 14 | `vendorRegistrationType` | `VendorRegistrationType` | required | Seven-value closed set in section 7 |

### 3.4 Root parameter records

#### `A5RfqCreateParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `currencyCode` | `string` | required | No canonical-layer normalization |
| 3 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 4 | `isSingleSource` | `boolean` | required | `true` or `false` |
| 5 | `lines` | `array<A5RfqSourceLineParameters>` | required | Preserve array order |
| 6 | `quoteDueAt` | `DateTimeOffset` | required | UTC fixed form |
| 7 | `singleSourceJustification` | `string` | required, nullable | Emit explicit `null` when absent |

#### `A5RfqVendorInviteParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 3 | `remarks` | `string` | required | No canonical-layer normalization |
| 4 | `rfqNumber` | `string` | required | No canonical-layer normalization |
| 5 | `rfqVersion` | `uint` | required | Canonical unsigned form |
| 6 | `vendorId` | `Guid` | required | Canonical lowercase D form |

#### `A5QuotationRevisionSubmitParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `attachmentObjectKey` | `string` | required | Attachment rules in section 3.5 |
| 2 | `attachmentSha256` | `string` | required | Attachment rules in section 3.5 |
| 3 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 4 | `currencyCode` | `string` | required | No canonical-layer normalization |
| 5 | `deliveryTerms` | `string` | required | No canonical-layer normalization |
| 6 | `headerDiscountValue` | `decimal` | required | Always emitted, including zero |
| 7 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 8 | `invitationId` | `Guid` | required | Canonical lowercase D form |
| 9 | `invitationVersion` | `uint` | required | Canonical unsigned form |
| 10 | `lateAuthorizationRemarks` | `string` | required, nullable | Emit explicit `null` when absent |
| 11 | `lines` | `array<A5QuotationLineParameters>` | required | Preserve array order |
| 12 | `paymentTerms` | `string` | required | No canonical-layer normalization |
| 13 | `previousQuotationVersion` | `uint` | required, nullable | Emit explicit `null` when absent |
| 14 | `receivedAt` | `DateTimeOffset` | required | UTC fixed form |
| 15 | `requestLateAuthorization` | `boolean` | required | `true` or `false` |
| 16 | `submissionSource` | `A5SubmissionSource` | required | Two-value closed set in section 7 |
| 17 | `vendorAttestation` | `string` | required | No canonical-layer normalization |
| 18 | `vendorQuoteReference` | `string` | required | No canonical-layer normalization |
| 19 | `warrantyTerms` | `string` | required | No canonical-layer normalization |

#### `A5QuotationTechnicalVerifyParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `complianceEvidenceJson` | `string` | required | String content; not parsed as nested JSON |
| 3 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 4 | `isCompliant` | `boolean` | required | `true` or `false` |
| 5 | `quotationNumber` | `string` | required | No canonical-layer normalization |
| 6 | `quotationVersion` | `uint` | required | Canonical unsigned form |
| 7 | `remarks` | `string` | required | No canonical-layer normalization |
| 8 | `vendorQuotationLineId` | `Guid` | required | Canonical lowercase D form |

#### `A5ComparisonCreateParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 3 | `rfqNumber` | `string` | required | No canonical-layer normalization |
| 4 | `rfqVersion` | `uint` | required | Canonical unsigned form |

#### `A5ComparisonRecommendParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `comparisonNumber` | `string` | required | No canonical-layer normalization |
| 3 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 4 | `recommendationRemarks` | `string` | required | No canonical-layer normalization |
| 5 | `singleSourceJustification` | `string` | required, nullable | Emit explicit `null` when absent |
| 6 | `vendorQuotationId` | `Guid` | required | Canonical lowercase D form |
| 7 | `version` | `uint` | required | Canonical unsigned form |

#### `A5ComparisonApprovalParameters`

Used by `COMPARISON_APPROVE`, `COMPARISON_REJECT`, `COMPARISON_REVISION_REQUEST` and
`COMPARISON_RESUBMIT`. The action ID, never the parameter type, selects the action.

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `comparisonNumber` | `string` | required | No canonical-layer normalization |
| 3 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 4 | `remarks` | `string` | required | No canonical-layer normalization |
| 5 | `version` | `uint` | required | Canonical unsigned form |

#### `A5PurchaseOrderCreateParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `comparisonNumber` | `string` | required | No canonical-layer normalization |
| 3 | `comparisonVersion` | `uint` | required | Canonical unsigned form |
| 4 | `idempotencyKey` | `string` | required | Preserved unchanged |

#### `A5PurchaseOrderSubmitParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 3 | `poNumber` | `string` | required | No canonical-layer normalization |
| 4 | `remarks` | `string` | required | No canonical-layer normalization |
| 5 | `version` | `uint` | required | Canonical unsigned form |

#### `A5PurchaseOrderIssueParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 3 | `poNumber` | `string` | required | No canonical-layer normalization |
| 4 | `remarks` | `string` | required | No canonical-layer normalization |
| 5 | `version` | `uint` | required | Canonical unsigned form |

#### `A5PurchaseOrderAmendParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `amendmentReason` | `string` | required | No canonical-layer normalization |
| 2 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 3 | `deliveryTerms` | `string` | required | No canonical-layer normalization |
| 4 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 5 | `paymentTerms` | `string` | required | No canonical-layer normalization |
| 6 | `poNumber` | `string` | required | No canonical-layer normalization |
| 7 | `version` | `uint` | required | Canonical unsigned form |
| 8 | `warrantyTerms` | `string` | required | No canonical-layer normalization |

#### `A5PurchaseOrderReviseRejectedParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `deliveryTerms` | `string` | required | No canonical-layer normalization |
| 3 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 4 | `paymentTerms` | `string` | required | No canonical-layer normalization |
| 5 | `poNumber` | `string` | required | No canonical-layer normalization |
| 6 | `rejectedVersion` | `uint` | required | Canonical unsigned form |
| 7 | `revisionReason` | `string` | required | No canonical-layer normalization |
| 8 | `warrantyTerms` | `string` | required | No canonical-layer normalization |

#### `A5PurchaseOrderApprovalParameters`

Used by `PO_APPROVE` and `PO_REJECT`. The action ID, never the parameter type, selects the action.

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `expectedCurrentVersion` | `uint` | required, nullable | Emit explicit `null` when absent |
| 3 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 4 | `poNumber` | `string` | required | No canonical-layer normalization |
| 5 | `remarks` | `string` | required | No canonical-layer normalization |
| 6 | `version` | `uint` | required | Canonical unsigned form |

#### `A5PurchaseOrderCancelParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 3 | `poNumber` | `string` | required | No canonical-layer normalization |
| 4 | `reason` | `string` | required | No canonical-layer normalization |
| 5 | `version` | `uint` | required | Canonical unsigned form |

#### `A5MaterialFollowUpTransitionParameters`

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 2 | `handoffId` | `Guid` | required | Canonical lowercase D form |
| 3 | `idempotencyKey` | `string` | required | Preserved unchanged |
| 4 | `reason` | `string` | required | No canonical-layer normalization |
| 5 | `toStatus` | `A5MaterialFollowUpTargetStatus` | required | Two-value closed set in section 7 |
| 6 | `version` | `uint` | required | Canonical unsigned form |

### 3.5 Attachment validation

`attachmentObjectKey`:

- length is 1 through 500 UTF-16 code units;
- permitted characters are ASCII `A-Z`, `a-z`, `0-9`, `.`, `_`, `-` and `/`;
- `..` is forbidden anywhere;
- colon and reverse solidus are forbidden;
- a leading solidus or reverse solidus is forbidden;
- these rules reject URI scheme prefixes, Windows drive prefixes, traversal and absolute paths.

`attachmentSha256` is exactly 64 characters, each one an ASCII digit `0-9` or lowercase
hexadecimal letter `a-f`. Uppercase hexadecimal and every other character are forbidden.

### 3.6 Unsigned plan schema

| Position | Wire name | Type | Presence | Constraints |
|---:|---|---|---|---|
| 1 | `actionId` | closed action token | required | One of the 19 action IDs |
| 2 | `canonicalFormVersion` | `uint` | required | Exactly `2` |
| 3 | `organization` | `string` | required | At least one non-whitespace character; not trimmed |
| 4 | `parameters` | action root object | required | Schema selected only by `actionId` |
| 5 | `planId` | `Guid` | required | Nonzero GUID in canonical lowercase D form |
| 6 | `target` | `string` | required | At least one non-whitespace character; not trimmed |

There is no plan-level expected-resource-version property. Request version fields in the selected
parameter schema are the only version source.

## 4. Canonical strings

First validate the source UTF-16 string. Reject an unmatched high surrogate and an unmatched low surrogate. A valid surrogate pair is accepted and encoded as described below. Rejection occurs before signable canonical bytes are returned.

Surround every string value with ASCII quotation mark bytes `0x22`. Process the string from left to right:

| Input | Canonical bytes |
|---|---|
| U+0022 quotation mark | `\"` |
| U+005C reverse solidus | `\\` |
| U+0008 | `\b` |
| U+0009 | `\t` |
| U+000A | `\n` |
| U+000C | `\f` |
| U+000D | `\r` |
| Other U+0000 through U+001F | `\uXXXX`, four uppercase hexadecimal digits |
| U+0020 through U+D7FF except U+0022 and U+005C | Direct UTF-8 |
| U+E000 through U+FFFF | Direct UTF-8 |
| U+10000 through U+10FFFF | Its UTF-16 high and low surrogates, each as `\uXXXX` with uppercase hexadecimal |
| Unmatched UTF-16 surrogate | Reject |

U+0027 apostrophe, U+002F solidus, U+003C less-than, U+003E greater-than and U+0026 ampersand are emitted directly. Canonical bytes therefore are not HTML-safe and must be context-encoded before use in HTML, web-page markup, HTML log viewers or HTML email bodies.

JSON property names are the fixed ASCII literals in the contract and require no escapes.

## 5. Scalar forms

### 5.1 GUID

Emit exactly 36 lowercase ASCII characters in `8-4-4-4-12` D form:

`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

Each `x` is a lowercase hexadecimal digit. Reject uppercase, braces, parentheses, omitted hyphens and all other representations.

### 5.2 Unsigned 32-bit integer

Emit the mathematical value in ASCII base-10:

- permitted range: `0` through `4294967295`;
- no sign;
- no leading zero unless the complete value is `0`;
- no exponent, fraction or whitespace.

### 5.3 Decimal

Emit a JSON number in invariant plain notation:

- no exponent;
- no leading plus sign;
- strip trailing fractional zeroes;
- remove the decimal point when the fractional part becomes empty;
- emit both positive and negative decimal zero as `0`;
- preserve all remaining decimal digits exactly.

Examples: `1.50 -> 1.5`, `3.000 -> 3`, `-0.00 -> 0`.

### 5.4 Boolean and null

Emit only lowercase JSON tokens `true`, `false` and `null`.

## 6. Temporal forms

- `DateTimeOffset`: normalize to UTC and emit exactly `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`.
- `DateOnly`: emit exactly `yyyy-MM-dd`.
- All digits are ASCII.
- Seven fractional-second digits are mandatory for `DateTimeOffset`.

## 7. Closed tokens

Emit closed enum and action identifiers through the canonical string algorithm.

Current closed tokens are case-sensitive:

- Submission source: `EMAIL_RECEIVED`, `PHYSICAL_RECEIVED`.
- Material follow-up target: `InProgress`, `Completed`.
- Vendor registration type: `REGULAR`, `COMPOSITION`, `UNREGISTERED`, `SEZ`, `OVERSEAS`, `DEEMED_EXPORT`, `UIN`.
- Purchase action identifiers are the 19 literals declared by `A5PurchaseActionId`.

No trimming, aliases, case folding or legacy synonyms are permitted.

## 8. Plan hashing

Construct the complete unsigned plan using sections 2 through 7. Compute SHA-256 over those exact UTF-8 bytes. Represent the plan hash as 64 lowercase hexadecimal ASCII characters.

The hash covers the nested canonical parameter bytes and both canonical-form version fields. Any byte difference changes the hash.

## 9. Versioning rule

Any change to string escaping, scalar formatting, property names, property order, array treatment, temporal formats, null emission or root structure requires a new canonical-form version and new golden vectors. Canonical form version 2 must never be silently redefined.
