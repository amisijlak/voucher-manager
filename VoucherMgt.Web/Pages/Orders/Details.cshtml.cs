using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using VoucherMgt.Web.Authorization;

namespace VoucherMgt.Web.Pages.Orders;

public class DetailsModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;
    private readonly IProcurementService _procurement;
    private readonly IAuthorizationService _authorization;
    private readonly IWebHostEnvironment _environment;

    public DetailsModel(
        IRepository repository,
        IOrganizationScope scope,
        IProcurementService procurement,
        IAuthorizationService authorization,
        IWebHostEnvironment environment)
    {
        _repository = repository;
        _scope = scope;
        _procurement = procurement;
        _authorization = authorization;
        _environment = environment;
    }

    public OrderForm? Item { get; private set; }
    public List<TradingCompany> Companies { get; private set; } = [];
    public bool CanIssue { get; private set; }
    public bool CanCreateInvoice { get; private set; }
    public bool CanApproveInvoice { get; private set; }
    public bool CanPayInvoice { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        return await LoadAsync(id, cancellationToken) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnGetLogoAsync(int companyId, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var company = await _repository.Set<TradingCompany>()
            .FirstOrDefaultAsync(c => c.Id == companyId && c.OrganizationId == organization.Id && c.IsActive, cancellationToken);
        return company is null ? NotFound() : CompanyLogos.Open(_environment, company, organization.Id);
    }

    public async Task<IActionResult> OnPostIssueAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.OrdersIssue)) return Forbid();
        PageAlerts.Set(this, await _procurement.IssueOrderAsync(id, cancellationToken));
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.OrdersIssue)) return Forbid();
        PageAlerts.Set(this, await _procurement.CancelOrderAsync(id, cancellationToken));
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostIssueInvoiceAsync(int id, int invoiceId, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesCreate)) return Forbid();
        if (!await OwnsInvoiceAsync(id, invoiceId, cancellationToken)) return NotFound();
        PageAlerts.Set(this, await _procurement.IssueInvoiceAsync(invoiceId, cancellationToken));
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveInvoiceAsync(int id, int invoiceId, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesApprove)) return Forbid();
        if (!await OwnsInvoiceAsync(id, invoiceId, cancellationToken)) return NotFound();
        PageAlerts.Set(this, await _procurement.ApproveInvoiceAsync(invoiceId, cancellationToken));
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Item = await _repository.Set<OrderForm>()
            .Include(o => o.TradingCompany)
            .Include(o => o.Lines)
            .Include(o => o.Invoices)
            .FirstOrDefaultAsync(o => o.Id == id && o.OrganizationId == organization.Id, cancellationToken);
        Companies = await _repository.Set<TradingCompany>()
            .Where(c => c.OrganizationId == organization.Id && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        CanIssue = await _authorization.HasPermissionAsync(User, AppPermissions.OrdersIssue);
        CanCreateInvoice = await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesCreate);
        CanApproveInvoice = await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesApprove);
        CanPayInvoice = await _authorization.HasPermissionAsync(User, AppPermissions.InvoicesPay);
        return Item is not null;
    }

    private async Task<bool> OwnsInvoiceAsync(int orderId, int invoiceId, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        return await _repository.Set<Invoice>().AnyAsync(
            i => i.Id == invoiceId && i.OrderFormId == orderId && i.OrganizationId == organization.Id,
            cancellationToken);
    }
}
