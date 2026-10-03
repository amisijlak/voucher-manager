using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Accountability;

public class IndexModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public IndexModel(IRepository repository, IOrganizationScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    public IReadOnlyList<Requisition> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Items = await _repository.Set<Requisition>()
            .Include(r => r.Disbursement)
            .Where(r => r.OrganizationId == organization.Id &&
                (r.Status == RequisitionStatus.Disbursed
                 || r.Status == RequisitionStatus.AccountabilitySubmitted
                 || r.Status == RequisitionStatus.AccountabilityReturned))
            .OrderBy(r => r.Number)
            .ToListAsync(cancellationToken);
    }
}
