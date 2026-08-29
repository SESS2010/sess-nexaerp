# Trial master data for frontend development

This is a standalone development seed, not a migration. Trial rows must never be part of a customer deployment or promoted into another database. Every seeded row has `CreatedBy = 'TRIAL_DATA'`, and every business code begins `TRIAL-`.

The script is intentionally re-runnable and transactional. Applying it resets only rows carrying both trial markers. Removal follows foreign-key order and does not cascade. If operational records reference trial masters, removal fails and rolls back so those operational records can be removed explicitly first.

## Safety gate

The wrapper requires all of the following:

- `DOTNET_ENVIRONMENT=Development`
- `NexaErp__AllowTrialData=true`
- `PGDATABASE` exactly equal to `NexaErp__ExpectedDatabase`
- explicit `PGHOST`, `PGPORT`, and `PGUSER`
- a PostgreSQL 17+ migrated `advance` database
- none of the four managed production principals present

Use libpq's `PGPASSWORD` environment variable when password authentication is required. Never put a password on the command line.

## Apply

```powershell
$env:DOTNET_ENVIRONMENT='Development'
$env:NexaErp__AllowTrialData='true'
$env:PGHOST='127.0.0.1'
$env:PGPORT='5432'
$env:PGDATABASE='your_exact_development_database'
$env:PGUSER='postgres'
$env:NexaErp__ExpectedDatabase=$env:PGDATABASE
& .\tools\trial-master-data.ps1 -Action Apply
```

Expected rows are 6 UOMs, 6 categories, 4 subcategories, 5 manufacturers, 15 vendors, 20 items, 2 warehouses, and 22 rack bins. The rack-bin total is 10 general bins (five in each warehouse) plus 12 QC bins (one for each of six categories in each company): 80 rows overall.

All UOMs use the API's canonical six-decimal quantity precision. `NOS`, `SET`, and `LOT` are count units; `KG`, `MTR`, and `LTR` use `MASS`, `LENGTH`, and `VOLUME` respectively.

## Remove in one command

With the same process-only environment variables set:

```powershell
& .\tools\trial-master-data.ps1 -Action Remove
```

The exact foreign-key order is rack bins, warehouses, items, vendors, subcategories, manufacturers, categories, then UOMs. A successful removal verifies that zero marked rows remain.
