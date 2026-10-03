using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Budgets;

public class DetailsModel : PageModel
{
    private static readonly RequisitionStatus[] CommittedStatuses =
    [
        RequisitionStatus.Approved, RequisitionStatus.Disbursed, RequisitionStatus.AccountabilitySubmitted,
        RequisitionStatus.AccountabilityReturned, RequisitionStatus.Accounted, RequisitionStatus.Closed
    ];

    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public DetailsModel(IRepository repository, IOrganizationScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    public BudgetPeriod? Period { get; private set; }
    public IEnumerable<IGrouping<string, BudgetLine>> Groups { get; private set; } = [];
    public Dictionary<int, decimal> Committed { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Period = await _repository.Set<BudgetPeriod>()
            .Include(p => p.Lines).ThenInclude(l => l.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organization.Id, cancellationToken);
        if (Period is null)
        {
            return NotFound();
        }

        Groups = Period.Lines
            .OrderBy(l => l.Category?.SortOrder ?? 99)
            .ThenBy(l => l.LineNumber)
            .GroupBy(l => l.Category is null ? "Uncategorised" : $"{l.Category.Code}. {l.Category.Name}");

        Committed = await _repository.Set<RequisitionLine>()
            .Where(l => l.BudgetLine!.BudgetPeriodId == id && CommittedStatuses.Contains(l.Requisition.Status))
            .GroupBy(l => l.BudgetLineId!.Value)
            .Select(g => new { Id = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Id, x => x.Amount, cancellationToken);
        return Page();
    }
}
