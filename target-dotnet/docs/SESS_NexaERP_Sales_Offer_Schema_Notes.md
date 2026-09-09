# Deferred Sales offer schema notes

The Sales offer is not the Estimated BOM and its selling price must never be derived from the Estimated BOM total. The current material variance answers whether Design estimated material cost correctly. Future offer-versus-actual variance answers whether the job was commercially successful and remains deferred until Sales is implemented.

A future offer aggregate must link to the Customer PO and therefore to each one-machine Job Order without rewriting either frozen BOM baseline. It must support three distinct offer kinds:

- `FINISHED_GOODS`: a chamber or other finished machine, priced from experience and its BOM.
- `SPARE`: parts priced from the company item register's most recent accepted landed purchase rate plus a governed margin.
- `SERVICE`: manpower and engineer charges; material is not required.

The offer must retain immutable submitted revisions, currency/rate evidence where applicable, selling-price components and the accepted customer version. Actual cost continues to come from generated Actual BOM accepted-bill allocations. Adding an offer baseline later must add a third commercial projection; it must not repurpose `EstimatedUnitValue` or the Estimated BOM total as revenue.