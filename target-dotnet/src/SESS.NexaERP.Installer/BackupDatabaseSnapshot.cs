using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Npgsql;

internal sealed record VerifiedBackupConfiguration(
    string ConnectionEnvironment, string ExpectedHost, int ExpectedPort,
    string ExpectedDatabase, string ExpectedSystemIdentifier,
    string PostgreSqlBin, string BackupRoot, string WorkingRoot);

internal sealed record BackupFileEvidence(string Name,long Length,string Sha256);
internal sealed record BackupDatabaseEvidence(string BootstrapRole,int ServerVersion,
    string Roles,string Metadata,SortedDictionary<string,long> TableCounts);
internal sealed record VerifiedBackupManifest(int Format,string State,Guid RootId,Guid RunId,
    DateTimeOffset StartedUtc,DateTimeOffset VerifiedUtc,bool Weekly,string Database,
    string SourceSystemIdentifier,BackupDatabaseEvidence DatabaseEvidence,
    BackupFileEvidence[] Files);

internal static class BackupDatabaseSnapshot
{
    internal static async Task<BackupDatabaseEvidence> ReadAsync(NpgsqlConnection connection,NpgsqlTransaction? transaction)
    {
        var bootstrap=await Scalar(connection,transaction,"SELECT rolname::text FROM pg_roles WHERE oid=10");
        var version=int.Parse(await Scalar(connection,transaction,"SELECT current_setting('server_version_num')"));
        var roles=await Scalar(connection,transaction,RolesSql);
        var metadata=NormalizeDefinitions(await Scalar(connection,transaction,MetadataSql));
        var names=new List<(string Schema,string Table)>();
        await using(var command=new NpgsqlCommand("""
            SELECT n.nspname,c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE c.relkind IN ('r','p') AND n.nspname !~ '^pg_' AND n.nspname<>'information_schema'
            ORDER BY n.nspname,c.relname
            """,connection,transaction))
        await using(var reader=await command.ExecuteReaderAsync())
            while(await reader.ReadAsync()) names.Add((reader.GetString(0),reader.GetString(1)));
        var counts=new SortedDictionary<string,long>(StringComparer.Ordinal);
        foreach(var name in names)
        {
            var table=Quote(name.Schema)+"."+Quote(name.Table);
            await using var command=new NpgsqlCommand("SELECT count(*) FROM "+table,connection,transaction) { CommandTimeout=1800 };
            counts.Add(table,(long)(await command.ExecuteScalarAsync())!);
        }
        return new(bootstrap,version,roles,metadata,counts);
    }



    internal static string NormalizeSqlDefinition(string sql)
    {
        // Only literal, unbounded varchar-to-text casts are normalized.
        // Bounded varchar casts may truncate and must remain distinguishable.
        const string literal=@"'(?:[^'\\]|'')*'";
        var input=sql;
        sql=Regex.Replace(input,@"(?<open>\()?ARRAY\[(?<values>"+literal+@"::character varying(?:, "+literal+@"::character varying)*)\](?(open)\))::text\[\]",
            match=>
            {
                if(!IsCodeAt(input,match.Index) ||
                    (match.Index>0 && (char.IsLetterOrDigit(input[match.Index-1]) || input[match.Index-1]=='_')))
                    return match.Value;
                var values=Regex.Replace(match.Groups["values"].Value,
                    @"(?<literal>"+literal+@")::character varying",
                    value=>value.Groups["literal"].Value+"::text",
                    RegexOptions.CultureInvariant,TimeSpan.FromSeconds(2));
                return "ARRAY["+values+"]";
            },RegexOptions.CultureInvariant,TimeSpan.FromSeconds(2));
        input=sql;
        return Regex.Replace(input,@"(?<open>\()?(?<value>"+literal+@")::character varying(?(open)\))::text",
            match=>IsCodeAt(input,match.Index) ? match.Groups["value"].Value+"::text" : match.Value,
            RegexOptions.CultureInvariant,TimeSpan.FromSeconds(2));
    }

    private static bool IsCodeAt(string sql,int position)
    {
        var quote='\0';
        var escapeString=false;
        for(var i=0;i<position;i++)
        {
            var c=sql[i];
            if(quote!='\0')
            {
                if(quote=='\'' && escapeString && c=='\\') { i++; continue; }
                if(c==quote)
                {
                    if(i+1<position && sql[i+1]==quote) i++;
                    else quote='\0';
                }
            }
            else if(c=='\'' || c=='"')
            {
                quote=c;
                escapeString=c=='\'' && i>0 && (sql[i-1]=='E' || sql[i-1]=='e')
                    && (i<2 || !(char.IsLetterOrDigit(sql[i-2]) || sql[i-2]=='_'));
            }
        }
        return quote=='\0';
    }

    private static string NormalizeDefinitions(string metadata)
    {
        var root=JsonNode.Parse(metadata)!.AsObject();
        if(root["constraints"] is JsonArray constraints)
            foreach(var item in constraints)
                item!["definition"]=NormalizeSqlDefinition(item["definition"]!.GetValue<string>());
        if(root["indexes"] is JsonArray indexes)
            for(var i=0;i<indexes.Count;i++)
                indexes[i]=NormalizeSqlDefinition(indexes[i]!.GetValue<string>());
        return root.ToJsonString();
    }
    internal static void RequireEqual(BackupDatabaseEvidence expected,BackupDatabaseEvidence actual)
    {
        if(expected.BootstrapRole!=actual.BootstrapRole || expected.ServerVersion/10000!=actual.ServerVersion/10000
            || expected.Roles!=actual.Roles || expected.Metadata!=actual.Metadata
            || expected.TableCounts.Count!=actual.TableCounts.Count || expected.TableCounts.Any(x=>!actual.TableCounts.TryGetValue(x.Key,out var count) || count!=x.Value))
            throw new InvalidOperationException("Restored database counts or security/schema metadata differ from the backup snapshot.");
    }

    internal static string Quote(string value)=>"\""+value.Replace("\"","\"\"")+"\"";

    internal static async Task<string> Scalar(NpgsqlConnection c,NpgsqlTransaction? t,string sql)
    {
        await using var command=new NpgsqlCommand(sql,c,t) { CommandTimeout=1800 };
        return Convert.ToString(await command.ExecuteScalarAsync(),System.Globalization.CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("Backup metadata query returned no value.");
    }

    // Password hashes are deliberately excluded; recovery re-establishes credentials.
    internal const string RolesSql="""
        SELECT jsonb_build_object(
          'roles',(SELECT jsonb_agg(jsonb_build_object('name',rolname,'super',rolsuper,'inherit',rolinherit,
            'createRole',rolcreaterole,'createDb',rolcreatedb,'login',rolcanlogin,'replication',rolreplication,
            'bypass',rolbypassrls,'limit',rolconnlimit,'validUntil',rolvaliduntil,'config',rolconfig) ORDER BY rolname)
            FROM pg_roles WHERE rolname !~ '^pg_'),
          'memberships',(SELECT jsonb_agg(jsonb_build_object('role',r.rolname,'member',m.rolname,
            'grantor',g.rolname,'admin',a.admin_option,'inherit',a.inherit_option,'set',a.set_option)
            ORDER BY r.rolname,m.rolname,g.rolname)
            FROM pg_auth_members a JOIN pg_roles r ON r.oid=a.roleid JOIN pg_roles m ON m.oid=a.member
            JOIN pg_roles g ON g.oid=a.grantor
            WHERE r.rolname !~ '^pg_' OR m.rolname !~ '^pg_'))::text
        """;

    internal const string MetadataSql="""
        SELECT jsonb_build_object(
          'database',(SELECT jsonb_build_object('name',datname,'owner',pg_get_userbyid(datdba),
            'acl',coalesce(datacl,acldefault('d',datdba))::text,'encoding',pg_encoding_to_char(encoding),'provider',datlocprovider,
            'collate',datcollate,'ctype',datctype,'locale',datlocale,'connectionLimit',datconnlimit)
            FROM pg_database WHERE datname=current_database()),
          'extensions',(SELECT jsonb_agg(jsonb_build_object('name',e.extname,'version',e.extversion,
            'owner',pg_get_userbyid(e.extowner),'schema',n.nspname,'relocatable',e.extrelocatable) ORDER BY e.extname)
            FROM pg_extension e JOIN pg_namespace n ON n.oid=e.extnamespace),
          'databaseSettings',(SELECT jsonb_agg(jsonb_build_object('role',CASE WHEN s.setrole=0 THEN '' ELSE pg_get_userbyid(s.setrole) END,
            'settings',s.setconfig) ORDER BY s.setrole=0,pg_get_userbyid(s.setrole))
            FROM pg_db_role_setting s WHERE s.setdatabase=(SELECT oid FROM pg_database WHERE datname=current_database())),
          'schemas',(SELECT jsonb_agg(jsonb_build_object('name',nspname,'owner',pg_get_userbyid(nspowner),
            'acl',coalesce(nspacl,acldefault('n',nspowner))::text) ORDER BY nspname) FROM pg_namespace
            WHERE nspname !~ '^pg_' AND nspname<>'information_schema'),
          'relations',(SELECT jsonb_agg(jsonb_build_object('schema',n.nspname,'name',c.relname,'kind',c.relkind,
            'owner',pg_get_userbyid(c.relowner),'acl',coalesce(c.relacl,acldefault(CASE WHEN c.relkind='S' THEN 's'::"char" ELSE 'r'::"char" END,c.relowner))::text,'rls',c.relrowsecurity,
            'forceRls',c.relforcerowsecurity,'options',c.reloptions) ORDER BY n.nspname,c.relname)
            FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname !~ '^pg_' AND n.nspname<>'information_schema'),
          'columns',(SELECT jsonb_agg(jsonb_build_object('schema',n.nspname,'table',c.relname,
            'name',a.attname,'number',(SELECT count(*) FROM pg_attribute ax WHERE ax.attrelid=a.attrelid AND ax.attnum>0 AND ax.attnum<=a.attnum AND NOT ax.attisdropped),'type',format_type(a.atttypid,a.atttypmod),
            'notNull',a.attnotnull,'identity',a.attidentity,'generated',a.attgenerated,
            'default',pg_get_expr(d.adbin,d.adrelid),'acl',coalesce(a.attacl,'{}'::aclitem[])::text) ORDER BY n.nspname,c.relname,a.attnum)
            FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            LEFT JOIN pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum
            WHERE a.attnum>0 AND NOT a.attisdropped AND n.nspname !~ '^pg_' AND n.nspname<>'information_schema'),
          'functions',(SELECT jsonb_agg(jsonb_build_object('schema',n.nspname,'name',p.proname,
            'args',pg_get_function_identity_arguments(p.oid),'owner',pg_get_userbyid(p.proowner),'acl',coalesce(p.proacl,acldefault('f',p.proowner))::text,
            'definer',p.prosecdef,'config',p.proconfig,'body',md5(p.prosrc),'language',l.lanname,
            'result',pg_get_function_result(p.oid),'volatile',p.provolatile,'strict',p.proisstrict)
            ORDER BY n.nspname,p.proname,pg_get_function_identity_arguments(p.oid))
            FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace JOIN pg_language l ON l.oid=p.prolang
            WHERE n.nspname !~ '^pg_' AND n.nspname<>'information_schema'),
          'constraints',(SELECT jsonb_agg(jsonb_build_object('schema',n.nspname,'table',c.relname,
            'name',k.conname,'definition',pg_get_constraintdef(k.oid,true),'validated',k.convalidated)
            ORDER BY n.nspname,c.relname,k.conname)
            FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname !~ '^pg_' AND n.nspname<>'information_schema'),
          'indexes',(SELECT jsonb_agg(indexdef ORDER BY schemaname,tablename,indexname) FROM pg_indexes
            WHERE schemaname !~ '^pg_' AND schemaname<>'information_schema'),
          'triggers',(SELECT jsonb_agg(jsonb_build_object('schema',n.nspname,'table',c.relname,'name',t.tgname,
            'definition',pg_get_triggerdef(t.oid),'enabled',t.tgenabled) ORDER BY n.nspname,c.relname,t.tgname)
            FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE NOT t.tgisinternal AND n.nspname !~ '^pg_' AND n.nspname<>'information_schema'),
          'defaultAcls',(SELECT jsonb_agg(jsonb_build_object('role',pg_get_userbyid(d.defaclrole),
            'schema',coalesce(n.nspname,''),'type',d.defaclobjtype,'acl',d.defaclacl::text)
            ORDER BY pg_get_userbyid(d.defaclrole),n.nspname,d.defaclobjtype)
            FROM pg_default_acl d LEFT JOIN pg_namespace n ON n.oid=d.defaclnamespace))::text
        """;
}
