using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VoucherMgt.Web.Authorization;

namespace VoucherMgt.Web.Pages.Account;

public class RolesModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuthorizationService _authorization;

    public RolesModel(RoleManager<IdentityRole> roleManager, IAuthorizationService authorization)
    {
        _roleManager = roleManager;
        _authorization = authorization;
    }

    public List<RoleRow> Roles { get; private set; } = [];
    public IReadOnlyList<IGrouping<string, PermissionDefinition>> PermissionGroups { get; private set; } = [];
    [BindProperty] public RoleInput Input { get; set; } = new();

    public async Task OnGetAsync(string? id) => await LoadAsync(id);

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var permission = string.IsNullOrWhiteSpace(Input.Id) ? AppPermissions.RolesCreate : AppPermissions.RolesEdit;
        if (!await _authorization.HasPermissionAsync(User, permission)) return Forbid();
        Input.SelectedPermissions = Input.SelectedPermissions.Where(AppPermissions.IsValid).Distinct().ToList();
        IdentityRole role;
        if (string.IsNullOrWhiteSpace(Input.Id))
        {
            role = new IdentityRole(Input.Name.Trim());
            var created = await _roleManager.CreateAsync(role);
            if (!created.Succeeded)
            {
                TempData["Error"] = string.Join("; ", created.Errors.Select(e => e.Description));
                return RedirectToPage();
            }
        }
        else
        {
            role = await _roleManager.FindByIdAsync(Input.Id) ?? throw new InvalidOperationException("Role was not found.");
            role.Name = Input.Name.Trim();
            await _roleManager.UpdateAsync(role);
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in claims.Where(c => c.Type == AppPermissions.ClaimType).ToList())
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var selected in Input.SelectedPermissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim(AppPermissions.ClaimType, selected));
        }

        TempData["Status"] = "Role saved.";
        return RedirectToPage(new { id = role.Id });
    }

    private async Task LoadAsync(string? selectedRoleId)
    {
        PermissionGroups = AppPermissions.All.GroupBy(p => p.Group).ToList();
        Roles = [];
        foreach (var role in _roleManager.Roles.OrderBy(r => r.Name))
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            Roles.Add(new RoleRow
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                PermissionCount = claims.Count(c => c.Type == AppPermissions.ClaimType),
                IsSelected = role.Id == selectedRoleId
            });
        }

        if (!string.IsNullOrWhiteSpace(selectedRoleId))
        {
            var selected = await _roleManager.FindByIdAsync(selectedRoleId);
            if (selected is not null)
            {
                var claims = await _roleManager.GetClaimsAsync(selected);
                Input = new RoleInput
                {
                    Id = selected.Id,
                    Name = selected.Name ?? string.Empty,
                    SelectedPermissions = claims.Where(c => c.Type == AppPermissions.ClaimType).Select(c => c.Value).ToList()
                };
            }
        }
    }

    public sealed class RoleInput
    {
        public string? Id { get; set; }
        [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
        public List<string> SelectedPermissions { get; set; } = [];
    }

    public sealed class RoleRow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int PermissionCount { get; set; }
        public bool IsSelected { get; set; }
    }
}
