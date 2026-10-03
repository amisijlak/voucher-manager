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
using VoucherMgt.Web.Documents;

namespace VoucherMgt.Web.Pages.Requisitions;

public class DetailsModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;
    private readonly IPettyCashService _pettyCash;
    private readonly IAuthorizationService _authorization;
    private readonly IWebHostEnvironment _environment;

    public DetailsModel(IRepository repository, IOrganizationScope scope, IPettyCashService pettyCash, IAuthorizationService authorization, IWebHostEnvironment environment)
    {
        _repository = repository;
        _scope = scope;
        _pettyCash = pettyCash;
        _authorization = authorization;
        _environment = environment;
    }

    public Requisition? Item { get; private set; }
    public bool CanSubmit { get; private set; }
    public bool CanDecide { get; private set; }
    public bool CanDisburse { get; private set; }
    public bool CanReview { get; private set; }
    public bool CanClose { get; private set; }

    [BindProperty] public DecisionDraft Decision { get; set; } = new();
    [BindProperty] public DisbursementDraft Disbursement { get; set; } = new() { Method = "Cash", PaidOn = DateOnly.FromDateTime(DateTime.Today) };
    [BindProperty] public string? ReviewComments { get; set; }
    [BindProperty] public bool ReviewApprove { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        Decision.ApprovedUgx = Item!.RequestedUgx;
        Decision.ApprovedUsd = Item.RequestedUsd;
        Disbursement.AmountUgx = Item.ApprovedUgx;
        Disbursement.AmountUsd = Item.ApprovedUsd;
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.RequisitionsSubmit))
        {
            return Forbid();
        }

        var result = await _pettyCash.SubmitRequisitionAsync(id, PageAlerts.Actor(User), cancellationToken);
        PageAlerts.Set(this, result);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDecideAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.ApprovalsDecide))
        {
            return Forbid();
        }

        var result = await _pettyCash.DecideAsync(id, Decision, PageAlerts.Actor(User), cancellationToken);
        PageAlerts.Set(this, result);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDisburseAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.DisbursementsRecord))
        {
            return Forbid();
        }

        var result = await _pettyCash.DisburseAsync(id, Disbursement, PageAlerts.Actor(User), cancellationToken);
        PageAlerts.Set(this, result);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReviewAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.AccountabilityReview))
        {
            return Forbid();
        }

        var result = await _pettyCash.ReviewAccountabilityAsync(id, ReviewApprove, ReviewComments, PageAlerts.Actor(User), cancellationToken);
        PageAlerts.Set(this, result);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCloseAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _pettyCash.CloseRequisitionAsync(id, PageAlerts.Actor(User), cancellationToken);
        PageAlerts.Set(this, result);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnGetReceiptAsync(int lineId, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var line = await _repository.Set<AccountabilityLine>()
            .Include(l => l.Accountability)
            .ThenInclude(a => a.Requisition)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.Accountability.Requisition.OrganizationId == organization.Id, cancellationToken);
        if (line?.ReceiptFilePath is null || !System.IO.File.Exists(line.ReceiptFilePath))
        {
            return NotFound();
        }

        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "receipts", organization.Id.ToString()));
        var full = Path.GetFullPath(line.ReceiptFilePath);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        return PhysicalFile(full, ContentType(full), line.ReceiptFileName ?? Path.GetFileName(full));
    }

    public async Task<IActionResult> OnGetPdfAsync(int id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var name = Item!.Organization?.Name ?? "Voucher Management";
        return File(RequisitionFiles.Pdf(Item, name), "application/pdf", $"{Item.Number}.pdf");
    }

    public async Task<IActionResult> OnGetCsvAsync(int id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        return File(RequisitionFiles.Csv(Item!), "text/csv", $"{Item!.Number}.csv");
    }

    private async Task<bool> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Item = await _repository.Set<Requisition>()
            .Include(r => r.Lines)
            .Include(r => r.Decisions)
            .Include(r => r.Disbursement)
            .Include(r => r.Accountability)!.ThenInclude(a => a!.Lines)
            .Include(r => r.BudgetPeriod)
            .Include(r => r.Organization)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organization.Id, cancellationToken);
        if (Item is null)
        {
            return false;
        }

        CanSubmit = await _authorization.HasPermissionAsync(User, AppPermissions.RequisitionsSubmit);
        CanDecide = await _authorization.HasPermissionAsync(User, AppPermissions.ApprovalsDecide);
        CanDisburse = await _authorization.HasPermissionAsync(User, AppPermissions.DisbursementsRecord);
        CanReview = await _authorization.HasPermissionAsync(User, AppPermissions.AccountabilityReview);
        CanClose = CanReview;
        return true;
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
