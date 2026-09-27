using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// R10 (approved 26 September 2026): each company's legal identity (legal name, GSTIN, PAN, address,
/// state code) is stored in the database. It feeds the GST state rule (R2) and every print. A
/// warehouse may carry its own state code, because the place of supply is the delivery location;
/// a warehouse without one uses its company's state.
///
/// The values are entered through the governed, Technical-Director-only profile endpoint; this
/// migration creates no company data. GSTIN, PAN and state code are checked for form and for
/// consistency with each other.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260926110000_CompanyProfileAndWarehouseState")]
public sealed class CompanyProfileAndWarehouseState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            CREATE TABLE advance.company_profiles(
              "CompanyId" uuid PRIMARY KEY REFERENCES advance.companies("Id"),
              "LegalName" text NOT NULL CHECK(length(btrim("LegalName")) BETWEEN 1 AND 200),
              "TradeName" text CHECK("TradeName" IS NULL OR length(btrim("TradeName")) BETWEEN 1 AND 200),
              "Gstin" text NOT NULL CHECK("Gstin" ~ '^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$'),
              "Pan" text NOT NULL CHECK("Pan" ~ '^[A-Z]{5}[0-9]{4}[A-Z]$'),
              "StateCode" text NOT NULL CHECK("StateCode" ~ '^[0-9]{2}$'),
              "State" text NOT NULL CHECK(length(btrim("State")) BETWEEN 1 AND 100),
              "AddressLine1" text NOT NULL CHECK(length(btrim("AddressLine1")) BETWEEN 1 AND 200),
              "AddressLine2" text CHECK("AddressLine2" IS NULL OR length("AddressLine2")<=200),
              "City" text NOT NULL CHECK(length(btrim("City")) BETWEEN 1 AND 100),
              "PinCode" text NOT NULL CHECK("PinCode" ~ '^[1-9][0-9]{5}$'),
              "Phone" text CHECK("Phone" IS NULL OR length("Phone")<=40),
              "Email" text CHECK("Email" IS NULL OR length("Email")<=200),
              "Version" bigint NOT NULL DEFAULT 0,
              "UpdatedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
              "UpdatedBy" text NOT NULL,
              CONSTRAINT "CK_company_profiles_gstin_state" CHECK(substring("Gstin" from 1 for 2)="StateCode"),
              CONSTRAINT "CK_company_profiles_gstin_pan" CHECK(substring("Gstin" from 3 for 10)="Pan"));
            CREATE TABLE advance.warehouse_state_codes(
              "WarehouseId" uuid PRIMARY KEY REFERENCES advance.warehouses("Id"),
              "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
              "StateCode" text NOT NULL CHECK("StateCode" ~ '^[0-9]{2}$'),
              "Version" bigint NOT NULL DEFAULT 0,
              "UpdatedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
              "UpdatedBy" text NOT NULL);
            DO $acl$ BEGIN
              IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_runtime') THEN
                EXECUTE 'GRANT SELECT,INSERT,UPDATE ON advance.company_profiles,advance.warehouse_state_codes TO nexa_erp_runtime';
              END IF;
            END $acl$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.company_profiles) OR EXISTS(SELECT 1 FROM advance.warehouse_state_codes) THEN
                RAISE EXCEPTION 'Company profile rollback refuses retained company legal details.';
              END IF;
            END $guard$;
            DROP TABLE IF EXISTS advance.warehouse_state_codes;
            DROP TABLE IF EXISTS advance.company_profiles;
            """);
    }
}
