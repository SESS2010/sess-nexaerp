using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ItemReferenceMasterFrontendReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.InsertData(
                schema: "advance",
                table: "page_definitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("41000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, "Masters", "masters.item-categories", "/masters/item-categories", "Item Category Master", null, null, 0L },
                    { new Guid("41000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, "Masters", "masters.item-subcategories", "/masters/item-subcategories", "Item Subcategory Master", null, null, 0L },
                    { new Guid("41000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, "Masters", "masters.manufacturers", "/masters/manufacturers", "Manufacturer Master", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("029464c3-629a-21a5-8352-2b612ddcbeb6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("040b4067-21ce-61f9-9f72-83bbedc1d379"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("06c1559b-8ab7-74ef-503e-b89963c5ebef"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("0781a843-db91-6bbc-c997-4e96b0800e22"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("07fff1e3-0861-9c7f-d43e-aa861368b886"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("09b51c62-d9d0-edda-015d-536b02df3d9e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("0b5973e9-75d6-ec71-7646-2a91d1c2e49a"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("0c55dfc4-97a8-47e5-eb48-209b7f7974e2"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("0c588677-4e71-cf90-69ec-814fc351c168"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("0cc54f61-95ba-55eb-97fa-788b9361f77f"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("0f6d3deb-a723-f13b-b2d9-5f43c7c704ff"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("12146efd-75bf-e4a8-1a55-1dbf4fe5798d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("15082440-a786-5292-97f1-7d030b55fef3"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("16675a12-f6f6-712a-18f9-7ded9db1c599"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("17934ea9-2500-46fd-25a7-589d40ab8b6a"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("17e2e2aa-ff9e-6d1d-fa46-a7fa109cde89"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("19152032-a285-91ca-90ff-84296ac440f7"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("1b3db722-db20-2a5a-e177-a6c8df406ffe"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("1fae4d60-12af-5b84-b5f1-b3248110c967"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("1ff4f209-cbdf-34c6-04a9-7f7b5e128d55"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("202661b8-36a0-6733-7bf5-2fa97c04f97e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("208dd226-3841-8a95-d98f-f51b38d27cae"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("217306ee-0967-58ef-c137-e9ed1a16668a"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("261448f7-4dc0-10fb-4fce-7577c4431e0d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("2825d3ec-d47e-ce6e-7ce1-a5141810a816"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("291acd3a-c19e-1594-98e3-25ed0048a760"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("2c186de4-fc2c-db21-c33e-ad116164e3eb"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2cb48399-1bf3-b70a-145f-e1400d4fa250"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("2df4ddaf-aa45-3455-9703-6b228232275a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("2f877e63-45b8-0e50-ee13-879d23ff2a6e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("329c31a9-ad20-db8d-61d6-8c406345961e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("32d1770a-c60b-08ba-cfd3-8b286e25ede6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("32d74c96-a907-bc7c-5337-6a6d85dcdc16"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("335bc3e0-520c-63e8-7a0d-5fd94d822dec"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("368c9b6f-d582-ad4e-87eb-12c17ae20138"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("391e745f-fce0-f37f-9b51-d203317e78d9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("395cc609-fc53-b0e5-65b6-70dda24e09da"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("3a8aa728-2583-e632-ff22-6d3d8cb8efe1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("3b2b0435-d79c-4a7f-94a0-ebc22cfff99f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("3c4da9d4-d707-d9bd-8793-f6b4ac748be1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("3d8609a7-d649-0c32-6002-aca4f7930459"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("3f9365a1-bf98-beb6-6275-78eb55d58f57"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("4101a734-4cb6-f8c5-604f-0ffab08fa50d"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("4201b75c-e74f-d928-4dde-d26d628bfb02"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("44472290-b080-3668-1484-e133e34946d9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("468a99c3-a322-85fc-199e-1c572f0e7f7f"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("49a02acc-c2e8-7492-f950-c4d9b9139579"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("4c8b3874-fa52-21f6-daec-4c0bad142977"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("4dafdd3e-3189-c5cd-64b9-6a106751ef00"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("4eb92a2a-0d4c-44bb-8416-4e205b0be827"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("5252387d-93f6-30b3-769f-2c622bfa9101"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("541c7004-cc06-93a3-cf4b-d3d7130f79be"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("5703b510-3d72-c0c7-7742-e5a411a41729"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("58d96c10-9cb2-df0f-0d76-a67c016b76ae"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("5b9b68a6-58e6-99f3-de27-df37bfe8acf3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("5bce9432-6659-f069-bd95-5246afab8817"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("5ec67469-7905-754c-9b42-c63e060c402c"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("5f2fd903-bdcd-8c28-3ec4-9852b3ee9b75"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("7159dd8d-ca9f-9eac-a30b-9743994e6569"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("75b049fd-95fd-ec35-0d52-957923ff37e1"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("78e67707-1380-051f-c060-718a0df6358f"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("81e05a7e-f1ae-ecdf-9303-94fbfce36bbe"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("86730bd2-34d3-95a1-f6c0-bedf37d23435"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("8b0a08a7-8502-fde8-4e2c-7141aca52136"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("8b312e85-e3f9-fe16-9888-2da711992959"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("8bc51778-49c0-bc66-9cf7-fa3e5ab94860"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("8db3e37f-1d61-0b7a-f8c6-6d50e1151c61"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("8e39e076-6614-3b5f-5e93-005396770961"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("8eaa48e0-1b51-a5f9-6b32-e204d2f568fd"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("8f4dc00e-eaf4-4109-046d-c537de4018ae"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("968d3a33-742a-5574-faaa-a245f55d0102"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("96e5c669-0ae9-a1fd-b3e9-e0354c5b2621"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("99e0aa2f-2c35-bdf7-d992-8607ee22ae50"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("9d1d5501-bbaa-80f8-6501-bf3ebb05cf81"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("a049d045-caab-317e-32d4-3441d3fc5bb8"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("a0665a5b-0cdf-9ace-6583-3a4f84d63645"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("a099cd80-d1c1-7dba-f1fb-2f241d4fa926"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("a13a78e7-fa42-6d58-24fb-8e0b853e7c96"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("a16d8dbd-e25c-4a5d-599f-0c89bdbac63d"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("a3dcf094-3b02-0a1c-1c2d-81ffe382f81e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("a71f0357-996b-d7fc-083a-cb238e611d8c"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("a7648574-243d-b23d-556a-eaee7d006f84"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("a78be10e-c713-a7b5-0f84-b380dee8805e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("a7d609c6-28fc-78fb-6460-f475abb6fe05"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("a939412e-7da8-72d4-e379-82fd04aad2a8"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("aaaf9c6e-595e-b9df-e02b-556f30221220"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("ab88d8b2-b091-9c99-e963-fdb84e79b0d7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("ac54e238-5f2a-0996-17f8-a42f6bc4a2ae"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("b54c9c0b-61cf-9e09-bc07-4fbb5d9b7d85"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("b666e79a-8f02-3b8f-3d0e-3ca5ebea97d2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("b819237a-329e-e8d8-bc12-93ff06ea146c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("b9b48f13-fd43-92f6-4254-f401fe255c3b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("ba5594b7-3665-384e-9bb4-d58d43538330"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("be44625b-95b2-2baf-7daa-1ac5ad782037"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("c6be76f7-535f-572d-a9c6-488044acee0f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("c97823de-f7f4-bd27-8ce7-9846065f54fb"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c9dc0f4c-39bd-eac1-c221-a3f7c06a5757"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("ca82b310-413d-32f0-d89b-39ed6735af41"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("cb8c7655-09d9-5b8e-679e-4fd3ecbd6243"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("cba5e114-3e2d-f1ca-657b-79def74e42d3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("d089097c-0e34-4142-9112-06c114df9073"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("d0e9c4ed-8fa8-bdf3-1a41-97a8518237d3"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("d17f0b3d-84ab-5fff-a7fa-9887755ef966"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("d1c32c2b-88f1-76c0-bbcc-51a089d17a83"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("d6c22089-30dc-34cf-2f8e-1e0d6c45be16"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("d75eeb9a-355e-2d35-b361-39cb0fe44d19"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("dd718fd1-8e8d-f264-a47b-5055697610dc"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("e07953c0-3702-5168-9ba4-4a8c1841bcd1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("e15c3573-535d-16a1-3312-5043f0266366"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("e194fada-80fb-dc73-b0b3-285a0571cf69"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("e5f0087c-2449-45fc-7745-1bcb77183303"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("e71783a4-5cee-23f5-f6df-e60090cf0168"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("e788d0d3-bd41-9cb6-b3ec-8ebca1514f62"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("ebe1f3b7-6574-d512-417b-12d1d0f3bc62"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("ed137e53-e32b-07eb-2e36-74363e62ae2b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("f20aab45-ad20-2ed9-570a-6617a2a38659"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("f34128b0-0ca7-9003-6217-3e97d0517080"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("f3da0471-ec2f-9f36-5cb2-6798eed49f90"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("f518fce0-52b6-3247-ec45-b48f3731b6a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("f552d60a-fcb4-254d-c102-2907248cfe9c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("f561f735-3083-288e-7622-d515ab7b8536"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("f6ee3e5a-6b22-68aa-da27-4f546ed54720"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("f75cd766-2831-df5e-1667-36c6019a18b0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("f9f61a6e-457f-67b0-3e3a-e1a4902d912c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("fa21d463-6984-90b4-1995-d8e6c9212862"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("fae6df60-6e8e-3d61-21c4-84231af4c475"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("fc67164c-17df-efb8-3d0a-ef376b448876"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", true, new Guid("41000000-0000-0000-0000-000000000001"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("fedc692c-9db1-d1ea-2e4c-717a64753ccf"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("ff4a8838-ed05-985b-71d9-60768c9c4bf6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-item-reference-masters", false, new Guid("41000000-0000-0000-0000-000000000003"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("029464c3-629a-21a5-8352-2b612ddcbeb6"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("040b4067-21ce-61f9-9f72-83bbedc1d379"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("06c1559b-8ab7-74ef-503e-b89963c5ebef"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0781a843-db91-6bbc-c997-4e96b0800e22"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("07fff1e3-0861-9c7f-d43e-aa861368b886"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("09b51c62-d9d0-edda-015d-536b02df3d9e"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0b5973e9-75d6-ec71-7646-2a91d1c2e49a"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0c55dfc4-97a8-47e5-eb48-209b7f7974e2"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0c588677-4e71-cf90-69ec-814fc351c168"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0cc54f61-95ba-55eb-97fa-788b9361f77f"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0f6d3deb-a723-f13b-b2d9-5f43c7c704ff"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("12146efd-75bf-e4a8-1a55-1dbf4fe5798d"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("15082440-a786-5292-97f1-7d030b55fef3"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("16675a12-f6f6-712a-18f9-7ded9db1c599"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("17934ea9-2500-46fd-25a7-589d40ab8b6a"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("17e2e2aa-ff9e-6d1d-fa46-a7fa109cde89"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("19152032-a285-91ca-90ff-84296ac440f7"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1b3db722-db20-2a5a-e177-a6c8df406ffe"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1fae4d60-12af-5b84-b5f1-b3248110c967"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1ff4f209-cbdf-34c6-04a9-7f7b5e128d55"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("202661b8-36a0-6733-7bf5-2fa97c04f97e"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("208dd226-3841-8a95-d98f-f51b38d27cae"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("217306ee-0967-58ef-c137-e9ed1a16668a"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("261448f7-4dc0-10fb-4fce-7577c4431e0d"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2825d3ec-d47e-ce6e-7ce1-a5141810a816"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("291acd3a-c19e-1594-98e3-25ed0048a760"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2c186de4-fc2c-db21-c33e-ad116164e3eb"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2cb48399-1bf3-b70a-145f-e1400d4fa250"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2df4ddaf-aa45-3455-9703-6b228232275a"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2f877e63-45b8-0e50-ee13-879d23ff2a6e"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("329c31a9-ad20-db8d-61d6-8c406345961e"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("32d1770a-c60b-08ba-cfd3-8b286e25ede6"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("32d74c96-a907-bc7c-5337-6a6d85dcdc16"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("335bc3e0-520c-63e8-7a0d-5fd94d822dec"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("368c9b6f-d582-ad4e-87eb-12c17ae20138"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("391e745f-fce0-f37f-9b51-d203317e78d9"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("395cc609-fc53-b0e5-65b6-70dda24e09da"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3a8aa728-2583-e632-ff22-6d3d8cb8efe1"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3b2b0435-d79c-4a7f-94a0-ebc22cfff99f"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3c4da9d4-d707-d9bd-8793-f6b4ac748be1"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3d8609a7-d649-0c32-6002-aca4f7930459"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3f9365a1-bf98-beb6-6275-78eb55d58f57"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4101a734-4cb6-f8c5-604f-0ffab08fa50d"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4201b75c-e74f-d928-4dde-d26d628bfb02"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44472290-b080-3668-1484-e133e34946d9"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("468a99c3-a322-85fc-199e-1c572f0e7f7f"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("49a02acc-c2e8-7492-f950-c4d9b9139579"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4c8b3874-fa52-21f6-daec-4c0bad142977"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4dafdd3e-3189-c5cd-64b9-6a106751ef00"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4eb92a2a-0d4c-44bb-8416-4e205b0be827"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5252387d-93f6-30b3-769f-2c622bfa9101"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("541c7004-cc06-93a3-cf4b-d3d7130f79be"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5703b510-3d72-c0c7-7742-e5a411a41729"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("58d96c10-9cb2-df0f-0d76-a67c016b76ae"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5b9b68a6-58e6-99f3-de27-df37bfe8acf3"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5bce9432-6659-f069-bd95-5246afab8817"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5ec67469-7905-754c-9b42-c63e060c402c"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5f2fd903-bdcd-8c28-3ec4-9852b3ee9b75"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7159dd8d-ca9f-9eac-a30b-9743994e6569"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("75b049fd-95fd-ec35-0d52-957923ff37e1"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("78e67707-1380-051f-c060-718a0df6358f"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("81e05a7e-f1ae-ecdf-9303-94fbfce36bbe"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("86730bd2-34d3-95a1-f6c0-bedf37d23435"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b0a08a7-8502-fde8-4e2c-7141aca52136"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b312e85-e3f9-fe16-9888-2da711992959"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8bc51778-49c0-bc66-9cf7-fa3e5ab94860"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8db3e37f-1d61-0b7a-f8c6-6d50e1151c61"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8e39e076-6614-3b5f-5e93-005396770961"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8eaa48e0-1b51-a5f9-6b32-e204d2f568fd"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8f4dc00e-eaf4-4109-046d-c537de4018ae"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("968d3a33-742a-5574-faaa-a245f55d0102"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("96e5c669-0ae9-a1fd-b3e9-e0354c5b2621"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("99e0aa2f-2c35-bdf7-d992-8607ee22ae50"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9d1d5501-bbaa-80f8-6501-bf3ebb05cf81"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a049d045-caab-317e-32d4-3441d3fc5bb8"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a0665a5b-0cdf-9ace-6583-3a4f84d63645"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a099cd80-d1c1-7dba-f1fb-2f241d4fa926"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a13a78e7-fa42-6d58-24fb-8e0b853e7c96"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a16d8dbd-e25c-4a5d-599f-0c89bdbac63d"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a3dcf094-3b02-0a1c-1c2d-81ffe382f81e"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a71f0357-996b-d7fc-083a-cb238e611d8c"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a7648574-243d-b23d-556a-eaee7d006f84"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a78be10e-c713-a7b5-0f84-b380dee8805e"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a7d609c6-28fc-78fb-6460-f475abb6fe05"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a939412e-7da8-72d4-e379-82fd04aad2a8"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("aaaf9c6e-595e-b9df-e02b-556f30221220"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ab88d8b2-b091-9c99-e963-fdb84e79b0d7"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ac54e238-5f2a-0996-17f8-a42f6bc4a2ae"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b54c9c0b-61cf-9e09-bc07-4fbb5d9b7d85"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b666e79a-8f02-3b8f-3d0e-3ca5ebea97d2"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b819237a-329e-e8d8-bc12-93ff06ea146c"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b9b48f13-fd43-92f6-4254-f401fe255c3b"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ba5594b7-3665-384e-9bb4-d58d43538330"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("be44625b-95b2-2baf-7daa-1ac5ad782037"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c6be76f7-535f-572d-a9c6-488044acee0f"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c97823de-f7f4-bd27-8ce7-9846065f54fb"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c9dc0f4c-39bd-eac1-c221-a3f7c06a5757"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ca82b310-413d-32f0-d89b-39ed6735af41"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cb8c7655-09d9-5b8e-679e-4fd3ecbd6243"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cba5e114-3e2d-f1ca-657b-79def74e42d3"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d089097c-0e34-4142-9112-06c114df9073"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d0e9c4ed-8fa8-bdf3-1a41-97a8518237d3"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d17f0b3d-84ab-5fff-a7fa-9887755ef966"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1c32c2b-88f1-76c0-bbcc-51a089d17a83"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d6c22089-30dc-34cf-2f8e-1e0d6c45be16"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d75eeb9a-355e-2d35-b361-39cb0fe44d19"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("dd718fd1-8e8d-f264-a47b-5055697610dc"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e07953c0-3702-5168-9ba4-4a8c1841bcd1"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e15c3573-535d-16a1-3312-5043f0266366"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e194fada-80fb-dc73-b0b3-285a0571cf69"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e5f0087c-2449-45fc-7745-1bcb77183303"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e71783a4-5cee-23f5-f6df-e60090cf0168"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e788d0d3-bd41-9cb6-b3ec-8ebca1514f62"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ebe1f3b7-6574-d512-417b-12d1d0f3bc62"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ed137e53-e32b-07eb-2e36-74363e62ae2b"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f20aab45-ad20-2ed9-570a-6617a2a38659"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f34128b0-0ca7-9003-6217-3e97d0517080"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f3da0471-ec2f-9f36-5cb2-6798eed49f90"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f518fce0-52b6-3247-ec45-b48f3731b6a8"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f552d60a-fcb4-254d-c102-2907248cfe9c"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f561f735-3083-288e-7622-d515ab7b8536"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f6ee3e5a-6b22-68aa-da27-4f546ed54720"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f75cd766-2831-df5e-1667-36c6019a18b0"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f9f61a6e-457f-67b0-3e3a-e1a4902d912c"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fa21d463-6984-90b4-1995-d8e6c9212862"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fae6df60-6e8e-3d61-21c4-84231af4c475"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fc67164c-17df-efb8-3d0a-ef376b448876"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fedc692c-9db1-d1ea-2e4c-717a64753ccf"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ff4a8838-ed05-985b-71d9-60768c9c4bf6"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("41000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("41000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("41000000-0000-0000-0000-000000000003"));
        }
    }
}
