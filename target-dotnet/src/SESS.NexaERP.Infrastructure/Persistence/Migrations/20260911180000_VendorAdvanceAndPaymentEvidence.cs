using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260911180000_VendorAdvanceAndPaymentEvidence")]
public sealed class VendorAdvanceAndPaymentEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(VendorAdvancePaymentSql.Up, suppressTransaction: false);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(VendorAdvancePaymentSql.Down, suppressTransaction: false);
}