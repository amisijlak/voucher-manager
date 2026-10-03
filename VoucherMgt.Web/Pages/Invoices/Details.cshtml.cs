using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Models;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using VoucherMgt.Web.Authorization;

namespace VoucherMgt.Web.Pages.Invoices;

public class DetailsModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;
    private readonly IProcurementService _procurement;
    private readonly IAuthorizationService _authorization;

    public DetailsModel(IRepository repository, IOrganizationScope scope, IProcurementService procurement, IAuthorizationService authorization)
    {
        _repository = repository;
        _scope = scope;
        _procurement = procurement;
        _authorization = authorization;
    }

    public Invoice? Item { get; private set; }
    public bool CanCreate { get; private set; }
    public bool CanApprove { get; private set; }
    public bool CanPay { get; private set; }
    [BindProperty] public PaymentDraft Payment { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken) =>
        await LoadAsync(id, cancellationToken) ? Page() : NotFound();

    public async Task<IActionResult> OnPostIssueAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesCreate)) return Forbid();
        PageAlerts.Set(this, await _procurement.IssueInvoiceAsync(id, cancellationToken));
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesApprove)) return Forbid();
        PageAlerts.Set(this, await _procurement.ApproveInvoiceAsync(id, cancellationToken));
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPayAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesPay)) return Forbid();
        PageAlerts.Set(this, await _procurement.PayInvoiceAsync(id, Payment, cancellationToken));
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesApprove)) return Forbid();
        PageAlerts.Set(this, await _procurement.CancelInvoiceAsync(id, cancellationToken));
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Item = await _repository.Set<Invoice>()
            .Include(i => i.Lines)
            .Include(i => i.OrderForm)
            .Include(i => i.TradingCompany)
            .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organization.Id, cancellationToken);
        if (Item is null) return false;
        CanCreate = await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesCreate);
        CanApprove = await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesApprove);
        CanPay = await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesPay);
        Payment.AmountUgx = Item.GrossUgx;
        Payment.AmountUsd = Item.GrossUsd;
        return true;
    }
}
