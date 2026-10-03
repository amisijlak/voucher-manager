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

    public IndexModel(IRepository repository, IOrganizationScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    public List<TradingCompany> Companies { get; private set; } = [];
    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string Phone { get; set; } = string.Empty;
    [BindProperty] public string Address { get; set; } = string.Empty;
    [BindProperty] public string City { get; set; } = string.Empty;
    [BindProperty] public string Country { get; set; } = string.Empty;
    [BindProperty] public decimal DefaultVatRate { get; set; }
    [BindProperty] public string PaymentNote { get; set; } = string.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostOrganizationAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        organization.Name = Name.Trim();
        organization.Phone = Phone.Trim();
        organization.Address = Address.Trim();
        organization.City = City.Trim();
        organization.Country = Country.Trim();
        organization.DefaultVatRate = DefaultVatRate;
        organization.PaymentNote = PaymentNote.Trim();
        await _repository.SaveChangesAsync(cancellationToken);
        TempData["Status"] = "Organization settings saved.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompanyAsync(int companyId, string companyName, string shortName, string? accountNumber, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var company = await _repository.Set<TradingCompany>()
            .FirstOrDefaultAsync(c => c.Id == companyId && c.OrganizationId == organization.Id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        company.Name = companyName.Trim();
        company.ShortName = shortName.Trim();
        company.AccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber.Trim();
        await _repository.SaveChangesAsync(cancellationToken);
        TempData["Status"] = "Company saved.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Name = organization.Name;
        Phone = organization.Phone;
        Address = organization.Address;
        City = organization.City;
        Country = organization.Country;
        DefaultVatRate = organization.DefaultVatRate;
        PaymentNote = organization.PaymentNote;
        Companies = await _repository.Set<TradingCompany>()
            .Where(c => c.OrganizationId == organization.Id)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
