using System.Security.Cryptography;
using System.Text;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

internal static class StoresPart1SeedData
{
    internal const string SeedReason = "INITIAL_STORES_CONFIGURATION";
    internal static readonly DateTimeOffset EffectiveFrom = new(2026, 8, 27, 0, 0, 0, TimeSpan.Zero);
    internal static readonly Guid TechnicalDirectorEmployeeId = Rev866SeedData.Employees
        .Single(employee => employee.EmployeeCode == "SESS-001").Id;

    internal static readonly BusinessRuleConfigurationVersion[] ConfigurationVersions =
        BuildConfigurationVersions();

    private static BusinessRuleConfigurationVersion[] BuildConfigurationVersions()
    {
        var rules = new[]
        {
            Rule(StoresConfigurationRuleKeys.SerialCaptureThreshold, "DECIMAL", "5000", "INR"),
            Rule(StoresConfigurationRuleKeys.QcCompletionDays, "INTEGER", "2", "DAYS"),
            Rule(StoresConfigurationRuleKeys.EmergencyPurchaseCountPerMonth, "INTEGER", "10", "COUNT"),
            Rule(StoresConfigurationRuleKeys.EmergencyPurchaseValueLimit, "DECIMAL", "5000", "INR"),
            Rule(StoresConfigurationRuleKeys.ExpenseFoodPerPersonPerDay, "DECIMAL", "300", "INR_PERSON_DAY"),
            Rule(StoresConfigurationRuleKeys.ExpenseLodgingSinglePerDay, "DECIMAL", "800", "INR_DAY"),
            Rule(StoresConfigurationRuleKeys.ExpenseLodgingDoublePerDay, "DECIMAL", "1200", "INR_DAY"),
            Rule(StoresConfigurationRuleKeys.ExpenseDailyApprovalCap, "DECIMAL", "5000", "INR_DAY"),
            Rule(StoresConfigurationRuleKeys.ExpenseTravelDistanceThresholdKm, "DECIMAL", "100", "KM")
        };

        var companies = new[]
        {
            (MultiCompanyFoundationSeedData.SessPvtLtdId, "PVT"),
            (MultiCompanyFoundationSeedData.SessProprietorshipId, "PROP")
        };

        return companies.SelectMany(company => rules.Select(rule => new BusinessRuleConfigurationVersion
        {
            Id = StableId("stores-part1-configuration", company.Item1.ToString("N"), rule.Key),
            CompanyId = company.Item1,
            RuleKey = rule.Key,
            ValueType = rule.ValueType,
            OldValueJson = null,
            NewValueJson = rule.JsonValue,
            UnitCode = rule.UnitCode,
            VersionNumber = 1,
            PreviousVersionId = null,
            EffectiveFrom = EffectiveFrom,
            ChangedByEmployeeId = TechnicalDirectorEmployeeId,
            ChangedByRoleCode = StoresConfigurationRoleCodes.TechnicalDirector,
            ChangeReason = SeedReason,
            ChangedAt = EffectiveFrom,
            CorrelationId = $"STORES-P1-{company.Item2}-{rule.Key}"
        })).ToArray();
    }

    private static (string Key, string ValueType, string JsonValue, string UnitCode) Rule(
        string key,
        string valueType,
        string jsonValue,
        string unitCode) => (key, valueType, jsonValue, unitCode);

    private static Guid StableId(params string[] parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', parts)));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
