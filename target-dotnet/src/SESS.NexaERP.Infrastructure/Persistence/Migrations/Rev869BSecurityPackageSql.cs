namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

public static class Rev869BSecurityPackageSql
{
    public static string InstallCommandContext => Rev869BCommandContextSql.Install;
    public static string InstallControlledMutation => Rev869BControlledMutationSql.Install;
    public static string RemoveControlledMutation => Rev869BControlledMutationSql.Remove;
    public static string RemoveCommandContext => Rev869BCommandContextSql.Remove;
}
