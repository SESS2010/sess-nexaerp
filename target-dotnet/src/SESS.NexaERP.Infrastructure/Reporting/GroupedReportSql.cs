namespace SESS.NexaERP.Infrastructure.Reporting;

/// <summary>One statement for access, totals, paged groups and the source rows behind them.</summary>
internal static class GroupedReportSql
{
    // Source SQL exposes group_filter, labels, total_group, detail, metrics and sort_key.
    internal static string Build(ReportDefinition definition, string sourceSql, string groupPredicate = "true", string? availabilitySql = null)
    {
        var accessSql = ReportAccessSql.Access;
        var availabilityFilter = "true";
        var availabilityHeader = "";
        if (availabilitySql is not null)
        {
            availabilityFilter = "(SELECT ready FROM report_availability)";
            availabilityHeader = "'sourceReady',(SELECT ready FROM report_availability),'sourceIssue',(SELECT issue_code FROM report_availability),";
            accessSql = accessSql.Replace("report_audit AS (",
                "report_availability AS MATERIALIZED (" + availabilitySql + "),report_audit AS (",StringComparison.Ordinal)
                .Replace("CASE WHEN a.allowed THEN 'Export' ELSE 'Denied' END",
                    "CASE WHEN NOT a.allowed THEN 'Denied' WHEN (SELECT ready FROM report_availability) THEN 'Export' ELSE 'Unavailable' END",StringComparison.Ordinal)
                .Replace("CASE WHEN a.allowed THEN 'Success' ELSE 'Failure' END",
                    "CASE WHEN a.allowed AND (SELECT ready FROM report_availability) THEN 'Success' ELSE 'Failure' END",StringComparison.Ordinal)
                .Replace("WHERE @export OR NOT a.allowed","WHERE @export OR NOT a.allowed OR NOT (SELECT ready FROM report_availability)",StringComparison.Ordinal)
                .Replace("'mode',@mode)","'mode',@mode,'sourceIssue',(SELECT issue_code FROM report_availability))",StringComparison.Ordinal);
        }
        var metrics = definition.SummaryColumns.Where(column => column.Type == "number").ToArray();
        var aggregate = string.Join(",", metrics.Select(column => $"coalesce(sum((metrics->>'{column.Key}')::numeric),0) AS \"{column.Key}\""));
        var totals = string.Join(",", metrics.Select(column => $"sum(\"{column.Key}\") AS \"{column.Key}\""));
        var metricJson = string.Join(",", metrics.Select(column => $"'{column.Key}',g.\"{column.Key}\""));
        var totalJson = string.Join(",", metrics.Select(column => $"'{column.Key}',t.\"{column.Key}\""));
        return $$"""
            WITH {{ReportAccessSql.Context}},{{accessSql}},
            report_source AS NOT MATERIALIZED ({{sourceSql}}),
            selected_source AS NOT MATERIALIZED (
              SELECT * FROM report_source
              WHERE {{availabilityFilter}} AND (@group_filter IS NULL OR group_filter @> @group_filter)
                AND (@mode<>'details' OR @metric IS NULL OR coalesce((metrics->>@metric)::numeric,0)<>0)
            ),
            report_groups AS (
              SELECT group_filter,labels,total_group,count(*) AS source_count,{{aggregate}}
              FROM selected_source GROUP BY group_filter,labels,total_group HAVING {{groupPredicate}}
            ),
            numbered_groups AS MATERIALIZED (
              SELECT *,row_number() OVER group_order AS summary_ordinal,
                sum(source_count) OVER group_order-source_count+1 AS detail_start
              FROM report_groups
              WINDOW group_order AS (ORDER BY total_group::text,group_filter::text,labels::text ROWS UNBOUNDED PRECEDING)
            ),
            report_details AS (
              SELECT s.*,row_number() OVER(ORDER BY g.summary_ordinal,s.sort_key) AS detail_ordinal
              FROM selected_source s JOIN numbered_groups g
                ON s.group_filter=g.group_filter AND s.labels=g.labels AND s.total_group=g.total_group
              WHERE @export OR @mode='details'
            ),
            report_totals AS (
              SELECT total_group,{{totals}},sum(source_count) AS source_count,min(detail_start) AS detail_start
              FROM numbered_groups GROUP BY total_group
            ),
            output_rows AS (
              SELECT 1 AS kind,g.summary_ordinal AS ordinal,
                g.labels||jsonb_build_object('group',g.group_filter,{{metricJson}},
                  'detailStart',g.detail_start,'detailCount',g.source_count) AS payload
              FROM numbered_groups g
              WHERE (@export OR @mode='summary')
                AND (@export OR g.summary_ordinal>@offset AND g.summary_ordinal<=@offset+@page_size)
              UNION ALL
              SELECT 2,d.detail_ordinal,d.detail||d.metrics||jsonb_build_object('group',d.group_filter)
              FROM report_details d
              WHERE (@export OR @mode='details')
                AND (@export OR d.detail_ordinal>@offset AND d.detail_ordinal<=@offset+@page_size)
            )
            SELECT 0 AS kind,0::bigint AS ordinal,jsonb_build_object(
              'allowed',(SELECT allowed FROM access),'generatedAt',statement_timestamp(),'timeZone',@report_timezone,{{availabilityHeader}}
              'totalRows',CASE WHEN @mode='details' THEN coalesce((SELECT sum(source_count) FROM numbered_groups),0)
                ELSE (SELECT count(*) FROM numbered_groups) END,
              'totalSourceRows',coalesce((SELECT sum(source_count) FROM numbered_groups),0),
              'totals',coalesce((SELECT jsonb_agg(t.total_group||jsonb_build_object(
                'group',t.total_group,{{totalJson}},'detailStart',t.detail_start,'detailCount',t.source_count)
                ORDER BY t.total_group::text) FROM report_totals t),'[]'::jsonb)
            )::text AS payload
            UNION ALL SELECT kind,ordinal,payload::text FROM output_rows ORDER BY kind,ordinal
            """;
    }
}
