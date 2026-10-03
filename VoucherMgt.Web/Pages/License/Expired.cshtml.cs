using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoucherMgt.Web.Pages.License;

public class ExpiredModel : PageModel
{
    public string OrganizationCode { get; private set; } = string.Empty;

    public void OnGet(string? org)
    {
        OrganizationCode = string.IsNullOrWhiteSpace(org) ? "status-one" : org;
    }
}
