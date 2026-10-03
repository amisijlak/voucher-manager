using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Models;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Requisitions;

public class EditModel : PageModel
{
    private readonly IPettyCashService _pettyCash;
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public EditModel(IPettyCashService pettyCash, IRepository repository, IOrganizationScope scope)
    {
        _pettyCash = pettyCash;
        _repository = repository;
        _scope = scope;
    }

    [BindProperty] public RequisitionInput Input { get; set; } = new();
    public string TrackingNumber { get; private set; } = string.Empty;
    public List<BudgetPeriod> Periods { get; private set; } = [];
    public List<BudgetLine> BudgetLines { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return RedirectToPage("Index", new { open = "new" });
        }

        await LoadLookupsAsync(cancellationToken);

        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisition = await _repository.Set<Requisition>()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organization.Id, cancellationToken);
        if (requisition is null)
        {
            return NotFound();
        }

        if (requisition.Status is not (RequisitionStatus.Draft or RequisitionStatus.Returned))
        {
            return RedirectToPage("Details", new { id });
        }

        TrackingNumber = requisition.Number;
        Input = new RequisitionInput
        {
            Id = requisition.Id,
            Title = requisition.Title,
            PeriodStart = requisition.PeriodStart,
            PeriodEnd = requisition.PeriodEnd,
            Department = requisition.Department,
            Purpose = requisition.Purpose,
            BudgetPeriodId = requisition.BudgetPeriodId,
            Lines = requisition.Lines.OrderBy(l => l.LineNumber).Select(l => new LineInput
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitAmount = l.UnitAmount,
                Currency = l.Currency,
                BudgetLineId = l.BudgetLineId
            }).ToList()
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _pettyCash.SaveRequisitionAsync(ToDraft(), PageAlerts.Actor(User), cancellationToken);
        PageAlerts.Set(this, result);
        if (!result.Ok || result.Id is null)
        {
            await LoadLookupsAsync(cancellationToken);
            if (Input.Lines.Count == 0)
            {
                Input.Lines.Add(new LineInput());
            }

            return Page();
        }

        return RedirectToPage("Details", new { id = result.Id });
    }

    private RequisitionDraft ToDraft() => new()
    {
        Id = Input.Id,
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

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Periods = await _repository.Set<BudgetPeriod>()
            .Where(p => p.OrganizationId == organization.Id)
            .OrderByDescending(p => p.StartsOn)
            .ToListAsync(cancellationToken);
        BudgetLines = await _repository.Set<BudgetLine>()
            .Include(l => l.BudgetPeriod)
            .Where(l => l.BudgetPeriod.OrganizationId == organization.Id)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken);
    }

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
