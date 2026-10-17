using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Application.Purchase;

/// <summary>Substitution is a current identity authority, never a rewrite of the retained workflow snapshot.
/// The calling service and SQL guard must additionally enforce document participation and assignment freshness.</summary>
public static class AccountsBackupDecisionContext
{
    public const string CounterpartSnapshot = "{\"steps\":[{\"stepNumber\":1,\"resolutionType\":\"DEPARTMENT_MAPPING\",\"employeeCode\":\"SESS-14\",\"roleCode\":\"ACCOUNTS_MANAGER\"}]}";
    public static bool CanUse(ICurrentUser? user) => user is {IsAuthenticated:true} && user.EmployeeId is { } id && id!=Guid.Empty &&
        user.EmployeeCode==AccountsBackupIdentityPolicy.EmployeeCode && AccountsBackupIdentityPolicy.IsBackup(user.IdentityAuthorityType) &&
        !string.IsNullOrWhiteSpace(user.OrganizationId) && !string.IsNullOrWhiteSpace(user.IdentityIssuer) && !string.IsNullOrWhiteSpace(user.IdentitySubject) &&
        user.EffectiveRoleAssignments.Any(a=>a.AssignmentId!=Guid.Empty && a.RoleCode==AccountsBackupIdentityPolicy.AccountsRole && a.AssignmentType is "FULL" or "TEMPORARY");
    public static bool CanSubstitute(ICurrentUser? user,PurchaseApprovalWorkflowSnapshot snapshot,
        PurchaseApprovalWorkflowStepSnapshot step,Guid actor) =>
        user is not null && CanUse(user) && actor!=Guid.Empty && user.EmployeeId==actor &&
        user.OrganizationId==snapshot.OrganizationId && step.StepNumber==1 &&
        step.ResolutionType==PurchaseApproverResolutionTypes.DepartmentMapping &&
        step.EmployeeCode=="SESS-14" && step.RoleCode==AccountsBackupIdentityPolicy.AccountsRole;
}
