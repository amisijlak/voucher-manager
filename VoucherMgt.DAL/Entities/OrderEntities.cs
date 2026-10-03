using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VoucherMgt.Common;

namespace VoucherMgt.DAL.Entities;

[Table("OrderForms", Schema = DatabaseSchemas.Voucher)]
public class OrderForm : BaseEntity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    public OrderKind Kind { get; set; } = OrderKind.General;
    public OrderDirection Direction { get; set; } = OrderDirection.ClientOrder;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public int TradingCompanyId { get; set; }
    public TradingCompany TradingCompany { get; set; } = default!;

    public int? RequisitionId { get; set; }
    public Requisition? Requisition { get; set; }

    [Required, MaxLength(200)]
    public string PartyName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? ContactName { get; set; }

    [MaxLength(200)]
    public string? InvoiceName { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(80)]
    public string? CityTown { get; set; }

    [MaxLength(80)]
    public string? Country { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [MaxLength(40)]
    public string? Fax { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(80)]
    public string? Channel { get; set; }

    public VatMode VatMode { get; set; } = VatMode.Exclusive;

    [Column(TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; } = 18m;

    [MaxLength(300)]
    public string? PaymentNote { get; set; }

    [MaxLength(120)]
    public string? ClientSignatoryName { get; set; }

    [MaxLength(120)]
    public string? ClientDesignation { get; set; }

    public DateOnly? ClientSignedOn { get; set; }

    [MaxLength(120)]
    public string? BusinessManager { get; set; }

    [MaxLength(120)]
    public string? BusinessSupervisor { get; set; }

    [MaxLength(80)]
    public string? AccountNumber { get; set; }

    [MaxLength(120)]
    public string? CreditControl { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetUsd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatUsd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossUsd { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
    public List<Invoice> Invoices { get; set; } = [];
}

[Table("OrderLines", Schema = DatabaseSchemas.Voucher)]
public class OrderLine : BaseEntity
{
    public int OrderFormId { get; set; }
    public OrderForm OrderForm { get; set; } = default!;

    public int LineNumber { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; } = 1;

    [Required, MaxLength(400)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = Money.Ugx;

    [MaxLength(160)]
    public string? Programme { get; set; }

    public int? LengthSeconds { get; set; }
    public int? Insertions { get; set; }
}

[Table("Invoices", Schema = DatabaseSchemas.Voucher)]
public class Invoice : BaseEntity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? ExternalNumber { get; set; }

    public InvoiceDirection Direction { get; set; } = InvoiceDirection.Receivable;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public int? OrderFormId { get; set; }
    public OrderForm? OrderForm { get; set; }

    public int? TradingCompanyId { get; set; }
    public TradingCompany? TradingCompany { get; set; }

    [Required, MaxLength(200)]
    public string PartyName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? ContactName { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? DueOn { get; set; }

    public VatMode VatMode { get; set; } = VatMode.Exclusive;

    [Column(TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; } = 18m;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetUsd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatUsd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossUsd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidUsd { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];
}

[Table("InvoiceLines", Schema = DatabaseSchemas.Voucher)]
public class InvoiceLine : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = default!;

    public int LineNumber { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; } = 1;

    [Required, MaxLength(400)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = Money.Ugx;
}
