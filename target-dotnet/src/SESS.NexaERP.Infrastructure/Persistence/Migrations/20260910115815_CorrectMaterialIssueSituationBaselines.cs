using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    public partial class CorrectMaterialIssueSituationBaselines : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql("""
                DO $guard$
                DECLARE definition text;
                BEGIN
                  IF to_regclass('advance.material_issue_requests') IS NULL THEN
                    RAISE EXCEPTION 'Material issue request foundation is missing.';
                  END IF;
                  SELECT pg_get_constraintdef(oid) INTO definition
                    FROM pg_constraint
                    WHERE conrelid='advance.material_issue_requests'::regclass
                      AND conname='CK_mir_situation';
                  IF definition IS NULL OR position('CHAMBER_MANUFACTURE' in definition)=0
                     OR position('SERVICE_CUSTOMER_PO' in definition)=0
                     OR position('SITE_PROJECT_PO' in definition)=0
                     OR position('CONSUMABLE_OFFICE' in definition)=0
                     OR position('SPARE_SALE' in definition)>0 THEN
                    RAISE EXCEPTION 'Material issue situation constraint is absent or not at the expected predecessor contract.';
                  END IF;
                END $guard$;

                ALTER TABLE advance.material_issue_requests DROP CONSTRAINT "CK_mir_situation";
                ALTER TABLE advance.material_issue_requests ADD CONSTRAINT "CK_mir_situation"
                  CHECK (("Situation" IN ('CHAMBER_MANUFACTURE','SERVICE_CUSTOMER_PO','SITE_PROJECT_PO')
                           AND "DestinationType"='JOB_ORDER' AND "JobOrderId" IS NOT NULL)
                      OR ("Situation"='SPARE_SALE' AND "DestinationType"='CUSTOMER'
                           AND "JobOrderId" IS NULL AND "CustomerId" IS NOT NULL)
                      OR ("Situation"='CONSUMABLE_OFFICE' AND "DestinationType" IN ('DEPARTMENT','OTHER')
                           AND "JobOrderId" IS NULL));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql("""
                DO $guard$
                DECLARE definition text;
                BEGIN
                  IF to_regclass('advance.material_issue_requests') IS NULL THEN
                    RAISE EXCEPTION 'Material issue request foundation is missing.';
                  END IF;
                  IF EXISTS (SELECT 1 FROM advance.material_issue_requests WHERE "Situation"='SPARE_SALE') THEN
                    RAISE EXCEPTION 'Refusing rollback: SPARE_SALE material issue evidence exists.';
                  END IF;
                  SELECT pg_get_constraintdef(oid) INTO definition
                    FROM pg_constraint
                    WHERE conrelid='advance.material_issue_requests'::regclass
                      AND conname='CK_mir_situation';
                  IF definition IS NULL OR position('SPARE_SALE' in definition)=0 THEN
                    RAISE EXCEPTION 'Material issue situation constraint is absent or not at the expected successor contract.';
                  END IF;
                END $guard$;

                ALTER TABLE advance.material_issue_requests DROP CONSTRAINT "CK_mir_situation";
                ALTER TABLE advance.material_issue_requests ADD CONSTRAINT "CK_mir_situation"
                  CHECK (("Situation" IN ('CHAMBER_MANUFACTURE','SERVICE_CUSTOMER_PO','SITE_PROJECT_PO')
                           AND "DestinationType"='JOB_ORDER' AND "JobOrderId" IS NOT NULL)
                      OR ("Situation"='CONSUMABLE_OFFICE' AND "DestinationType" IN ('DEPARTMENT','OTHER')
                           AND "JobOrderId" IS NULL));
                """);
        }
    }
}