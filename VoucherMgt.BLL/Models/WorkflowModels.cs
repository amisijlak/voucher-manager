using VoucherMgt.Common;

namespace VoucherMgt.BLL.Models;

public sealed record WorkflowResult(bool Ok, string Message, int? Id = null)
{
    public static WorkflowResult Success(string message, int? id = null) => new(true, message, id);
    public static WorkflowResult Fail(string message) => new(false, message);
}

public sealed class Actor
{
    public required string UserId { get; init; }
    public required string Name { get; init; }
}

public sealed class MoneyLineDraft
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitAmount { get; set; }
    public string Currency { get; set; } = Money.Ugx;
    public int? BudgetLineId { get; set; }
    public string? Programme { get; set; }
    public int? LengthSeconds { get; set; }
    public int? Insertions { get; set; }
}

public sealed class RequisitionDraft
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string? Department { get; set; }
    public string? Purpose { get; set; }
    public int? BudgetPeriodId { get; set; }
    public List<MoneyLineDraft> Lines { get; set; } = [];
}

public sealed class DecisionDraft
{
    public ApprovalAction Action { get; set; }
    public decimal ApprovedUgx { get; set; }
    public decimal ApprovedUsd { get; set; }
    public string? Comments { get; set; }
}

public sealed class DisbursementDraft
{
    public decimal AmountUgx { get; set; }
    public decimal AmountUsd { get; set; }
    public string Method { get; set; } = "Cash";
    public string? Reference { get; set; }
    public DateOnly PaidOn { get; set; }
    public string? Notes { get; set; }
}

public sealed class AccountabilityLineDraft
{
    public DateOnly SpentOn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReceiptNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = Money.Ugx;
    public string? ReceiptFilePath { get; set; }
    public string? ReceiptFileName { get; set; }
}

public sealed class AccountabilityDraft
{
    public string? Notes { get; set; }
    public List<AccountabilityLineDraft> Lines { get; set; } = [];
}

public sealed class OrderDraft
{
    public int? Id { get; set; }
    public OrderKind Kind { get; set; } = OrderKind.General;
    public OrderDirection Direction { get; set; } = OrderDirection.ClientOrder;
    public int TradingCompanyId { get; set; }
    public int? RequisitionId { get; set; }
    public string PartyName { get; set; } = string.Empty;
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
    public decimal VatRate { get; set; } = 18m;
    public string? PaymentNote { get; set; }
    public string? ClientSignatoryName { get; set; }
    public string? ClientDesignation { get; set; }
    public DateOnly? ClientSignedOn { get; set; }
    public string? BusinessManager { get; set; }
    public string? BusinessSupervisor { get; set; }
    public string? AccountNumber { get; set; }
    public string? CreditControl { get; set; }
    public List<MoneyLineDraft> Lines { get; set; } = [];
}

public sealed class InvoiceDraft
{
    public int? Id { get; set; }
    public InvoiceDirection Direction { get; set; } = InvoiceDirection.Receivable;
    public int? OrderFormId { get; set; }
    public int? TradingCompanyId { get; set; }
    public string? ExternalNumber { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Address { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueOn { get; set; }
    public VatMode VatMode { get; set; } = VatMode.Exclusive;
    public decimal VatRate { get; set; } = 18m;
    public string? Notes { get; set; }
    public List<MoneyLineDraft> Lines { get; set; } = [];
}

public sealed class PaymentDraft
{
    public decimal AmountUgx { get; set; }
    public decimal AmountUsd { get; set; }
    public string? Reference { get; set; }
}

public sealed class BudgetLineDraft
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = Money.Ugx;
    public int? CategoryId { get; set; }
    public int? TradingCompanyId { get; set; }
}

public sealed class BudgetPeriodDraft
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string? Notes { get; set; }
    public List<BudgetLineDraft> Lines { get; set; } = [];
}

public sealed class DashboardSummary
{
    public int OpenRequisitions { get; init; }
    public int AwaitingApproval { get; init; }
    public int AwaitingAccountability { get; init; }
    public int OpenOrders { get; init; }
    public int UnpaidInvoices { get; init; }
    public decimal UnaccountedUgx { get; init; }
    public decimal UnaccountedUsd { get; init; }
    public decimal BudgetUgx { get; init; }
    public decimal BudgetCommittedUgx { get; init; }
    public string? ActiveBudgetName { get; init; }
}
