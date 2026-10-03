using System.Security.Claims;
using VoucherMgt.BLL.Core;

namespace VoucherMgt.Web.Middleware;

public class CurrentOrganizationContext : ICurrentOrganizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public CurrentOrganizationContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public string OrganizationCode
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            var userOrganization = context?.User.FindFirstValue("OrganizationCode");
            var queryCode = context?.Request.Query["org"].FirstOrDefault();
            var headerCode = context?.Request.Headers["X-Organization-Code"].FirstOrDefault();
            var configuredCode = _configuration["DefaultOrganization:Code"];

            return Normalize(userOrganization)
                ?? Normalize(queryCode)
                ?? Normalize(headerCode)
                ?? Normalize(configuredCode)
                ?? "status-one";
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
