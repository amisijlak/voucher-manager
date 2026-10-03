using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Models;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using VoucherMgt.Web.Authorization;

namespace VoucherMgt.Web.Pages.Requisitions;

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

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty] public RequisitionInput Input { get; set; } = new();
    public IReadOnlyList<Requisition> Items { get; private set; } = [];
    public List<BudgetPeriod> Periods { get; private set; } = [];
    public List<BudgetLine> BudgetLines { get; private set; } = [];
    public bool CanCreate { get; private set; }
    public bool ShowDialog { get; private set; }
    public string NextNumber { get; private set; } = string.Empty;

    public async Task OnGetAsync(string? open, CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        if (string.Equals(open, "new", StringComparison.OrdinalIgnoreCase) && CanCreate)
        {
            ShowDialog = true;
            PrepareNewInput();
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.RequisitionsCreate))
        {
            return Forbid();
        }

        var result = await _pettyCash.SaveRequisitionAsync(ToDraft(), PageAlerts.Actor(User), cancellationToken);
        if (!result.Ok || result.Id is null)
        {
            TempData["Error"] = result.Message;
            ShowDialog = true;
            await LoadAsync(cancellationToken);
            if (Input.Lines.Count == 0)
            {
                Input.Lines.Add(new LineInput());
            }

            return Page();
        }

        var number = await _repository.Set<Requisition>()
            .Where(r => r.Id == result.Id)
            .Select(r => r.Number)
            .FirstAsync(cancellationToken);
        TempData["Status"] = $"{number} was saved.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        CanCreate = await _authorization.HasPermissionAsync(User, AppPermissions.RequisitionsCreate);
        var query = _repository.Set<Requisition>().Where(r => r.OrganizationId == organization.Id);
        if (!string.IsNullOrWhiteSpace(Status) && Enum.TryParse<RequisitionStatus>(Status, out var parsed))
        {
            query = query.Where(r => r.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(r => r.Number.Contains(term) || r.Title.Contains(term) || r.RequestedByName.Contains(term));
        }

        Items = await query.OrderByDescending(r => r.CreatedOn).ToListAsync(cancellationToken);
        Periods = await _repository.Set<BudgetPeriod>()
            .Where(p => p.OrganizationId == organization.Id)
            .OrderByDescending(p => p.StartsOn)
            .ToListAsync(cancellationToken);
        BudgetLines = await _repository.Set<BudgetLine>()
            .Include(l => l.BudgetPeriod)
            .Where(l => l.BudgetPeriod.OrganizationId == organization.Id)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken);

        var head = $"REQ-{DateTime.Today.Year}-";
        var numbers = await _repository.Set<Requisition>()
            .Where(r => r.OrganizationId == organization.Id && r.Number.StartsWith(head))
            .Select(r => r.Number)
            .ToListAsync(cancellationToken);
        NextNumber = $"{head}{PettyCashService.NextSequence(numbers, head):0000}";
        if (Input.Lines.Count == 0)
        {
            PrepareNewInput();
        }
    }

    private void PrepareNewInput()
    {
        Input.Id = null;
        Input.PeriodStart = DateOnly.FromDateTime(DateTime.Today);
        Input.PeriodEnd = DateOnly.FromDateTime(DateTime.Today);
        if (Input.Lines.Count == 0)
        {
            Input.Lines = [new LineInput()];
        }
    }

    private RequisitionDraft ToDraft() => new()
    {
        Id = null,
        Title = Input.Title,
        PeriodStart = Input.PeriodStart,
        PeriodEnd = Input.PeriodEnd,
        Department = Input.Department,
        Purpose = Input.Purpose,
        BudgetPeriodId = Input.BudgetPeriodId,
        Lines = Input.Lines.Select(l => new MoneyLineDraft
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitAmount = l.UnitAmount,
            Currency = l.Currency,
            BudgetLineId = l.BudgetLineId
        }).ToList()
    };

    public sealed class RequisitionInput
    {
        public int? Id { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        [MaxLength(80)] public string? Department { get; set; }
        [MaxLength(500)] public string? Purpose { get; set; }
        public int? BudgetPeriodId { get; set; }
        public List<LineInput> Lines { get; set; } = [];
    }

    public sealed class LineInput
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitAmount { get; set; }
        public string Currency { get; set; } = Money.Ugx;
        public int? BudgetLineId { get; set; }
    }
}
