# REV869B A5 canonical form version 2

## 1. Status and scope

This document defines the byte-level canonical form used by the REV869B A5 Purchase action parameters and unsigned plans. It is sufficient for an implementation that does not use .NET or System.Text.Json to reproduce the same bytes.

The canonical form version is the unsigned integer `2`. Every canonical parameter root and every unsigned plan root contains `"canonicalFormVersion":2`.

## 2. Document encoding and structure

- Encode the complete document as UTF-8 without a byte-order mark.
- Emit one JSON value with no leading or trailing bytes.
- Emit no insignificant whitespace, indentation or line breaks.
- Object properties use the fixed literal wire names declared by the contract.
- Every current property name is ASCII.
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

## 3. Canonical strings

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

## 4. Scalar forms

### 4.1 GUID

Emit exactly 36 lowercase ASCII characters in `8-4-4-4-12` D form:

`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

Each `x` is a lowercase hexadecimal digit. Reject uppercase, braces, parentheses, omitted hyphens and all other representations.

### 4.2 Unsigned 32-bit integer

Emit the mathematical value in ASCII base-10:

- permitted range: `0` through `4294967295`;
- no sign;
- no leading zero unless the complete value is `0`;
- no exponent, fraction or whitespace.

### 4.3 Decimal

Emit a JSON number in invariant plain notation:

- no exponent;
- no leading plus sign;
- strip trailing fractional zeroes;
- remove the decimal point when the fractional part becomes empty;
- emit both positive and negative decimal zero as `0`;
- preserve all remaining decimal digits exactly.

Examples: `1.50 -> 1.5`, `3.000 -> 3`, `-0.00 -> 0`.

### 4.4 Boolean and null

Emit only lowercase JSON tokens `true`, `false` and `null`.

## 5. Temporal forms

- `DateTimeOffset`: normalize to UTC and emit exactly `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`.
- `DateOnly`: emit exactly `yyyy-MM-dd`.
- All digits are ASCII.
- Seven fractional-second digits are mandatory for `DateTimeOffset`.

## 6. Closed tokens

Emit closed enum and action identifiers through the canonical string algorithm.

Current closed tokens are case-sensitive:

- Submission source: `EMAIL_RECEIVED`, `PHYSICAL_RECEIVED`.
- Material follow-up target: `InProgress`, `Completed`.
- Vendor registration type: `REGULAR`, `COMPOSITION`, `UNREGISTERED`, `SEZ`, `OVERSEAS`, `DEEMED_EXPORT`, `UIN`.
- Purchase action identifiers are the 19 literals declared by `A5PurchaseActionId`.

No trimming, aliases, case folding or legacy synonyms are permitted.

## 7. Plan hashing

Construct the complete unsigned plan using sections 2 through 6. Compute SHA-256 over those exact UTF-8 bytes. Represent the plan hash as 64 lowercase hexadecimal ASCII characters.

The hash covers the nested canonical parameter bytes and both canonical-form version fields. Any byte difference changes the hash.

## 8. Versioning rule

Any change to string escaping, scalar formatting, property names, property order, array treatment, temporal formats, null emission or root structure requires a new canonical-form version and new golden vectors. Canonical form version 2 must never be silently redefined.

