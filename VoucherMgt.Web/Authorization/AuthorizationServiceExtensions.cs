using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace VoucherMgt.Web.Authorization;

public static class AuthorizationServiceExtensions
{
    public static async Task<bool> HasPermissionAsync(this IAuthorizationService authorizationService, ClaimsPrincipal user, string permission)
    {
        var result = await authorizationService.AuthorizeAsync(user, null, AppPermissions.ToPolicyName(permission));
        return result.Succeeded;
    }
}
