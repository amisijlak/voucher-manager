using System.Security.Claims;
using VoucherMgt.BLL.Models;

namespace VoucherMgt.Web;

public static class PageAlerts
{
    public static Actor Actor(ClaimsPrincipal user) => new()
    {
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        Name = user.FindFirstValue("FullName") ?? user.Identity?.Name ?? "User"
    };

    public static void Set(Microsoft.AspNetCore.Mvc.RazorPages.PageModel page, WorkflowResult result)
    {
        page.TempData[result.Ok ? "Status" : "Error"] = result.Message;
    }
}
