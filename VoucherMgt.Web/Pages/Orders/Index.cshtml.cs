using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public IndexModel(IRepository repository, IOrganizationScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    public IReadOnlyList<OrderForm> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var query = _repository.Set<OrderForm>().Include(o => o.TradingCompany).Where(o => o.OrganizationId == organization.Id);
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(o => o.Number.Contains(term) || o.PartyName.Contains(term));
        }

        Items = await query.OrderByDescending(o => o.CreatedOn).ToListAsync(cancellationToken);
    }
}
