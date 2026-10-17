using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Application.Common;

public static class AccountsBackupActionBoundary
{
    public static void RequireAccountsBackupPageAction(this ICurrentUser user,string page,string action)
    {
        if(AccountsBackupIdentityPolicy.IsBackup(user.IdentityAuthorityType) && !AccountsBackupIdentityPolicy.AllowsPageAction(page,action))
            throw new UnauthorizedAccessException("This accounts identity cannot perform stores, requester or purchase-maker actions. Use the separately authorized staff identity.");
    }
}
