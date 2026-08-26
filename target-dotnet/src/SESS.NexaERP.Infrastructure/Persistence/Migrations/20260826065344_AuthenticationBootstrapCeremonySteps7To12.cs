using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthenticationBootstrapCeremonySteps7To12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(AuthenticationBootstrapCeremonySql.PreUp);
            migrationBuilder.DropCheckConstraint(
                name: "CK_authentication_bootstrap_completion",
                schema: "advance",
                table: "authentication_bootstrap_state");

            migrationBuilder.AddColumn<int>(
                name: "CompanyCount",
                schema: "advance",
                table: "authentication_bootstrap_state",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "CompanySetSha256",
                schema: "advance",
                table: "authentication_bootstrap_state",
                type: "bytea",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_authentication_bootstrap_completion",
                schema: "advance",
                table: "authentication_bootstrap_state",
                sql: "(\"Status\"='PENDING' AND \"EmployeeId\" IS NULL AND \"CompanyId\" IS NULL AND \"OrganizationId\" IS NULL AND \"IssuerSha256\" IS NULL AND \"SubjectSha256\" IS NULL AND \"CompanyCount\" IS NULL AND \"CompanySetSha256\" IS NULL AND \"CompletedAt\" IS NULL AND \"CompletedBy\" IS NULL) OR (\"Status\"='COMPLETED' AND \"EmployeeId\" IS NOT NULL AND \"CompanyId\" IS NOT NULL AND length(trim(\"OrganizationId\"))>0 AND octet_length(\"IssuerSha256\")=32 AND octet_length(\"SubjectSha256\")=32 AND \"CompanyCount\">0 AND octet_length(\"CompanySetSha256\")=32 AND \"CompletedAt\" IS NOT NULL AND length(trim(\"CompletedBy\"))>0)");
            migrationBuilder.Sql(AuthenticationBootstrapCeremonySql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(AuthenticationBootstrapCeremonySql.Down);
            migrationBuilder.DropCheckConstraint(
                name: "CK_authentication_bootstrap_completion",
                schema: "advance",
                table: "authentication_bootstrap_state");

            migrationBuilder.DropColumn(
                name: "CompanyCount",
                schema: "advance",
                table: "authentication_bootstrap_state");

            migrationBuilder.DropColumn(
                name: "CompanySetSha256",
                schema: "advance",
                table: "authentication_bootstrap_state");

            migrationBuilder.AddCheckConstraint(
                name: "CK_authentication_bootstrap_completion",
                schema: "advance",
                table: "authentication_bootstrap_state",
                sql: "(\"Status\"='PENDING' AND \"EmployeeId\" IS NULL AND \"CompanyId\" IS NULL AND \"OrganizationId\" IS NULL AND \"IssuerSha256\" IS NULL AND \"SubjectSha256\" IS NULL AND \"CompletedAt\" IS NULL AND \"CompletedBy\" IS NULL) OR (\"Status\"='COMPLETED' AND \"EmployeeId\" IS NOT NULL AND \"CompanyId\" IS NOT NULL AND length(trim(\"OrganizationId\"))>0 AND octet_length(\"IssuerSha256\")=32 AND octet_length(\"SubjectSha256\")=32 AND \"CompletedAt\" IS NOT NULL AND length(trim(\"CompletedBy\"))>0)");
        }
    }
}
