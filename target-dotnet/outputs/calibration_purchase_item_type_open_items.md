# Calibration, Purchase Pair, and Item Type Follow-up

## Open item: legacy item classification

The advance.items table is empty at migration design time. The 2,870 historical items remain in the frozen
legacy_import.items REV848 snake_case schema and are outside this migration.

The future legacy_import to advance data migration must:

1. inventory every distinct legacy MaterialType value and its count;
2. obtain explicit approval for the mapping to RAW_MATERIAL, COMPONENT, CONSUMABLE, SPARE,
   FINISHED_MACHINE, TOOL, SERVICE_ITEM, or NON_STOCK;
3. populate ItemType and set IsReturnable=true exactly for TOOL;
4. reject unmapped or ambiguous values rather than guessing; and
5. retire MaterialType only in a later separately approved migration after all application consumers have moved.

This follow-up migration deliberately leaves MaterialType unchanged and performs no legacy data access or copy.
