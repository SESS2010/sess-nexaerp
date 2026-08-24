using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260823093000_StoresStockMovementLedgerHardening")]
public sealed class StoresStockMovementLedgerHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $stores_rev848_purge$
            DECLARE
                expected_count constant bigint := 4270;
                candidate_count bigint;
                deleted_count bigint;
                referenced_count bigint;
                join_predicate text;
                inbound_fk record;
            BEGIN
                LOCK TABLE nexa.stock_movements IN SHARE ROW EXCLUSIVE MODE;
                LOCK TABLE
                    nexa.goods_receipt_lines,
                    nexa.delivery_challan_lines,
                    nexa.inventory_transaction_lines,
                    nexa.material_return_lines
                IN SHARE MODE;

                SELECT count(*)
                INTO candidate_count
                FROM nexa.stock_movements
                WHERE created_by = 'REV848_STOCK_LEDGER_MOVEMENT';

                IF candidate_count <> expected_count THEN
                    RAISE EXCEPTION
                        'REV848 stock purge refused: expected % marked rows, found %.',
                        expected_count,
                        candidate_count;
                END IF;

                FOR inbound_fk IN
                    SELECT
                        constraint_row.conname,
                        constraint_row.conrelid,
                        constraint_row.confrelid,
                        constraint_row.conkey,
                        constraint_row.confkey,
                        referencing_table.relname AS table_name
                    FROM pg_catalog.pg_constraint AS constraint_row
                    JOIN pg_catalog.pg_class AS referencing_table
                      ON referencing_table.oid = constraint_row.conrelid
                    JOIN pg_catalog.pg_namespace AS referencing_schema
                      ON referencing_schema.oid = referencing_table.relnamespace
                    WHERE constraint_row.contype = 'f'
                      AND constraint_row.confrelid = 'nexa.stock_movements'::regclass
                      AND referencing_schema.nspname = 'nexa'
                      AND referencing_table.relname IN (
                          'goods_receipt_lines',
                          'delivery_challan_lines',
                          'inventory_transaction_lines',
                          'material_return_lines')
                LOOP
                    SELECT string_agg(
                        format('referencing_row.%I = movement_row.%I',
                            referencing_column.attname,
                            movement_column.attname),
                        ' AND '
                        ORDER BY key_pair.position)
                    INTO join_predicate
                    FROM unnest(inbound_fk.conkey, inbound_fk.confkey)
                         WITH ORDINALITY AS key_pair(
                             referencing_attribute_number,
                             movement_attribute_number,
                             position)
                    JOIN pg_catalog.pg_attribute AS referencing_column
                      ON referencing_column.attrelid = inbound_fk.conrelid
                     AND referencing_column.attnum = key_pair.referencing_attribute_number
                    JOIN pg_catalog.pg_attribute AS movement_column
                      ON movement_column.attrelid = inbound_fk.confrelid
                     AND movement_column.attnum = key_pair.movement_attribute_number;

                    EXECUTE format(
                        'SELECT count(*) FROM nexa.%I AS referencing_row ' ||
                        'JOIN nexa.stock_movements AS movement_row ON %s ' ||
                        'WHERE movement_row.created_by = $1',
                        inbound_fk.table_name,
                        join_predicate)
                    INTO referenced_count
                    USING 'REV848_STOCK_LEDGER_MOVEMENT';

                    IF referenced_count > 0 THEN
                        RAISE EXCEPTION
                            'REV848 stock purge refused: % marked movements are referenced by nexa.% through constraint %.',
                            referenced_count,
                            inbound_fk.table_name,
                            inbound_fk.conname;
                    END IF;
                END LOOP;

                DELETE FROM nexa.stock_movements
                WHERE created_by = 'REV848_STOCK_LEDGER_MOVEMENT';
                GET DIAGNOSTICS deleted_count = ROW_COUNT;

                IF deleted_count <> expected_count THEN
                    RAISE EXCEPTION
                        'REV848 stock purge count changed during execution: expected %, deleted %.',
                        expected_count,
                        deleted_count;
                END IF;

                RAISE NOTICE 'STORES_REV848_PURGE_DELETED_COUNT=%', deleted_count;
            END;
            $stores_rev848_purge$;

            DO $stores_line_empty_check$
            DECLARE
                line_table text;
                line_count bigint;
            BEGIN
                FOREACH line_table IN ARRAY ARRAY[
                    'goods_receipt_lines',
                    'delivery_challan_lines',
                    'inventory_transaction_lines',
                    'material_return_lines']
                LOOP
                    EXECUTE format('SELECT count(*) FROM nexa.%I', line_table)
                    INTO line_count;
                    IF line_count <> 0 THEN
                        RAISE EXCEPTION
                            'Stores tenant hardening refused: nexa.% contains % rows; silent backfill is forbidden.',
                            line_table,
                            line_count;
                    END IF;
                END LOOP;
            END;
            $stores_line_empty_check$;

            ALTER TABLE nexa.goods_receipts
                ADD CONSTRAINT uq_nexa_goods_receipts_company_id_id
                UNIQUE (company_id, id);
            ALTER TABLE nexa.delivery_challans
                ADD CONSTRAINT uq_nexa_delivery_challans_company_id_id
                UNIQUE (company_id, id);
            ALTER TABLE nexa.inventory_transactions
                ADD CONSTRAINT uq_nexa_inventory_transactions_company_id_id
                UNIQUE (company_id, id);
            ALTER TABLE nexa.material_returns
                ADD CONSTRAINT uq_nexa_material_returns_company_id_id
                UNIQUE (company_id, id);

            ALTER TABLE nexa.goods_receipt_lines
                ADD COLUMN company_id bigint NOT NULL,
                ADD CONSTRAINT fk_nexa_goods_receipt_lines_company
                    FOREIGN KEY (company_id) REFERENCES nexa.companies (id),
                ADD CONSTRAINT uq_nexa_goods_receipt_lines_company_id_id
                    UNIQUE (company_id, id),
                ADD CONSTRAINT fk_nexa_goods_receipt_lines_company_header
                    FOREIGN KEY (company_id, goods_receipt_id)
                    REFERENCES nexa.goods_receipts (company_id, id);
            CREATE INDEX ix_nexa_goods_receipt_lines_company_header
                ON nexa.goods_receipt_lines (company_id, goods_receipt_id);

            ALTER TABLE nexa.delivery_challan_lines
                ADD COLUMN company_id bigint NOT NULL,
                ADD CONSTRAINT fk_nexa_delivery_challan_lines_company
                    FOREIGN KEY (company_id) REFERENCES nexa.companies (id),
                ADD CONSTRAINT uq_nexa_delivery_challan_lines_company_id_id
                    UNIQUE (company_id, id),
                ADD CONSTRAINT fk_nexa_delivery_challan_lines_company_header
                    FOREIGN KEY (company_id, delivery_challan_id)
                    REFERENCES nexa.delivery_challans (company_id, id);
            CREATE INDEX ix_nexa_delivery_challan_lines_company_header
                ON nexa.delivery_challan_lines (company_id, delivery_challan_id);

            ALTER TABLE nexa.inventory_transaction_lines
                ADD COLUMN company_id bigint NOT NULL,
                ADD CONSTRAINT fk_nexa_inventory_transaction_lines_company
                    FOREIGN KEY (company_id) REFERENCES nexa.companies (id),
                ADD CONSTRAINT uq_nexa_inventory_transaction_lines_company_id_id
                    UNIQUE (company_id, id),
                ADD CONSTRAINT fk_nexa_inventory_transaction_lines_company_header
                    FOREIGN KEY (company_id, inventory_transaction_id)
                    REFERENCES nexa.inventory_transactions (company_id, id);
            CREATE INDEX ix_nexa_inventory_transaction_lines_company_header
                ON nexa.inventory_transaction_lines (company_id, inventory_transaction_id);

            ALTER TABLE nexa.material_return_lines
                ADD COLUMN company_id bigint NOT NULL,
                ADD CONSTRAINT fk_nexa_material_return_lines_company
                    FOREIGN KEY (company_id) REFERENCES nexa.companies (id),
                ADD CONSTRAINT uq_nexa_material_return_lines_company_id_id
                    UNIQUE (company_id, id),
                ADD CONSTRAINT fk_nexa_material_return_lines_company_header
                    FOREIGN KEY (company_id, material_return_id)
                    REFERENCES nexa.material_returns (company_id, id);
            CREATE INDEX ix_nexa_material_return_lines_company_header
                ON nexa.material_return_lines (company_id, material_return_id);

            DROP INDEX nexa.ux_nexa_stock_movements_source_key;

            ALTER TABLE nexa.stock_movements
                ADD COLUMN goods_receipt_line_id bigint NULL,
                ADD COLUMN delivery_challan_line_id bigint NULL,
                ADD COLUMN inventory_transaction_line_id bigint NULL,
                ADD COLUMN material_return_line_id bigint NULL,
                ADD CONSTRAINT fk_nexa_stock_movements_goods_receipt_line
                    FOREIGN KEY (company_id, goods_receipt_line_id)
                    REFERENCES nexa.goods_receipt_lines (company_id, id),
                ADD CONSTRAINT fk_nexa_stock_movements_delivery_challan_line
                    FOREIGN KEY (company_id, delivery_challan_line_id)
                    REFERENCES nexa.delivery_challan_lines (company_id, id),
                ADD CONSTRAINT fk_nexa_stock_movements_inventory_transaction_line
                    FOREIGN KEY (company_id, inventory_transaction_line_id)
                    REFERENCES nexa.inventory_transaction_lines (company_id, id),
                ADD CONSTRAINT fk_nexa_stock_movements_material_return_line
                    FOREIGN KEY (company_id, material_return_line_id)
                    REFERENCES nexa.material_return_lines (company_id, id),
                ADD CONSTRAINT ck_nexa_stock_movement_source_count
                    CHECK (num_nonnulls(
                        goods_receipt_line_id,
                        delivery_challan_line_id,
                        inventory_transaction_line_id,
                        material_return_line_id) <= 1),
                ADD CONSTRAINT ck_nexa_stock_movement_adjustment_source
                    CHECK (
                        (
                            num_nonnulls(
                                goods_receipt_line_id,
                                delivery_challan_line_id,
                                inventory_transaction_line_id,
                                material_return_line_id) = 0
                            AND movement_type IN ('ADJUST_IN', 'ADJUST_OUT')
                            AND NULLIF(btrim(approved_override_reference), '') IS NOT NULL
                        )
                        OR
                        (
                            num_nonnulls(
                                goods_receipt_line_id,
                                delivery_challan_line_id,
                                inventory_transaction_line_id,
                                material_return_line_id) = 1
                            AND movement_type NOT IN ('ADJUST_IN', 'ADJUST_OUT')
                        ));

            CREATE UNIQUE INDEX ux_nexa_stock_movements_goods_receipt_line
                ON nexa.stock_movements (company_id, goods_receipt_line_id, movement_type)
                WHERE goods_receipt_line_id IS NOT NULL;
            CREATE UNIQUE INDEX ux_nexa_stock_movements_delivery_challan_line
                ON nexa.stock_movements (company_id, delivery_challan_line_id, movement_type)
                WHERE delivery_challan_line_id IS NOT NULL;
            CREATE UNIQUE INDEX ux_nexa_stock_movements_inventory_transaction_line
                ON nexa.stock_movements (company_id, inventory_transaction_line_id, movement_type)
                WHERE inventory_transaction_line_id IS NOT NULL;
            CREATE UNIQUE INDEX ux_nexa_stock_movements_material_return_line
                ON nexa.stock_movements (company_id, material_return_line_id, movement_type)
                WHERE material_return_line_id IS NOT NULL;
            CREATE INDEX ix_nexa_stock_movements_source_reporting
                ON nexa.stock_movements (
                    company_id,
                    lower(source_module),
                    lower(source_reference),
                    movement_type,
                    item_id);

            ALTER TABLE nexa.stock_movements
                VALIDATE CONSTRAINT ck_nexa_stock_quantity_positive;

            CREATE FUNCTION nexa.reject_stock_movement_history_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $stores_stock_append_only$
            BEGIN
                RAISE EXCEPTION USING
                    ERRCODE = '55000',
                    MESSAGE = format(
                        'nexa.stock_movements is append-only; %s is forbidden. Post a reversing movement instead.',
                        TG_OP);
                RETURN NULL;
            END;
            $stores_stock_append_only$;

            CREATE TRIGGER trg_nexa_stock_movements_append_only
            BEFORE UPDATE OR DELETE ON nexa.stock_movements
            FOR EACH ROW
            EXECUTE FUNCTION nexa.reject_stock_movement_history_mutation();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER trg_nexa_stock_movements_append_only
                ON nexa.stock_movements;
            DROP FUNCTION nexa.reject_stock_movement_history_mutation();

            DO $stores_restore_not_valid$
            DECLARE
                constraint_definition text;
            BEGIN
                SELECT pg_get_constraintdef(constraint_row.oid, true)
                INTO constraint_definition
                FROM pg_catalog.pg_constraint AS constraint_row
                WHERE constraint_row.conrelid = 'nexa.stock_movements'::regclass
                  AND constraint_row.conname = 'ck_nexa_stock_quantity_positive'
                  AND constraint_row.contype = 'c';

                IF constraint_definition IS NULL THEN
                    RAISE EXCEPTION
                        'Stores rollback refused: ck_nexa_stock_quantity_positive is missing.';
                END IF;

                ALTER TABLE nexa.stock_movements
                    DROP CONSTRAINT ck_nexa_stock_quantity_positive;
                EXECUTE format(
                    'ALTER TABLE nexa.stock_movements ADD CONSTRAINT %I %s NOT VALID',
                    'ck_nexa_stock_quantity_positive',
                    constraint_definition);
            END;
            $stores_restore_not_valid$;

            DROP INDEX nexa.ix_nexa_stock_movements_source_reporting;
            DROP INDEX nexa.ux_nexa_stock_movements_material_return_line;
            DROP INDEX nexa.ux_nexa_stock_movements_inventory_transaction_line;
            DROP INDEX nexa.ux_nexa_stock_movements_delivery_challan_line;
            DROP INDEX nexa.ux_nexa_stock_movements_goods_receipt_line;

            ALTER TABLE nexa.stock_movements
                DROP CONSTRAINT ck_nexa_stock_movement_adjustment_source,
                DROP CONSTRAINT ck_nexa_stock_movement_source_count,
                DROP CONSTRAINT fk_nexa_stock_movements_material_return_line,
                DROP CONSTRAINT fk_nexa_stock_movements_inventory_transaction_line,
                DROP CONSTRAINT fk_nexa_stock_movements_delivery_challan_line,
                DROP CONSTRAINT fk_nexa_stock_movements_goods_receipt_line,
                DROP COLUMN material_return_line_id,
                DROP COLUMN inventory_transaction_line_id,
                DROP COLUMN delivery_challan_line_id,
                DROP COLUMN goods_receipt_line_id;

            CREATE UNIQUE INDEX ux_nexa_stock_movements_source_key
                ON nexa.stock_movements (
                    company_id,
                    item_id,
                    lower(source_module),
                    lower(source_reference),
                    movement_type);

            DROP INDEX nexa.ix_nexa_material_return_lines_company_header;
            ALTER TABLE nexa.material_return_lines
                DROP CONSTRAINT fk_nexa_material_return_lines_company_header,
                DROP CONSTRAINT uq_nexa_material_return_lines_company_id_id,
                DROP CONSTRAINT fk_nexa_material_return_lines_company,
                DROP COLUMN company_id;

            DROP INDEX nexa.ix_nexa_inventory_transaction_lines_company_header;
            ALTER TABLE nexa.inventory_transaction_lines
                DROP CONSTRAINT fk_nexa_inventory_transaction_lines_company_header,
                DROP CONSTRAINT uq_nexa_inventory_transaction_lines_company_id_id,
                DROP CONSTRAINT fk_nexa_inventory_transaction_lines_company,
                DROP COLUMN company_id;

            DROP INDEX nexa.ix_nexa_delivery_challan_lines_company_header;
            ALTER TABLE nexa.delivery_challan_lines
                DROP CONSTRAINT fk_nexa_delivery_challan_lines_company_header,
                DROP CONSTRAINT uq_nexa_delivery_challan_lines_company_id_id,
                DROP CONSTRAINT fk_nexa_delivery_challan_lines_company,
                DROP COLUMN company_id;

            DROP INDEX nexa.ix_nexa_goods_receipt_lines_company_header;
            ALTER TABLE nexa.goods_receipt_lines
                DROP CONSTRAINT fk_nexa_goods_receipt_lines_company_header,
                DROP CONSTRAINT uq_nexa_goods_receipt_lines_company_id_id,
                DROP CONSTRAINT fk_nexa_goods_receipt_lines_company,
                DROP COLUMN company_id;

            ALTER TABLE nexa.material_returns
                DROP CONSTRAINT uq_nexa_material_returns_company_id_id;
            ALTER TABLE nexa.inventory_transactions
                DROP CONSTRAINT uq_nexa_inventory_transactions_company_id_id;
            ALTER TABLE nexa.delivery_challans
                DROP CONSTRAINT uq_nexa_delivery_challans_company_id_id;
            ALTER TABLE nexa.goods_receipts
                DROP CONSTRAINT uq_nexa_goods_receipts_company_id_id;
            """);

        // The REV848 purge is irreversible. Down removes the schema hardening
        // but cannot reconstruct the deleted orphan ledger rows.
    }
}
