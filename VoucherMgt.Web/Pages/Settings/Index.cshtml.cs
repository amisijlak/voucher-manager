using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;
    private readonly IWebHostEnvironment _environment;

    public IndexModel(IRepository repository, IOrganizationScope scope, IWebHostEnvironment environment)
    {
        _repository = repository;
        _scope = scope;
        _environment = environment;
    }

    public List<TradingCompany> Companies { get; private set; } = [];
    [BindProperty] public decimal DefaultVatRate { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnGetLogoAsync(int companyId, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var company = await _repository.Set<TradingCompany>()
            .FirstOrDefaultAsync(c => c.Id == companyId && c.OrganizationId == organization.Id, cancellationToken);
        return company is null ? NotFound() : CompanyLogos.Open(_environment, company, organization.Id);
    }

    public async Task<IActionResult> OnPostVatAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        organization.DefaultVatRate = DefaultVatRate;
        await _repository.SaveChangesAsync(cancellationToken);
        TempData["Status"] = "VAT rate saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompanyAsync(int companyId, string companyName, string? contact, IFormFile? logo, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var company = await _repository.Set<TradingCompany>()
            .FirstOrDefaultAsync(c => c.Id == companyId && c.OrganizationId == organization.Id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(companyName))
        {
            TempData["Error"] = "Enter the company name.";
            return RedirectToPage();
        }

        company.Name = companyName.Trim();
        company.Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();
        if (logo is { Length: > 0 })
        {
            var saved = await CompanyLogos.SaveAsync(CompanyLogos.Root(_environment, organization.Id), logo);
            if (saved.Error is not null)
            {
                TempData["Error"] = saved.Error;
                return RedirectToPage();
            }

            CompanyLogos.Delete(_environment, company, organization.Id);
            company.LogoFilePath = saved.Path;
            company.LogoFileName = saved.Name;
        }

        await _repository.SaveChangesAsync(cancellationToken);
        TempData["Status"] = "Company saved.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        DefaultVatRate = organization.DefaultVatRate;
        Companies = await _repository.Set<TradingCompany>()
            .Where(c => c.OrganizationId == organization.Id)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
