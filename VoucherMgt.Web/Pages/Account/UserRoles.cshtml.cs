using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Account;

public class UserRolesModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IOrganizationScope _scope;

    public UserRolesModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IOrganizationScope scope)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _scope = scope;
    }

    public List<UserRow> Users { get; private set; } = [];
    public List<string> RoleNames { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAssignAsync(string userId, string role, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organization.Id, cancellationToken);
        if (user is null || !await _roleManager.RoleExistsAsync(role))
        {
            return NotFound();
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        TempData["Status"] = $"{role} assigned to {user.FullName}.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        RoleNames = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync(cancellationToken);
        Users = [];
        foreach (var user in await _userManager.Users.Where(u => u.OrganizationId == organization.Id).OrderBy(u => u.FullName).ToListAsync(cancellationToken))
        {
            Users.Add(new UserRow
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList()
            });
        }
    }

    public sealed class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
    }
}
