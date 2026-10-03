using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public PermissionAuthorizationHandler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        if (context.User.HasClaim(AppPermissions.ClaimType, requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        var user = await _userManager.GetUserAsync(context.User);
        if (user is null)
        {
            return;
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            if (claims.Any(c => c.Type == AppPermissions.ClaimType && c.Value == requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
