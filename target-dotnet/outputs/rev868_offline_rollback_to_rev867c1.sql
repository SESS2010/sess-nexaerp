START TRANSACTION;
ALTER TABLE nexa.purchase_requirement_handoffs DROP CONSTRAINT "FK_purchase_requirement_handoffs_rack_bins_RackBinId";

ALTER TABLE nexa.purchase_requirement_handoffs DROP CONSTRAINT "FK_purchase_requirement_handoffs_warehouses_WarehouseId";

ALTER TABLE nexa.stock_availability_check_lines DROP CONSTRAINT "FK_stock_availability_check_lines_rack_bins_RackBinId";

ALTER TABLE nexa.stock_availability_check_lines DROP CONSTRAINT "FK_stock_availability_check_lines_warehouses_WarehouseId";

ALTER TABLE nexa.stock_reservations DROP CONSTRAINT "FK_stock_reservations_rack_bins_RackBinId";

ALTER TABLE nexa.stock_reservations DROP CONSTRAINT "FK_stock_reservations_warehouses_WarehouseId";

DROP TABLE nexa.purchase_number_sequences;

DROP INDEX nexa."IX_stock_reservations_ItemId_WarehouseId_RackBinId_Status";

DROP INDEX nexa."IX_stock_reservations_PurchaseRequisitionLineId_LocationKey_St~";

DROP INDEX nexa."IX_stock_reservations_RackBinId";

DROP INDEX nexa."IX_stock_reservations_WarehouseId";

DROP INDEX nexa."IX_stock_availability_check_lines_PurchaseRequisitionLineId_Wa~";

DROP INDEX nexa."IX_stock_availability_check_lines_RackBinId";

DROP INDEX nexa."IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur~";

DROP INDEX nexa."IX_stock_availability_check_lines_WarehouseId";

ALTER TABLE nexa.stock_availability_check_lines DROP CONSTRAINT "CK_stock_check_lines_quantities_valid";

DROP INDEX nexa."IX_purchase_requisitions_OrganizationId_FinancialYear_PrSequen~";

ALTER TABLE nexa.purchase_requisitions DROP CONSTRAINT "CK_purchase_requisitions_estimated_total_nonnegative";

ALTER TABLE nexa.purchase_requisition_lines DROP CONSTRAINT "CK_pr_lines_reconcile_requested";

DROP INDEX nexa."IX_purchase_requirement_handoffs_RackBinId";

DROP INDEX nexa."IX_purchase_requirement_handoffs_WarehouseId";

ALTER TABLE nexa.purchase_approval_route_settings DROP CONSTRAINT "CK_purchase_route_limits_valid";

ALTER TABLE nexa.stock_reservations DROP COLUMN "LocationKey";

ALTER TABLE nexa.stock_reservations DROP COLUMN "RackBinId";

ALTER TABLE nexa.stock_availability_check_lines DROP COLUMN "CheckedAt";

ALTER TABLE nexa.stock_availability_check_lines DROP COLUMN "LocationKey";

ALTER TABLE nexa.stock_availability_check_lines DROP COLUMN "RackBinId";

ALTER TABLE nexa.purchase_requisitions DROP COLUMN "FinancialYear";

ALTER TABLE nexa.purchase_requisitions DROP COLUMN "PrSequence";

ALTER TABLE nexa.purchase_requirement_handoffs DROP COLUMN "LocationKey";

ALTER TABLE nexa.purchase_requirement_handoffs DROP COLUMN "RackBinId";

ALTER TABLE nexa.stock_reservations ALTER COLUMN "WarehouseId" DROP NOT NULL;

ALTER TABLE nexa.stock_availability_check_lines ALTER COLUMN "WarehouseId" DROP NOT NULL;

ALTER TABLE nexa.purchase_requirement_handoffs ALTER COLUMN "WarehouseId" DROP NOT NULL;

CREATE UNIQUE INDEX "IX_stock_reservations_PurchaseRequisitionLineId_Status" ON nexa.stock_reservations ("PurchaseRequisitionLineId", "Status") WHERE "Status" = 'Active';

CREATE INDEX "IX_stock_availability_check_lines_PurchaseRequisitionLineId" ON nexa.stock_availability_check_lines ("PurchaseRequisitionLineId");

CREATE UNIQUE INDEX "IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur~" ON nexa.stock_availability_check_lines ("StockAvailabilityCheckId", "PurchaseRequisitionLineId");

ALTER TABLE nexa.purchase_requisitions ADD CONSTRAINT "CK_purchase_requisitions_estimated_total_nonnegative" CHECK ("EstimatedTotal" >= 0);

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260808190920_Rev868PurchaseLocationAllocationCorrection';

COMMIT;

START TRANSACTION;
DROP TABLE nexa.purchase_approval_route_settings;

DROP TABLE nexa.purchase_requirement_handoffs;

DROP TABLE nexa.purchase_requisition_approval_history;

DROP TABLE nexa.purchase_requisition_attachments;

DROP TABLE nexa.purchase_requisition_status_history;

DROP TABLE nexa.stock_availability_check_lines;

DROP TABLE nexa.stock_reservation_history;

DROP TABLE nexa.stock_availability_checks;

DROP TABLE nexa.stock_reservations;

DROP TABLE nexa.purchase_requisition_lines;

DROP TABLE nexa.purchase_requisitions;

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '01034d48-c4aa-7261-e9e4-888832ab13b2';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '01275179-960c-8401-c25c-ff3ea100b465';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '0130c6a9-a282-fe5f-0e87-f85dc76b2051';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '01afd214-f457-6905-469e-95e1ba60771c';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '026bea62-c207-632b-d3d7-cafb5c973658';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '02f7e8f7-a3d1-08cc-3d1b-9e439be2cf0d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '0557f009-230c-3043-7634-ef0d1dc3480b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '066b907a-90f6-f400-31c2-9a8de85f58fa';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '083fc830-139f-4006-f637-5f900fd8132e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '08df695d-e9ba-70f9-dd5e-6b8d88551bb9';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '0a99c83f-8f94-ecb7-a877-69267beedd8c';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '0d271d3e-4008-1465-6de5-9b660ff60bf7';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '0ef31d8a-189c-19fe-4a78-033ae9e70bc6';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '1056ab04-f3e9-fb95-b805-4a51a7698c69';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '1059f07f-9ce5-de0f-c16c-01cf02116aed';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '10c78633-b41c-5825-051f-a146d4402aeb';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '12ba4a62-899f-a2c2-6ca1-8c3c1399f8d3';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '12e1379b-f17a-7f0a-e522-8dba3b966cf9';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '14728dfc-d82e-6bfa-923b-5770ddac7bac';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '14aed82d-d726-2c80-fe1c-6e3c54538789';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '14fbdac8-67bc-e8e6-8b56-54d70160c626';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '161a0fe1-1bd2-aefb-1f3a-a8ff3d72c280';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '169c329d-735d-3ae9-d519-17363643a809';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '18226f75-4c36-6e6c-7db1-ac8b334b418f';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '198ead02-e678-7cfa-e082-0da2a9237d0e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '1c9f738e-13da-98ed-8735-c0af87d1bed1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '2175c1e4-246b-dc54-47cb-8607d03c2c4f';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '239b1345-26aa-1c2a-c562-e48e090ed35a';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '247195b9-2d76-5233-5d2a-466fd3bca58e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '24f0f519-6d9d-09f4-4fcf-e468d575687b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '2692e7b2-73fd-8756-8b3d-d437d29081a9';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '26c4abc1-da39-2a42-c592-3c708d29b708';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '27433977-1cb9-192e-3610-75d085355b48';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '2944f487-7898-b449-64f6-0f254dc905be';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '2946a6fe-8394-2003-3b33-b849e05c3fcc';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '2954e3a3-1352-68bf-fdff-a61240289f93';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '29dbf667-a680-7c0e-41c5-2dc90ee8db4f';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '2b663021-ceb8-6891-1411-78ef52c7eb8e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '2de773df-4003-1012-79ff-8040024a1b4a';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '2f9e7d78-ce42-7ea5-728c-87c48a3a7f91';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '305d84e6-491d-a3b4-e65a-151d9d7103bf';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '305efbfd-f002-5fa5-e72b-7743de8a4994';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '30d233d8-06fa-b3ca-b27b-5ddd08860846';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '31f2ed64-5eca-9e6d-8b1f-7fc420dea466';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '325d5475-24b6-69b3-ed22-4e7e66199841';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '32c4a61c-3146-ac2a-4f6e-bdb38740ccb0';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '37d60081-868c-812b-2e66-f1f8d246fbac';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '38473eb3-b92b-cbb4-4734-eee0f824ae35';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '38c70de3-dbe7-ad40-8654-1eeba4a5a9f7';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '395caf08-cf73-f71e-9890-3975df1baac2';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '3a9913da-c7c7-d537-03cb-d4a75c8c33fe';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '3db27053-76d7-050d-bbd7-96ea49d32e93';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '3ddc0294-a7ae-c93c-394f-0579b64e7f21';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '3eb8ca06-7f3a-2338-51b9-d93f2e710a8b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '3ed2e32a-1764-9ca4-3b76-61010dfeb3c2';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '3f77987f-e1a2-00f1-ae12-c97da302650c';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '406f8c4d-3b9e-1e3d-780b-d1a27edfc5e6';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '41a7df76-f655-3412-5f37-2cd417f98c82';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '41f7a71a-3efb-c4d3-55bb-c8a9508860d2';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '42a15e72-3c0e-67f1-746d-5ee534d9c502';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '42c3d133-02c4-f84e-ccfc-9369445b0a7b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '43ddaf52-98da-5578-2011-7757d6812123';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '4548ad6b-0d20-1b5b-3808-196248fdf7d5';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '4764ff10-a265-f4fc-9deb-b316154b1cb2';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '47a3ddf7-452f-a656-1973-de76128f4bab';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '49980728-f2c1-9f56-0ad9-b36c5a889719';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '4a323803-1aca-a52a-1836-8b15ee90d398';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '4ccfd0e0-caf1-0b35-a4e9-b4c610d1d518';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '4d26d81d-eebb-e354-305c-20c3de67eaba';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '4de91ccb-ee76-da93-f6b6-3fc772dfff78';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '4df35c89-1203-a270-b546-40696fe301f1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '4ebbd962-8401-cb16-ce3e-40b63680780f';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '5003bcd7-f0ec-79ce-a83a-1798a51795cc';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '5086a30f-82f9-c2b2-60e3-57ffc2de96c6';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '5173d448-e9cd-0d68-c79f-7f1e0ba4fd9c';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '517c3c10-2971-66d1-6a18-38dfda4d4d5d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '523cc95a-3d9f-9b75-9d87-7df0a1b34253';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '53c18ca9-f7ae-a720-5854-ef47c72ff7c4';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '57b0c075-0523-d6f6-b63e-0b114bc49400';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '58941021-2c3e-dd2f-ecc9-5eb5449171c1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '5a414697-e5fc-0555-677e-ea21efcf7bb6';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '5e835d64-eedf-47ea-747c-bd6f50092619';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '5fea3ee3-8203-6e8c-8252-3886526f5d80';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '61273037-8a65-61f3-387a-b4ae8d854662';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '616d1a16-c056-3b49-b92c-74d382827474';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '638fb6d9-200c-a3b0-9317-cd900579cbb2';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '64cf91c9-25bc-cfde-1954-9d8ab7f291f1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '67b3c775-07be-ea99-6655-a657dcaf45a5';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '67bf1f4b-87b1-90cd-15c8-409205e9e68f';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '6a4e890b-7586-b5b5-1a17-1e2c8ce592fa';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '6bdc4ba3-dc86-4023-164b-8530fa624738';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '6c4013cc-9b70-4bab-c658-efd52f1534d1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '6eec19ec-3377-42ce-3428-2faec9cfc5f0';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '70b4e227-f7b7-5634-7488-9806821837fa';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '71fba4e5-513d-78af-09e2-352fb4c8be7e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '727c6e92-30c8-ad23-6f5e-9714d4342f0d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '740f4cb3-9687-8846-9429-b8028f3fe929';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '77c824f5-4b8c-67d8-7512-c8be21d7e4e8';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '792cbc80-2b16-72eb-d980-7d4e174fee04';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '79b9c190-d7f3-44ad-64d9-39ddf14241d5';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '7a0d54be-e71c-e5b1-a734-fc55346672ac';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '7a38257f-236b-f091-cc2d-6a07d4b3b30d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '7ab64f73-3a84-bcde-1d48-46e0f48e5445';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '7d3f5f56-f674-1f72-68b0-3a55813f8dfc';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '824f4ce6-98b4-6f74-4d3f-3f5df926c4c0';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '85937b3f-f489-d8c8-40bb-5aee61844a4d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '85b6bc45-c13c-da25-3198-37ee0d504701';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '86346fed-f259-e15c-771a-61cb0b5e6188';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '863e4f26-ac98-0c5e-546b-449eccb3845e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '88352e79-c410-91c1-e012-de38870124d3';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '88ea25c7-3475-b940-a8fc-b72a8c33cc56';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '8a6baa4e-990e-de85-d729-3128dbb4b0a9';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '8aaeb6eb-fbde-56da-8033-35821404722f';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '8af45e13-d08f-c109-023f-c358663e71b6';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '8ce78826-3814-28ba-6dc2-9b29134d2f16';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '8e22ffbb-d684-6090-7e6e-3605773f71a0';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '8f6a81af-626b-1fe2-7bfd-6a65e20597f8';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '9238885e-5891-4759-3a2e-d11a14bf4216';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '93cdf397-66b9-7b69-9567-522ae6d132b3';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '941272bd-659e-dddd-0643-367822a5530b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '95823336-4ed8-1c15-e280-0dcf8334035a';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '963f18fc-c8c5-66fb-a59a-f250236ed752';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '96c24743-97ca-fa9b-8204-db4eb256bfb1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '997e03c9-200f-848d-95a5-07a8184fa888';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '9acc891b-a181-b02c-5324-4e3e461e3912';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = '9b233dda-aab8-6d62-224e-fd7aef39c60c';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'a08d7c05-387e-568a-d8e0-e68bc715a01c';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'a0b4a6f5-546e-c0da-f65c-37ef5cea452f';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'a48ba706-ddd8-7c00-c475-2af8e71c05a9';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'a7f2c4cb-37c6-6a11-0e81-01f72d96b9f6';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'a83d03e3-12f2-a974-3ed7-6a30b3417b0e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'a868cbbe-5698-fb7b-78b7-491135a21161';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'a95c1e81-50e3-7611-596e-138988ff96bc';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'aa3d4ca4-8a8f-a580-9f26-a8fbccf8d21a';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'ae0a6f84-4a8c-4a0a-2064-901d455061dc';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'aed2bfcd-4d44-f086-7fd6-9f9f2b15f48b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'af844032-41d3-fa9f-e2b1-dc574dcb5ebb';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'b161cf4a-3880-8da3-7f1a-e3e023e7beb6';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'b272dde4-4d09-cbab-f515-58c2d33cbb0e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'b4d08c0f-ce35-c067-b48c-1921a6e439a4';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'b8caf4b8-d200-7f93-d0de-034135a14a55';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'bace752c-586d-a395-8fa7-572166a4065b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'bb264540-e621-baa8-e366-53d5226521fa';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'bb857600-32b5-7229-f603-29dc829a4f3e';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'bc1ba3aa-0266-e40d-e923-93f9556f811b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'bc2cd481-43cf-34b2-bb18-556cc4610e77';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'c0d55070-b641-2405-681d-3d0e4cb48ec9';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'c3872708-d710-c5f1-21a4-98a0c809bd09';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'c4ec61cb-60c3-691d-2694-d6224e81675d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'c52f1f6a-2182-52d9-060a-e1cf8b2bf35d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'c5f29057-8ee7-a61a-ca1c-63115b20e6b1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'c8e771de-9e68-c6bb-77fa-99a35f151bbc';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'caf4e7c0-1e09-3f80-df52-19697ce7d9bd';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'cc60ef9d-c06b-db1f-df24-b7bd7d92cff7';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'ce1fc874-a8dd-69b0-5336-cbccf0053cbb';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'ce8e33eb-25dc-69e5-3883-7c87b5dfbc04';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd093e505-880e-d176-36de-0e7addfee298';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd10ce882-7553-d9d3-38f8-aaf30dfbeb8a';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd2371ab8-06a9-c9c8-5edd-29494f01b74a';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd47355ca-57d6-40ae-29a3-e9b9b51aff04';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd4a22905-013d-6a11-28f8-5287f5fc79f8';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd4e09e16-891a-34cc-1604-57d62da5f6d8';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd4ffa090-2309-a329-a24b-36be029a5644';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd5f64cfa-c44c-3deb-2934-367db7cb231b';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'd8b7eab3-ff6b-807d-abc1-83889a59c6d1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'da339d77-93c1-8b00-1227-6bda34380f10';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'dadda6ba-b432-508a-305a-77b8a391f540';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'db7b3a3a-9184-110b-4f1d-3a7970b42f99';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'ddff4cb2-3075-5fb8-5217-97bbc5b7c43d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'df09a412-868d-93ba-12b4-0ce27c1178ed';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'e24a5141-961b-7d1d-a40e-c826a74e2be6';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'e392d6b6-7b54-e0c2-1d67-42474353fd00';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'e4ef51b0-080a-a4b5-40cd-76ceae40608a';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'e4fb4bcd-a855-58f8-4858-8a4e825185dd';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'e668f430-c067-2ef4-2e92-80c3551045c1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'e93749b8-6e80-7b68-4915-57d98a6ea489';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'ed1cc2e0-7d4f-2edc-50d7-82c87c911dbe';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'ee607d44-6dbd-ece5-9270-9220f465a7f0';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'f095917e-cd2f-48df-373e-1ea1e7e2a7b8';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'f0bd803c-23d2-440c-ca37-02bb554bffd1';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'f25f6ac3-b0dd-64f4-a82d-b459aff22397';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'f279f58e-9c05-e09d-cc7a-86085c8b504c';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'f4c531c9-50f4-244d-ded3-acdf840ea285';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'f9b536bb-4190-53d4-ed42-16932e9c5a51';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'fa43bbd4-306d-f557-e8ef-d2cd39d87114';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'fa7ff198-dd2a-2f9f-1f73-6245d877889a';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'fd028b07-4ecc-439f-ca45-cbcf249574a9';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'fe763e56-8a62-9d7e-8722-77ba4816d949';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'ff2181d4-cb3b-ac61-78cb-474d8b70762d';

DELETE FROM nexa.role_page_permissions
WHERE "Id" = 'ffe73e4a-7346-cf6a-2e58-0fa7cc6e2b96';

DELETE FROM nexa.page_definitions
WHERE "Id" = '20000000-0000-0000-0000-000000000022';

DELETE FROM nexa.page_definitions
WHERE "Id" = '20000000-0000-0000-0000-000000000023';

DELETE FROM nexa.page_definitions
WHERE "Id" = '20000000-0000-0000-0000-000000000024';

DELETE FROM nexa.page_definitions
WHERE "Id" = '20000000-0000-0000-0000-000000000025';

DELETE FROM nexa.page_definitions
WHERE "Id" = '20000000-0000-0000-0000-000000000026';

DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260808182945_Rev868PurchaseRequisitionFoundation';

COMMIT;

