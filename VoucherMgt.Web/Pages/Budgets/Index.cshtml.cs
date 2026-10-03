using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Models;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using VoucherMgt.Web.Authorization;

namespace VoucherMgt.Web.Pages.Budgets;

public class IndexModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;
    private readonly IPettyCashService _pettyCash;
    private readonly IAuthorizationService _authorization;

    public IndexModel(IRepository repository, IOrganizationScope scope, IPettyCashService pettyCash, IAuthorizationService authorization)
    {
        _repository = repository;
        _scope = scope;
        _pettyCash = pettyCash;
        _authorization = authorization;
    }

    public List<BudgetPeriod> Periods { get; private set; } = [];
    public List<BudgetCategory> Categories { get; private set; } = [];
    public bool CanManage { get; private set; }
    [BindProperty] public BudgetPeriodDraft Draft { get; set; } = new()
    {
        StartsOn = DateOnly.FromDateTime(DateTime.Today),
        EndsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
        Lines = [new BudgetLineDraft()]
    };

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.BudgetsManage))
        {
            return Forbid();
        }

        var result = await _pettyCash.SaveBudgetPeriodAsync(Draft, cancellationToken);
        PageAlerts.Set(this, result);
        if (!result.Ok)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage("Details", new { id = result.Id });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Periods = await _repository.Set<BudgetPeriod>().Include(p => p.Lines)
            .Where(p => p.OrganizationId == organization.Id)
            .OrderByDescending(p => p.StartsOn)
            .ToListAsync(cancellationToken);
        Categories = await _repository.Set<BudgetCategory>()
            .Where(c => c.OrganizationId == organization.Id)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
        CanManage = await _authorization.HasPermissionAsync(User, AppPermissions.BudgetsManage);
    }
}
