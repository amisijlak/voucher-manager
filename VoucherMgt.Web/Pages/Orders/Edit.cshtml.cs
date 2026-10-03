using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Models;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Orders;

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

    [BindProperty] public OrderInput Input { get; set; } = new();
    public List<TradingCompany> Companies { get; private set; } = [];
    public List<Requisition> Requisitions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        var organization = await LoadLookupsAsync(cancellationToken);
        if (id is null)
        {
            Input.VatRate = organization.DefaultVatRate;
            Input.PaymentNote = organization.PaymentNote;
            Input.Country = organization.Country;
            Input.Lines = [new LineInput()];
            return Page();
        }

        var order = await _repository.Set<OrderForm>().Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.OrganizationId == organization.Id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Draft)
        {
            return RedirectToPage("Details", new { id });
        }

        Input = Map(order);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await _procurement.SaveOrderAsync(ToDraft(), cancellationToken);
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

    private async Task<Organization> LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Companies = await _repository.Set<TradingCompany>()
            .Where(c => c.OrganizationId == organization.Id && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        Requisitions = await _repository.Set<Requisition>()
            .Where(r => r.OrganizationId == organization.Id && r.Status != RequisitionStatus.Rejected)
            .OrderByDescending(r => r.CreatedOn)
            .Take(40)
            .ToListAsync(cancellationToken);
        return organization;
    }

    private OrderDraft ToDraft() => new()
    {
        Id = Input.Id,
        Kind = Input.Kind,
        Direction = Input.Direction,
        TradingCompanyId = Input.TradingCompanyId,
        RequisitionId = Input.RequisitionId,
        PartyName = Input.PartyName,
        ContactName = Input.ContactName,
        InvoiceName = Input.InvoiceName,
        Address = Input.Address,
        CityTown = Input.CityTown,
        Country = Input.Country,
        Phone = Input.Phone,
        Fax = Input.Fax,
        Email = Input.Email,
        Channel = Input.Channel,
        VatMode = Input.VatMode,
        VatRate = Input.VatRate,
        PaymentNote = Input.PaymentNote,
        ClientSignatoryName = Input.ClientSignatoryName,
        ClientDesignation = Input.ClientDesignation,
        ClientSignedOn = Input.ClientSignedOn,
        BusinessManager = Input.BusinessManager,
        BusinessSupervisor = Input.BusinessSupervisor,
        AccountNumber = Input.AccountNumber,
        CreditControl = Input.CreditControl,
        Lines = Input.Lines.Select(l => new MoneyLineDraft
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitAmount = l.UnitAmount,
            Currency = l.Currency,
            Programme = l.Programme,
            LengthSeconds = l.LengthSeconds,
            Insertions = l.Insertions
        }).ToList()
    };

    private static OrderInput Map(OrderForm order) => new()
    {
        Id = order.Id,
        Kind = order.Kind,
        Direction = order.Direction,
        TradingCompanyId = order.TradingCompanyId,
        RequisitionId = order.RequisitionId,
        PartyName = order.PartyName,
        ContactName = order.ContactName,
        InvoiceName = order.InvoiceName,
        Address = order.Address,
        CityTown = order.CityTown,
        Country = order.Country,
        Phone = order.Phone,
        Fax = order.Fax,
        Email = order.Email,
        Channel = order.Channel,
        VatMode = order.VatMode,
        VatRate = order.VatRate,
        PaymentNote = order.PaymentNote,
        ClientSignatoryName = order.ClientSignatoryName,
        ClientDesignation = order.ClientDesignation,
        ClientSignedOn = order.ClientSignedOn,
        BusinessManager = order.BusinessManager,
        BusinessSupervisor = order.BusinessSupervisor,
        AccountNumber = order.AccountNumber,
        CreditControl = order.CreditControl,
        Lines = order.Lines.OrderBy(l => l.LineNumber).Select(l => new LineInput
        {
            Description = l.Description,
            Quantity = l.Quantity,
            UnitAmount = l.UnitPrice,
            Currency = l.Currency,
            Programme = l.Programme,
            LengthSeconds = l.LengthSeconds,
            Insertions = l.Insertions
        }).ToList()
    };

    public sealed class OrderInput
    {
        public int? Id { get; set; }
        public OrderKind Kind { get; set; } = OrderKind.General;
        public OrderDirection Direction { get; set; } = OrderDirection.ClientOrder;
        public int TradingCompanyId { get; set; }
        public int? RequisitionId { get; set; }
        [Required, MaxLength(200)] public string PartyName { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? InvoiceName { get; set; }
        public string? Address { get; set; }
        public string? CityTown { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Channel { get; set; }
        public VatMode VatMode { get; set; } = VatMode.Exclusive;
        public decimal VatRate { get; set; } = 18;
        public string? PaymentNote { get; set; }
        public string? ClientSignatoryName { get; set; }
        public string? ClientDesignation { get; set; }
        public DateOnly? ClientSignedOn { get; set; }
        public string? BusinessManager { get; set; }
        public string? BusinessSupervisor { get; set; }
        public string? AccountNumber { get; set; }
        public string? CreditControl { get; set; }
        public List<LineInput> Lines { get; set; } = [];
    }

    public sealed class LineInput
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public decimal UnitAmount { get; set; }
        public string Currency { get; set; } = Money.Ugx;
        public string? Programme { get; set; }
        public int? LengthSeconds { get; set; }
        public int? Insertions { get; set; }
    }
}
