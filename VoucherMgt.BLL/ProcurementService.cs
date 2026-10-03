using VoucherMgt.BLL.Models;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace VoucherMgt.BLL;

public interface IProcurementService
{
    Task<WorkflowResult> SaveOrderAsync(OrderDraft draft, CancellationToken cancellationToken = default);
    Task<WorkflowResult> IssueOrderAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkflowResult> CancelOrderAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkflowResult> SaveInvoiceAsync(InvoiceDraft draft, CancellationToken cancellationToken = default);
    Task<WorkflowResult> IssueInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkflowResult> ApproveInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkflowResult> PayInvoiceAsync(int id, PaymentDraft draft, CancellationToken cancellationToken = default);
    Task<WorkflowResult> CancelInvoiceAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ProcurementService : IProcurementService
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public ProcurementService(IRepository repository, IOrganizationScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    public async Task<WorkflowResult> SaveOrderAsync(OrderDraft draft, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(draft.PartyName))
        {
            return WorkflowResult.Fail(draft.Direction == OrderDirection.Purchase
                ? "Enter the supplier name."
                : "Enter the client name.");
        }

        var company = await _repository.Set<TradingCompany>()
            .FirstOrDefaultAsync(c => c.Id == draft.TradingCompanyId && c.OrganizationId == organization.Id && c.IsActive, cancellationToken);
        if (company is null)
        {
            return WorkflowResult.Fail("Select the company this order belongs to.");
        }

        var lines = BuildOrderLines(draft.Lines, draft.Kind);
        if (lines.Count == 0)
        {
            return WorkflowResult.Fail("Add at least one order line.");
        }

        if (draft.RequisitionId is int requisitionId)
        {
            var exists = await _repository.Set<Requisition>()
                .AnyAsync(r => r.Id == requisitionId && r.OrganizationId == organization.Id, cancellationToken);
            if (!exists)
            {
                return WorkflowResult.Fail("The linked requisition was not found.");
            }
        }

        OrderForm order;
        if (draft.Id is int id)
        {
            order = await _repository.Set<OrderForm>()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id && o.OrganizationId == organization.Id, cancellationToken)
                ?? throw new InvalidOperationException("Order was not found.");
            if (order.Status != OrderStatus.Draft)
            {
                return WorkflowResult.Fail("Only a draft order can be edited. Cancel it and raise a new one if the issued order must change.");
            }

            foreach (var existing in order.Lines.ToList())
            {
                _repository.Remove(existing);
            }

            order.Lines.Clear();
        }
        else
        {
            order = new OrderForm
            {
                OrganizationId = organization.Id,
                Number = await NextNumberAsync(organization.Id, "LPO", cancellationToken),
                Status = OrderStatus.Draft
            };
            _repository.Add(order);
        }

        order.Kind = draft.Kind;
        order.Direction = draft.Direction;
        order.TradingCompanyId = company.Id;
        order.RequisitionId = draft.RequisitionId;
        order.PartyName = draft.PartyName.Trim();
        order.ContactName = Clean(draft.ContactName);
        order.InvoiceName = Clean(draft.InvoiceName);
        order.Address = Clean(draft.Address);
        order.CityTown = Clean(draft.CityTown);
        order.Country = Clean(draft.Country);
        order.Phone = Clean(draft.Phone);
        order.Fax = Clean(draft.Fax);
        order.Email = Clean(draft.Email);
        order.Channel = Clean(draft.Channel);
        order.VatMode = draft.VatMode;
        order.VatRate = draft.VatMode == VatMode.Exempt ? 0 : draft.VatRate;
        order.PaymentNote = Clean(draft.PaymentNote) ?? organization.PaymentNote;
        order.ClientSignatoryName = Clean(draft.ClientSignatoryName);
        order.ClientDesignation = Clean(draft.ClientDesignation);
        order.ClientSignedOn = draft.ClientSignedOn;
        order.BusinessManager = Clean(draft.BusinessManager);
        order.BusinessSupervisor = Clean(draft.BusinessSupervisor);
        order.AccountNumber = Clean(draft.AccountNumber) ?? company.AccountNumber;
        order.CreditControl = Clean(draft.CreditControl);
        foreach (var line in lines)
        {
            order.Lines.Add(line);
        }

        ApplyTotals(order);
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success("Order form saved.", order.Id);
    }

    public async Task<WorkflowResult> IssueOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var order = await _repository.Set<OrderForm>()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.OrganizationId == organization.Id, cancellationToken);
        if (order is null || order.Status != OrderStatus.Draft)
        {
            return WorkflowResult.Fail("Only a draft order can be issued.");
        }

        if (order.Lines.Count == 0 || string.IsNullOrWhiteSpace(order.PartyName))
        {
            return WorkflowResult.Fail("The order needs a party name and at least one line before it can be issued.");
        }

        order.Status = OrderStatus.Issued;
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"{order.Number} was issued.", order.Id);
    }

    public async Task<WorkflowResult> CancelOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var order = await _repository.Set<OrderForm>()
            .Include(o => o.Invoices)
            .FirstOrDefaultAsync(o => o.Id == id && o.OrganizationId == organization.Id, cancellationToken);
        if (order is null || order.Status is OrderStatus.Cancelled or OrderStatus.Closed)
        {
            return WorkflowResult.Fail("This order cannot be cancelled.");
        }

        if (order.Invoices.Any(i => i.Status is not (InvoiceStatus.Draft or InvoiceStatus.Cancelled)))
        {
            return WorkflowResult.Fail("Cancel or remove the live invoices on this order before cancelling it.");
        }

        order.Status = OrderStatus.Cancelled;
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"{order.Number} was cancelled.", order.Id);
    }

    public async Task<WorkflowResult> SaveInvoiceAsync(InvoiceDraft draft, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(draft.PartyName))
        {
            return WorkflowResult.Fail("Enter the name on the invoice.");
        }

        var lines = BuildInvoiceLines(draft.Lines);
        if (lines.Count == 0)
        {
            return WorkflowResult.Fail("Add at least one invoice line.");
        }

        OrderForm? order = null;
        if (draft.OrderFormId is int orderId)
        {
            order = await _repository.Set<OrderForm>()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.OrganizationId == organization.Id, cancellationToken);
            if (order is null || order.Status is OrderStatus.Draft or OrderStatus.Cancelled)
            {
                return WorkflowResult.Fail("Invoices can be raised only against an issued order.");
            }
        }

        if (draft.TradingCompanyId is int companyId)
        {
            var companyOk = await _repository.Set<TradingCompany>()
                .AnyAsync(c => c.Id == companyId && c.OrganizationId == organization.Id, cancellationToken);
            if (!companyOk)
            {
                return WorkflowResult.Fail("The selected company is not available.");
            }
        }

        Invoice invoice;
        if (draft.Id is int id)
        {
            invoice = await _repository.Set<Invoice>()
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organization.Id, cancellationToken)
                ?? throw new InvalidOperationException("Invoice was not found.");
            if (invoice.Status != InvoiceStatus.Draft)
            {
                return WorkflowResult.Fail("Only a draft invoice can be edited.");
            }

            foreach (var existing in invoice.Lines.ToList())
            {
                _repository.Remove(existing);
            }

            invoice.Lines.Clear();
        }
        else
        {
            invoice = new Invoice
            {
                OrganizationId = organization.Id,
                Number = await NextInvoiceNumberAsync(organization.Id, cancellationToken),
                Status = InvoiceStatus.Draft
            };
            _repository.Add(invoice);
        }

        invoice.Direction = draft.Direction;
        invoice.OrderFormId = order?.Id;
        invoice.TradingCompanyId = draft.TradingCompanyId ?? order?.TradingCompanyId;
        invoice.ExternalNumber = Clean(draft.ExternalNumber);
        invoice.PartyName = draft.PartyName.Trim();
        invoice.ContactName = Clean(draft.ContactName);
        invoice.Address = Clean(draft.Address);
        invoice.InvoiceDate = draft.InvoiceDate;
        invoice.DueOn = draft.DueOn;
        invoice.VatMode = draft.VatMode;
        invoice.VatRate = draft.VatMode == VatMode.Exempt ? 0 : draft.VatRate;
        invoice.Notes = Clean(draft.Notes);
        foreach (var line in lines)
        {
            invoice.Lines.Add(line);
        }

        ApplyInvoiceTotals(invoice);
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success("Invoice saved.", invoice.Id);
    }

    public async Task<WorkflowResult> IssueInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoice(id, cancellationToken);
        if (invoice is null || invoice.Status != InvoiceStatus.Draft)
        {
            return WorkflowResult.Fail("Only a draft invoice can be issued.");
        }

        if (invoice.Lines.Count == 0)
        {
            return WorkflowResult.Fail("Add invoice lines before issuing.");
        }

        invoice.Status = InvoiceStatus.Issued;
        if (invoice.OrderFormId is int orderId)
        {
            await RefreshOrderAsync(orderId, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"{invoice.Number} was issued.", invoice.Id);
    }

    public async Task<WorkflowResult> ApproveInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoice(id, cancellationToken);
        if (invoice is null || invoice.Status != InvoiceStatus.Issued)
        {
            return WorkflowResult.Fail("Only an issued invoice can be approved.");
        }

        invoice.Status = InvoiceStatus.Approved;
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"{invoice.Number} was approved.", invoice.Id);
    }

    public async Task<WorkflowResult> PayInvoiceAsync(int id, PaymentDraft draft, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoice(id, cancellationToken);
        if (invoice is null || invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.Approved))
        {
            return WorkflowResult.Fail("Record payment only on an issued or approved invoice.");
        }

        var ugx = Money.Round(draft.AmountUgx, Money.Ugx);
        var usd = Money.Round(draft.AmountUsd, Money.Usd);
        if (ugx < 0 || usd < 0 || (ugx == 0 && usd == 0))
        {
            return WorkflowResult.Fail("Enter the amount paid.");
        }

        if (ugx > invoice.GrossUgx || usd > invoice.GrossUsd)
        {
            return WorkflowResult.Fail("The payment cannot exceed the invoice total.");
        }

        invoice.PaidUgx = ugx;
        invoice.PaidUsd = usd;
        var ugxSettled = invoice.GrossUgx <= 0 || ugx >= invoice.GrossUgx;
        var usdSettled = invoice.GrossUsd <= 0 || usd >= invoice.GrossUsd;
        if (ugxSettled && usdSettled)
        {
            invoice.Status = InvoiceStatus.Paid;
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success(invoice.Status == InvoiceStatus.Paid
            ? $"{invoice.Number} was marked paid."
            : $"A partial payment was recorded on {invoice.Number}.", invoice.Id);
    }

    public async Task<WorkflowResult> CancelInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await LoadInvoice(id, cancellationToken);
        if (invoice is null || invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
        {
            return WorkflowResult.Fail("A paid invoice cannot be cancelled.");
        }

        invoice.Status = InvoiceStatus.Cancelled;
        if (invoice.OrderFormId is int orderId)
        {
            await RefreshOrderAsync(orderId, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"{invoice.Number} was cancelled.", invoice.Id);
    }

    private async Task<Invoice?> LoadInvoice(int id, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        return await _repository.Set<Invoice>()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organization.Id, cancellationToken);
    }

    private async Task RefreshOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await _repository.Set<OrderForm>()
            .Include(o => o.Invoices)
            .FirstAsync(o => o.Id == orderId, cancellationToken);
        if (order.Status is OrderStatus.Draft or OrderStatus.Cancelled or OrderStatus.Closed)
        {
            return;
        }

        var live = order.Invoices.Where(i => i.Status is not (InvoiceStatus.Draft or InvoiceStatus.Cancelled)).ToList();
        var ugx = live.Sum(i => i.GrossUgx);
        var usd = live.Sum(i => i.GrossUsd);
        var any = ugx > 0 || usd > 0;
        var coversUgx = order.GrossUgx <= 0 || ugx + 1 >= order.GrossUgx;
        var coversUsd = order.GrossUsd <= 0 || usd + 0.01m >= order.GrossUsd;
        order.Status = any && coversUgx && coversUsd
            ? OrderStatus.Invoiced
            : any ? OrderStatus.PartiallyInvoiced : OrderStatus.Issued;
    }

    private static void ApplyTotals(OrderForm order)
    {
        var (netUgx, vatUgx, grossUgx) = VatCalculator.Calculate(
            order.Lines.Where(l => l.Currency == Money.Ugx).Sum(l => l.Amount), order.VatMode, order.VatRate, Money.Ugx);
        var (netUsd, vatUsd, grossUsd) = VatCalculator.Calculate(
            order.Lines.Where(l => l.Currency == Money.Usd).Sum(l => l.Amount), order.VatMode, order.VatRate, Money.Usd);
        order.NetUgx = netUgx;
        order.VatUgx = vatUgx;
        order.GrossUgx = grossUgx;
        order.NetUsd = netUsd;
        order.VatUsd = vatUsd;
        order.GrossUsd = grossUsd;
    }

    private static void ApplyInvoiceTotals(Invoice invoice)
    {
        var (netUgx, vatUgx, grossUgx) = VatCalculator.Calculate(
            invoice.Lines.Where(l => l.Currency == Money.Ugx).Sum(l => l.Amount), invoice.VatMode, invoice.VatRate, Money.Ugx);
        var (netUsd, vatUsd, grossUsd) = VatCalculator.Calculate(
            invoice.Lines.Where(l => l.Currency == Money.Usd).Sum(l => l.Amount), invoice.VatMode, invoice.VatRate, Money.Usd);
        invoice.NetUgx = netUgx;
        invoice.VatUgx = vatUgx;
        invoice.GrossUgx = grossUgx;
        invoice.NetUsd = netUsd;
        invoice.VatUsd = vatUsd;
        invoice.GrossUsd = grossUsd;
    }

    private static List<OrderLine> BuildOrderLines(IEnumerable<MoneyLineDraft> drafts, OrderKind kind)
    {
        var lines = new List<OrderLine>();
        var number = 1;
        foreach (var draft in drafts)
        {
            var description = string.IsNullOrWhiteSpace(draft.Description) ? draft.Programme : draft.Description;
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            var currency = Money.Normalize(draft.Currency);
            var quantity = kind == OrderKind.Advertising && draft.Insertions is > 0
                ? draft.Insertions.Value
                : draft.Quantity <= 0 ? 1 : draft.Quantity;
            var unit = Money.Round(draft.UnitAmount, currency);
            var amount = Money.Round(quantity * unit, currency);
            if (amount <= 0)
            {
                continue;
            }

            lines.Add(new OrderLine
            {
                LineNumber = number++,
                Quantity = quantity,
                Description = description.Trim(),
                UnitPrice = unit,
                Amount = amount,
                Currency = currency,
                Programme = Clean(draft.Programme),
                LengthSeconds = draft.LengthSeconds,
                Insertions = draft.Insertions
            });
        }

        return lines;
    }

    private static List<InvoiceLine> BuildInvoiceLines(IEnumerable<MoneyLineDraft> drafts)
    {
        var lines = new List<InvoiceLine>();
        var number = 1;
        foreach (var draft in drafts)
        {
            if (string.IsNullOrWhiteSpace(draft.Description))
            {
                continue;
            }

            var currency = Money.Normalize(draft.Currency);
            var quantity = draft.Quantity <= 0 ? 1 : draft.Quantity;
            var unit = Money.Round(draft.UnitAmount, currency);
            var amount = Money.Round(quantity * unit, currency);
            if (amount <= 0)
            {
                continue;
            }

            lines.Add(new InvoiceLine
            {
                LineNumber = number++,
                Quantity = quantity,
                Description = draft.Description.Trim(),
                UnitPrice = unit,
                Amount = amount,
                Currency = currency
            });
        }

        return lines;
    }

    private async Task<string> NextNumberAsync(int organizationId, string prefix, CancellationToken cancellationToken)
    {
        var head = $"{prefix}-{DateTime.Today.Year}-";
        var numbers = await _repository.Set<OrderForm>()
            .Where(o => o.OrganizationId == organizationId && o.Number.StartsWith(head))
            .Select(o => o.Number)
            .ToListAsync(cancellationToken);
        return $"{head}{PettyCashService.NextSequence(numbers, head):0000}";
    }

    private async Task<string> NextInvoiceNumberAsync(int organizationId, CancellationToken cancellationToken)
    {
        var head = $"INV-{DateTime.Today.Year}-";
        var numbers = await _repository.Set<Invoice>()
            .Where(i => i.OrganizationId == organizationId && i.Number.StartsWith(head))
            .Select(i => i.Number)
            .ToListAsync(cancellationToken);
        return $"{head}{PettyCashService.NextSequence(numbers, head):0000}";
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
