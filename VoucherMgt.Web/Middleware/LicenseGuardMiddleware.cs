using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL.Core;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Middleware;

public class LicenseGuardMiddleware
{
    private readonly RequestDelegate _next;

    public LicenseGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentOrganizationContext organizationContext, IRepository repository)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var organization = await repository.Set<Organization>()
            .Include(o => o.Licenses)
            .FirstOrDefaultAsync(o => o.Code == organizationContext.OrganizationCode);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasValidLicense = organization?.IsActive == true && organization.Licenses.Any(l => l.IsValidOn(today));
        if (!hasValidLicense)
        {
            context.Response.Redirect($"/License/Expired?org={Uri.EscapeDataString(organizationContext.OrganizationCode)}");
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkip(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/License/Expired", StringComparison.OrdinalIgnoreCase);
    }
}
