using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Orders;

public class PrintModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;
    private readonly IWebHostEnvironment _environment;

    public PrintModel(IRepository repository, IOrganizationScope scope, IWebHostEnvironment environment)
    {
        _repository = repository;
        _scope = scope;
        _environment = environment;
    }

    public OrderForm? Item { get; private set; }
    public List<TradingCompany> Companies { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Item = await _repository.Set<OrderForm>()
            .Include(o => o.Lines)
            .Include(o => o.TradingCompany)
            .FirstOrDefaultAsync(o => o.Id == id && o.OrganizationId == organization.Id, cancellationToken);
        if (Item is null)
        {
            return NotFound();
        }

        Companies = await _repository.Set<TradingCompany>()
            .Where(c => c.OrganizationId == organization.Id && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnGetLogoAsync(int companyId, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var company = await _repository.Set<TradingCompany>()
            .FirstOrDefaultAsync(c => c.Id == companyId && c.OrganizationId == organization.Id && c.IsActive, cancellationToken);
        return company is null ? NotFound() : CompanyLogos.Open(_environment, company, organization.Id);
    }
}
