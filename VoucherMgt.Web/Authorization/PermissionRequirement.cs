using Microsoft.AspNetCore.Authorization;

namespace VoucherMgt.Web.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;

    public string Permission { get; }
}

public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(string permission)
    {
        Policy = AppPermissions.ToPolicyName(permission);
    }
}
