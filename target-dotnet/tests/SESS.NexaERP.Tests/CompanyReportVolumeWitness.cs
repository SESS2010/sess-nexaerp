#if REPORT_VOLUME_WITNESS
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    // Synthetic query-volume measurement after the real business assertions.
    // Separate copies retain column/index layout, but are not a governed posting ledger.
    private static async Task RunReportVolumeWitness(
        DbContextOptions<NexaErpDbContext> options, string runtimeConnection)
    {
        var evidence=Environment.GetEnvironmentVariable("SESS_REPORT_VOLUME_EVIDENCE")
            ?? throw new InvalidOperationException("Set SESS_REPORT_VOLUME_EVIDENCE.");
        Directory.CreateDirectory(evidence);
        await using var owner=new NexaErpDbContext(options);
        var settings=new NpgsqlConnectionStringBuilder(owner.Database.GetConnectionString());
        Assert.Equal("advance_parser",settings.Database);
        Assert.Equal("127.0.0.1",settings.Host);
        Assert.NotEqual(5432,settings.Port);
        await using var connection=new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync();
        DateOnly fixtureDate;
        await using(var clock=new NpgsqlCommand("SELECT greatest(current_date,(current_timestamp AT TIME ZONE 'UTC')::date)",connection))
        await using(var reader=await clock.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            fixtureDate=reader.GetFieldValue<DateOnly>(0);
        }
        async Task Execute(string sql)
        {
            await using var command=new NpgsqlCommand(sql,connection) {CommandTimeout=3600};
            await command.ExecuteNonQueryAsync();
        }
        async Task Progress(string message)=>await File.AppendAllTextAsync(
            Path.Combine(evidence,"progress.txt"),DateTimeOffset.UtcNow.ToString("O")+" "+message+Environment.NewLine);
        var setup=Stopwatch.StartNew();
        await File.WriteAllTextAsync(Path.Combine(evidence,"progress.txt"),"Business assertions passed; preparing separate synthetic query tables."+Environment.NewLine);
        await Execute("""
            CREATE SCHEMA report_volume;
            CREATE TABLE report_volume.items (LIKE advance.items INCLUDING ALL EXCLUDING CONSTRAINTS);
            CREATE TABLE report_volume.stock_movements (LIKE advance.stock_movements INCLUDING ALL EXCLUDING CONSTRAINTS);
            INSERT INTO report_volume.items SELECT * FROM advance.items;
            INSERT INTO report_volume.stock_movements SELECT * FROM advance.stock_movements;
            CREATE TEMP TABLE volume_template_item AS SELECT * FROM advance.items WHERE "ItemCode"='TRIAL-ITEM-001';
            CREATE TEMP TABLE volume_template_movement AS
              SELECT * FROM advance.stock_movements WHERE "MovementLeg"='RECEIPT_IN' AND "GoodsReceiptLineId" IS NOT NULL
              ORDER BY "CreatedAt","Id" LIMIT 1;
            DO $guard$ BEGIN
              IF (SELECT count(*) FROM volume_template_item)<>1 OR (SELECT count(*) FROM volume_template_movement)<>1
                THEN RAISE EXCEPTION 'Volume fixture templates missing'; END IF;
            END $guard$;
            GRANT USAGE ON SCHEMA report_volume TO nexa_erp_runtime;
            GRANT SELECT ON report_volume.items,report_volume.stock_movements TO nexa_erp_runtime;
            ALTER DATABASE advance_parser SET temp_file_limit='4GB';
            """);
        var existingItems=await owner.Items.CountAsync();
        for(var start=1;start<=300000-existingItems;start+=10000)
        {
            var end=Math.Min(start+9999,300000-existingItems);
            await Execute($$"""
                INSERT INTO report_volume.items
                SELECT clone.* FROM volume_template_item t CROSS JOIN generate_series({{start}},{{end}}) seq(value)
                CROSS JOIN LATERAL jsonb_populate_record(NULL::report_volume.items,to_jsonb(t)||jsonb_build_object(
                  'Id',md5('report-volume-item-'||seq.value)::uuid,'ItemCode','REPORT-VOLUME-'||lpad(seq.value::text,6,'0'),
                  'Name','Synthetic report-volume item '||seq.value,'Barcode',NULL,'PartNumber',NULL,
                  'CreatedBy','REPORT_VOLUME_FIXTURE','Version',0)) clone;
                """);
        }
        await Execute("""
            CREATE TEMP TABLE volume_items AS
              SELECT "Id",row_number() OVER(ORDER BY "ItemCode")::integer AS ordinal FROM report_volume.items;
            CREATE UNIQUE INDEX ON volume_items(ordinal);
            ANALYZE volume_items;
            """);
        await Progress("300000 items prepared");
        var existingMovements=await owner.StockMovements.CountAsync();
        for(var start=1;start<=2000000-existingMovements;start+=20000)
        {
            var end=Math.Min(start+19999,2000000-existingMovements);
            await Execute($$"""
                INSERT INTO report_volume.stock_movements
                SELECT clone.* FROM volume_template_movement t
                CROSS JOIN generate_series({{start}},{{end}}) seq(value)
                JOIN volume_items i ON i.ordinal=((seq.value-1)%300000)+1
                CROSS JOIN LATERAL jsonb_populate_record(NULL::report_volume.stock_movements,to_jsonb(t)||jsonb_build_object(
                  'Id',md5('report-volume-movement-'||seq.value)::uuid,'ItemId',i."Id",
                  'InventoryLotId',NULL,'InventorySerialId',NULL,'GoodsReceiptLineId',NULL,'OriginGoodsReceiptLineId',NULL,
                  'GoodsReceiptLineLotAllocationId',NULL,'QcInspectionRevisionId',NULL,'QcInspectionLotDispositionId',NULL,
                  'StockPostingBatchId',NULL,'BatchLineOrdinal',NULL,'PostingIdentity','REPORT-VOLUME-'||seq.value,
                  'MovementType','REPORT_VOLUME_FIXTURE','ReferenceType','REPORT_VOLUME_FIXTURE','ReferenceNumber','VOLUME-'||seq.value,
                  'MovementLeg',CASE WHEN (seq.value-1)/300000=0 THEN 'RECEIPT_IN' WHEN ((seq.value-1)/300000)%2=1 THEN 'ISSUE_OUT' ELSE 'TRANSFER_IN' END,
                  'QuantityIn',CASE WHEN (seq.value-1)/300000=0 THEN 10 WHEN ((seq.value-1)/300000)%2=0 THEN 1 ELSE 0 END,
                  'QuantityOut',CASE WHEN ((seq.value-1)/300000)%2=1 THEN 1 ELSE 0 END,
                  'PostingDate',DATE '{{fixtureDate:yyyy-MM-dd}}'-(6-(seq.value-1)/300000)*30,'CreatedBy','REPORT_VOLUME_FIXTURE','Version',0)) clone;
                """);
            if(end%100000==0) await Progress(end+" synthetic movements prepared");
        }
        await Execute("ANALYZE report_volume.items; ANALYZE report_volume.stock_movements;");
        await using(var counts=new NpgsqlCommand("SELECT (SELECT count(*) FROM report_volume.items),(SELECT count(*) FROM report_volume.stock_movements)",connection))
        {
            await using var reader=await counts.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(300000,reader.GetInt64(0));
            Assert.Equal(2000000,reader.GetInt64(1));
        }
        setup.Stop();
        await Progress("Exact 300000 items / 2000000 movements verified and analyzed");
        var company=await owner.Companies.SingleAsync(row=>row.Code=="SESS_PVT_LTD");
        var employee=await owner.Employees.SingleAsync(row=>row.EmployeeCode=="SESS-01");
        var assignments=await owner.EmployeeRoleAssignments
            .Where(row=>row.EmployeeId==employee.Id&&row.CompanyId==company.Id&&row.EffectiveTo==null)
            .Select(row=>row.Id).ToArrayAsync();
        await using var runtime=new NpgsqlConnection(runtimeConnection);
        await runtime.OpenAsync();
        var today=fixtureDate;
        var request=new CompanyReportRequest(FromDate:today.AddDays(-90),ToDate:today);
        NpgsqlCommand ReportCommand(string key,bool export)
        {
            // Only the two scaled relation names differ from production SQL.
            var sql=StockReportSql.Build(key=="movement-roll-forward")
                .Replace("advance.stock_movements","report_volume.stock_movements",StringComparison.Ordinal)
                .Replace("advance.items","report_volume.items",StringComparison.Ordinal);
            var command=new NpgsqlCommand(sql,runtime) {CommandTimeout=300};
            command.Parameters.AddWithValue("organization","SESS_PVT_LTD");
            command.Parameters.AddWithValue("employee",employee.Id);
            command.Parameters.AddWithValue("assignments",NpgsqlDbType.Array|NpgsqlDbType.Uuid,assignments);
            command.Parameters.AddWithValue("page_key","reports."+key);
            command.Parameters.AddWithValue("commercial",false);
            command.Parameters.AddWithValue("export",export);
            command.Parameters.AddWithValue("login","report-volume-witness");
            command.Parameters.AddWithValue("correlation",Guid.NewGuid().ToString("N"));
            command.Parameters.AddWithValue("from_date",NpgsqlDbType.Date,key=="movement-roll-forward"?request.FromDate!.Value:DateOnly.MinValue);
            command.Parameters.AddWithValue("to_date",NpgsqlDbType.Date,today);
            command.Parameters.AddWithValue("mode","summary");
            command.Parameters.Add("metric",NpgsqlDbType.Text).Value=DBNull.Value;
            command.Parameters.Add("group_filter",NpgsqlDbType.Jsonb).Value=DBNull.Value;
            command.Parameters.AddWithValue("offset",0L);
            command.Parameters.AddWithValue("page_size",100);
            command.Parameters.AddWithValue("report_timezone","UTC");
            return command;
        }
        var measurements=new List<object>();
        async Task SaveMeasurements()=>await File.WriteAllTextAsync(Path.Combine(evidence,"measurements.json"),
            JsonSerializer.Serialize(new {items=300000,movements=2000000,toDate=fixtureDate,tempFileLimitPerBackend="4GB",
                fixture="Separate table copies with production column/index layout; no posting-chain constraints or triggers. Production SQL differs only in the two scaled relation names.",
                syntheticSetupSeconds=setup.Elapsed.TotalSeconds,measurements}));
        foreach(var key in new[]{"stock-balance","movement-roll-forward"})
        {
            await Progress("Measuring "+key);
            var timer=Stopwatch.StartNew();
            var header=await ObserveSingleReportCommand(async ()=>
            {
                await using var command=ReportCommand(key,false);
                await using var reader=await command.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal(0,reader.GetInt32(0));
                using var json=JsonDocument.Parse(reader.GetString(2));
                var result=json.RootElement.Clone();
                Assert.True(result.GetProperty("allowed").GetBoolean());
                var rows=0; while(await reader.ReadAsync()) rows++;
                Assert.Equal(100,rows);
                return result;
            });
            timer.Stop();
            Assert.True(header.GetProperty("totalRows").GetInt64()>=300000);
            measurements.Add(new {report=key,operation="summary SQL",seconds=timer.Elapsed.TotalSeconds,
                totalRows=header.GetProperty("totalRows").GetInt64(),totalSourceRows=header.GetProperty("totalSourceRows").GetInt64()});
            await SaveMeasurements();
            await using var explain=ReportCommand(key,false);
            explain.CommandText="EXPLAIN (ANALYZE,BUFFERS,SETTINGS,FORMAT JSON) "+explain.CommandText;
            explain.CommandTimeout=1800;
            await File.WriteAllTextAsync(Path.Combine(evidence,key+"-explain.json"),(string)(await explain.ExecuteScalarAsync())!);
        }
        await using(var explain=ReportCommand("movement-roll-forward",true))
        {
            explain.CommandText="EXPLAIN (SETTINGS,FORMAT JSON) "+explain.CommandText;
            await File.WriteAllTextAsync(Path.Combine(evidence,"export-estimated-plan.json"),(string)(await explain.ExecuteScalarAsync())!);
        }
        await Progress("Exporting complete movement roll-forward");
        var exportTimer=Stopwatch.StartNew();
        var export=await ObserveSingleReportCommand(async ()=>
        {
            await using var command=ReportCommand("movement-roll-forward",true);
            await using var reader=await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess);
            Assert.True(await reader.ReadAsync());
            Assert.Equal(0,reader.GetInt32(0));
            using var header=JsonDocument.Parse(reader.GetString(2));
            Assert.True(header.RootElement.GetProperty("allowed").GetBoolean());
            await Progress("Export header received after "+exportTimer.Elapsed.TotalSeconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" seconds");
            using var workbook=new ReportWorkbook(ReportDefinitions.Find("movement-roll-forward"),"SESS_PVT_LTD",request,header.RootElement);
            long sourceRows=0;
            while(await reader.ReadAsync())
            {
                var kind=reader.GetInt32(0);
                using var row=JsonDocument.Parse(reader.GetString(2));
                workbook.Write(kind,row.RootElement);
                if(kind==2 && ++sourceRows%100000==0) await Progress(sourceRows+" detail rows written");
            }
            Assert.Equal(header.RootElement.GetProperty("totalSourceRows").GetInt64(),sourceRows);
            return workbook.Complete();
        });
        exportTimer.Stop();
        await File.WriteAllBytesAsync(Path.Combine(evidence,"movement-roll-forward.xlsx"),export);
        measurements.Add(new {report="movement-roll-forward",operation="SQL and Excel writer",seconds=exportTimer.Elapsed.TotalSeconds,bytes=export.Length});
        await SaveMeasurements();

        // Read the produced XML as a stream: a two-million-row workbook must not
        // be loaded into an object model just to verify its sheet boundaries.
        using(var archive=new System.IO.Compression.ZipArchive(new MemoryStream(export),System.IO.Compression.ZipArchiveMode.Read))
        {
            var ns=System.Xml.Linq.XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            using var manifest=archive.GetEntry("xl/workbook.xml")!.Open();
            var names=System.Xml.Linq.XDocument.Load(manifest).Descendants(ns+"sheet")
                .Select(row=>(string)row.Attribute("name")!).ToArray();
            long detailRows=0,summaryRows=0,links=0;
            var locationPattern=new System.Text.RegularExpressions.Regex(
                "^'(?<sheet>[^']+)'!A(?<start>[0-9]+):[A-Z]+(?<end>[0-9]+)$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            for(var index=0;index<names.Length;index++)
            {
                using var stream=archive.GetEntry($"xl/worksheets/sheet{index+1}.xml")!.Open();
                using var xml=System.Xml.XmlReader.Create(stream,new System.Xml.XmlReaderSettings
                    { DtdProcessing=System.Xml.DtdProcessing.Prohibit,IgnoreWhitespace=true });
                long rows=0;
                while(xml.Read())
                {
                    if(xml.NodeType!=System.Xml.XmlNodeType.Element) continue;
                    if(xml.LocalName=="row") rows++;
                    if(xml.LocalName!="hyperlink") continue;
                    links++;
                    var match=locationPattern.Match(xml.GetAttribute("location")??"");
                    Assert.True(match.Success);
                    var target=match.Groups["sheet"].Value;
                    Assert.Contains(target,names);
                    var start=long.Parse(match.Groups["start"].Value,System.Globalization.CultureInfo.InvariantCulture);
                    var end=long.Parse(match.Groups["end"].Value,System.Globalization.CultureInfo.InvariantCulture);
                    Assert.InRange(start,2,1_048_576);
                    Assert.InRange(end,start,target=="Details"?1_048_576:951_426);
                }
                Assert.InRange(rows,1,1_048_576);
                if(names[index].StartsWith("Details",StringComparison.Ordinal)) detailRows+=rows-1;
                if(names[index].StartsWith("Summary",StringComparison.Ordinal)) summaryRows+=rows-1;
            }
            Assert.Equal(2_000_000,detailRows);
            Assert.True(summaryRows>=300_000);
            Assert.True(links>=summaryRows);
            await Progress($"Excel XML verified: {summaryRows} summary rows, {detailRows} detail rows, {links} drill-through links, {names.Length} sheets");
        }
        await Progress("Volume query and export measurement complete");
    }
}
#endif
