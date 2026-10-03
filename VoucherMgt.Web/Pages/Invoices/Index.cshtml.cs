using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Invoices;

public class IndexModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public IndexModel(IRepository repository, IOrganizationScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    public IReadOnlyList<Invoice> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Items = await _repository.Set<Invoice>()
            .Include(i => i.OrderForm)
            .Where(i => i.OrganizationId == organization.Id)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);
    }
}
