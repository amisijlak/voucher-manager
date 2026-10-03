using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Models;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Invoices;

public class EditModel : PageModel
{
    private readonly IProcurementService _procurement;
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public EditModel(IProcurementService procurement, IRepository repository, IOrganizationScope scope)
    {
        _procurement = procurement;
        _repository = repository;
        _scope = scope;
    }

    [BindProperty] public InvoiceInput Input { get; set; } = new();
    public List<OrderForm> Orders { get; private set; } = [];
    public List<TradingCompany> Companies { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int? id, int? orderId, CancellationToken cancellationToken)
    {
        var organization = await LoadLookupsAsync(cancellationToken);
        if (id is int invoiceId)
        {
            var invoice = await _repository.Set<Invoice>().Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.OrganizationId == organization.Id, cancellationToken);
            if (invoice is null) return NotFound();
            if (invoice.Status != InvoiceStatus.Draft) return RedirectToPage("Details", new { id });
            Input = new InvoiceInput
            {
                Id = invoice.Id,
                Direction = invoice.Direction,
                OrderFormId = invoice.OrderFormId,
                TradingCompanyId = invoice.TradingCompanyId,
                ExternalNumber = invoice.ExternalNumber,
                PartyName = invoice.PartyName,
                ContactName = invoice.ContactName,
                Address = invoice.Address,
                InvoiceDate = invoice.InvoiceDate,
                DueOn = invoice.DueOn,
                VatMode = invoice.VatMode,
                VatRate = invoice.VatRate,
                Notes = invoice.Notes,
                Lines = invoice.Lines.OrderBy(l => l.LineNumber).Select(l => new LineInput
                {
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitAmount = l.UnitPrice,
                    Currency = l.Currency
                }).ToList()
            };
            return Page();
        }

        Input.InvoiceDate = DateOnly.FromDateTime(DateTime.Today);
        Input.DueOn = DateOnly.FromDateTime(DateTime.Today.AddDays(14));
        Input.VatRate = organization.DefaultVatRate;
        Input.Lines = [new LineInput()];
        if (orderId is int linkedId)
        {
            var order = Orders.FirstOrDefault(o => o.Id == linkedId);
            if (order is not null)
            {
                Input.OrderFormId = order.Id;
                Input.TradingCompanyId = order.TradingCompanyId;
                Input.PartyName = order.PartyName;
                Input.ContactName = order.ContactName;
                Input.Address = order.Address;
                Input.VatMode = order.VatMode;
                Input.VatRate = order.VatRate;
                Input.Direction = order.Direction == OrderDirection.Purchase ? InvoiceDirection.Payable : InvoiceDirection.Receivable;
                Input.Lines = order.Lines.OrderBy(l => l.LineNumber).Select(l => new LineInput
                {
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitAmount = l.UnitPrice,
                    Currency = l.Currency
                }).ToList();
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _procurement.SaveInvoiceAsync(new InvoiceDraft
        {
            Id = Input.Id,
            Direction = Input.Direction,
            OrderFormId = Input.OrderFormId,
            TradingCompanyId = Input.TradingCompanyId,
            ExternalNumber = Input.ExternalNumber,
            PartyName = Input.PartyName,
            ContactName = Input.ContactName,
            Address = Input.Address,
            InvoiceDate = Input.InvoiceDate,
            DueOn = Input.DueOn,
            VatMode = Input.VatMode,
            VatRate = Input.VatRate,
            Notes = Input.Notes,
            Lines = Input.Lines.Select(l => new MoneyLineDraft
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitAmount = l.UnitAmount,
                Currency = l.Currency
            }).ToList()
        }, cancellationToken);
        PageAlerts.Set(this, result);
        if (!result.Ok || result.Id is null)
        {
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage("Details", new { id = result.Id });
    }

    private async Task<Organization> LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Orders = await _repository.Set<OrderForm>().Include(o => o.Lines)
            .Where(o => o.OrganizationId == organization.Id && o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedOn)
            .ToListAsync(cancellationToken);
        Companies = await _repository.Set<TradingCompany>()
            .Where(c => c.OrganizationId == organization.Id && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return organization;
    }

    public sealed class InvoiceInput
    {
        public int? Id { get; set; }
        public InvoiceDirection Direction { get; set; } = InvoiceDirection.Receivable;
        public int? OrderFormId { get; set; }
        public int? TradingCompanyId { get; set; }
        public string? ExternalNumber { get; set; }
        [Required] public string PartyName { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? Address { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly? DueOn { get; set; }
        public VatMode VatMode { get; set; } = VatMode.Exclusive;
        public decimal VatRate { get; set; } = 18;
        public string? Notes { get; set; }
        public List<LineInput> Lines { get; set; } = [];
    }

    public sealed class LineInput
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitAmount { get; set; }
        public string Currency { get; set; } = Money.Ugx;
    }
}
