namespace SESS.NexaERP.Domain.Identity;

/// <summary>A second typed HUMAN mapping for the same canonical employee, never a SERVICE alias.
/// Existing HUMAN uniqueness and issuer/subject ownership remain intact.</summary>
public static class AccountsBackupIdentityPolicy
{
    public const string MappingType = "HUMAN_ACCT_BACKUP";
    public const string EmployeeCode = "SESS-41";
    public const string AccountsRole = "ACCOUNTS_MANAGER";
    public static bool IsBackup(string type) => string.Equals(type,MappingType,StringComparison.Ordinal);
    public static bool IsValidMapping(string employeeCode,string type,bool trustedApproversIssuer) =>
        !IsBackup(type) || string.Equals(employeeCode,EmployeeCode,StringComparison.Ordinal) && trustedApproversIssuer;
    public static bool AllowsPageAction(string page,string action)
    {
        // Read permission still requires the existing accounts grant and record scope.
        if(action is "view" or "print" or "download" or "export" or "view-commercial-values" or "view-audit-history") return true;
        return (page,action) switch
        {
            ("purchase.requisitions","verify" or "approve" or "reject" or "request-revision" or "request-clarification") => true,
            ("purchase.requisition-approvals","approve" or "reject" or "request-revision") => true,
            ("purchase.commercial-comparisons","approve" or "reject" or "request-revision" or "request-clarification") => true,
            ("purchase.po","approve" or "reject" or "request-revision" or "request-clarification") => true,
            ("masters.vendors","verify" or "reject") => true,
            ("settings.tax-gst","create") => true,
            ("stores.opening-stock","verify") => true,
            ("stores.stock-adjustments","approve" or "reject") => true,
            _ => false
        };
    }
    public static bool AllowsPurchaseDecisionOperation(string operation) => operation is
        "ApproveComparison" or "RejectComparison" or "RequestRevisionComparison" or "RequestClarificationComparison" or
        "ApprovePO" or "RejectPO" or "RequestRevisionPO" or "RequestClarificationPO";
    public static bool AllowsAssignment(string employeeCode,string type,string role,string assignmentType) =>
        AllowsRole(employeeCode,type,role) && (!IsBackup(type) || assignmentType is "FULL" or "TEMPORARY");
    public static bool AllowsRole(string employeeCode,string type,string role)
    {
        if(IsBackup(type)) return string.Equals(employeeCode,EmployeeCode,StringComparison.Ordinal)
            && string.Equals(role,AccountsRole,StringComparison.Ordinal);
        // Existing employees and existing service mappings retain their established role set.
        // The newly approved SESS-41 accounts-manager authority is usable only through his OTP identity.
        return !string.Equals(employeeCode,EmployeeCode,StringComparison.Ordinal)
            || !string.Equals(role,AccountsRole,StringComparison.Ordinal);
    }
}
