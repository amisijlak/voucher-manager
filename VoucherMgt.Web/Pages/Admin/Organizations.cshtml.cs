using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using VoucherMgt.Web.Authorization;

namespace VoucherMgt.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class OrganizationsModel : PageModel
{
    private readonly IRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuthorizationService _authorization;

    public OrganizationsModel(IRepository repository, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IAuthorizationService authorization)
    {
        _repository = repository;
        _userManager = userManager;
        _roleManager = roleManager;
        _authorization = authorization;
    }

    public IReadOnlyList<Organization> Organizations { get; private set; } = [];
    [BindProperty] public string OrganizationCode { get; set; } = string.Empty;
    [BindProperty] public string OrganizationName { get; set; } = string.Empty;
    [BindProperty] public string OrganizationEmail { get; set; } = string.Empty;
    [BindProperty] public string OrganizationPhone { get; set; } = string.Empty;
    [BindProperty] public int LicenseOrganizationId { get; set; }
    [BindProperty] public DateTime LicenseExpiresOn { get; set; } = DateTime.Today.AddYears(1);
    [BindProperty] public int UserOrganizationId { get; set; }
    [BindProperty] public string UserEmail { get; set; } = string.Empty;
    [BindProperty] public string UserFullName { get; set; } = string.Empty;
    [BindProperty] public string UserPassword { get; set; } = "User@123";
    [BindProperty] public string UserRole { get; set; } = "Requester";

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostCreateOrganizationAsync(CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.OrganizationsCreate)) return Forbid();
        var code = OrganizationCode.Trim().ToLowerInvariant().Replace(' ', '-');
        var organization = new Organization
        {
            Code = code,
            Name = OrganizationName.Trim(),
            Email = OrganizationEmail.Trim(),
            Phone = OrganizationPhone.Trim(),
            Country = "Uganda",
            IsActive = true,
            DefaultVatRate = 18,
            PaymentNote = "All payments should be made to the respective company accounts."
        };
        var key = $"LIC-{code.ToUpperInvariant()}-{Guid.NewGuid():N}";
        organization.Licenses.Add(new OrganizationLicense
        {
            LicenseKey = key[..Math.Min(80, key.Length)],
            StartsOn = DateOnly.FromDateTime(DateTime.Today),
            ExpiresOn = DateOnly.FromDateTime(LicenseExpiresOn),
            Notes = "Initial organization license"
        });
        _repository.Add(organization);
        await _repository.SaveChangesAsync(cancellationToken);
        TempData["Status"] = "Organization created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddLicenseAsync(CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.LicensesManage)) return Forbid();
        var organization = await _repository.Set<Organization>().FirstAsync(o => o.Id == LicenseOrganizationId, cancellationToken);
        var key = $"LIC-{organization.Code.ToUpperInvariant()}-{Guid.NewGuid():N}";
        _repository.Add(new OrganizationLicense
        {
            OrganizationId = organization.Id,
            LicenseKey = key[..Math.Min(80, key.Length)],
            StartsOn = DateOnly.FromDateTime(DateTime.Today),
            ExpiresOn = DateOnly.FromDateTime(LicenseExpiresOn),
            Notes = "Manual renewal"
        });
        await _repository.SaveChangesAsync(cancellationToken);
        TempData["Status"] = "License added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateUserAsync(CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.UsersCreate)) return Forbid();
        var organization = await _repository.Set<Organization>().FirstAsync(o => o.Id == UserOrganizationId, cancellationToken);
        if (!await _roleManager.RoleExistsAsync(UserRole))
        {
            TempData["Error"] = "Choose a known role.";
            return RedirectToPage();
        }

        var user = new ApplicationUser
        {
            UserName = UserEmail.Trim(),
            Email = UserEmail.Trim(),
            EmailConfirmed = true,
            FullName = UserFullName.Trim(),
            OrganizationId = organization.Id
        };
        var result = await _userManager.CreateAsync(user, UserPassword);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        await _userManager.AddToRoleAsync(user, UserRole);
        await _userManager.AddClaimAsync(user, new Claim("OrganizationId", organization.Id.ToString()));
        await _userManager.AddClaimAsync(user, new Claim("OrganizationCode", organization.Code));
        await _userManager.AddClaimAsync(user, new Claim("FullName", user.FullName));
        TempData["Status"] = "User created.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Organizations = await _repository.Set<Organization>().Include(o => o.Licenses).OrderBy(o => o.Name).ToListAsync(cancellationToken);
    }
}
