using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Models;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IPettyCashService _pettyCash;
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public IndexModel(IPettyCashService pettyCash, IRepository repository, IOrganizationScope scope)
    {
        _pettyCash = pettyCash;
        _repository = repository;
        _scope = scope;
    }

    public DashboardSummary Summary { get; private set; } = new();
    public IReadOnlyList<Requisition> RecentRequisitions { get; private set; } = [];
    public Organization Organization { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Organization = await _scope.GetRequiredAsync(cancellationToken);
        Summary = await _pettyCash.GetDashboardAsync(cancellationToken);
        RecentRequisitions = await _repository.Set<Requisition>()
            .Where(r => r.OrganizationId == Organization.Id)
            .OrderByDescending(r => r.CreatedOn)
            .Take(8)
            .ToListAsync(cancellationToken);
    }
}
