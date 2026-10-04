using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VoucherMgt.Common;

namespace VoucherMgt.DAL.Entities;

[Table("Requisitions", Schema = DatabaseSchemas.Voucher)]
public class Requisition : BaseEntity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    [MaxLength(80)]
    public string? Department { get; set; }

    [MaxLength(500)]
    public string? Purpose { get; set; }

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;

    [Required, MaxLength(120)]
    public string RequestedByName { get; set; } = string.Empty;

    [MaxLength(450)]
    public string? RequestedByUserId { get; set; }

    public int? BudgetPeriodId { get; set; }
    public BudgetPeriod? BudgetPeriod { get; set; }

    public DateTimeOffset? SubmittedOn { get; set; }

    [MaxLength(500)]
    public string? DecisionNote { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RequestedUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RequestedUsd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ApprovedUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ApprovedUsd { get; set; }

    public List<RequisitionLine> Lines { get; set; } = [];
    public List<ApprovalDecision> Decisions { get; set; } = [];
    public Disbursement? Disbursement { get; set; }
    public Accountability? Accountability { get; set; }
}

[Table("RequisitionLines", Schema = DatabaseSchemas.Voucher)]
public class RequisitionLine : BaseEntity
{
    public int RequisitionId { get; set; }
    public Requisition Requisition { get; set; } = default!;

    public int LineNumber { get; set; }

    [Required, MaxLength(400)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = Money.Ugx;

    public int? BudgetLineId { get; set; }
    public BudgetLine? BudgetLine { get; set; }
}

[Table("ApprovalDecisions", Schema = DatabaseSchemas.Voucher)]
public class ApprovalDecision : BaseEntity
{
    public int RequisitionId { get; set; }
    public Requisition Requisition { get; set; } = default!;

    public ApprovalAction Action { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ApprovedUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ApprovedUsd { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }

    [MaxLength(450)]
    public string? DecidedByUserId { get; set; }

    [Required, MaxLength(120)]
    public string DecidedByName { get; set; } = string.Empty;
}

[Table("Disbursements", Schema = DatabaseSchemas.Voucher)]
public class Disbursement : BaseEntity
{
    public int RequisitionId { get; set; }
    public Requisition Requisition { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountUgx { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountUsd { get; set; }

    [Required, MaxLength(30)]
    public string Method { get; set; } = "Cash";

    [MaxLength(80)]
    public string? Reference { get; set; }

    public DateOnly PaidOn { get; set; }

    [MaxLength(450)]
    public string? PaidByUserId { get; set; }

    [Required, MaxLength(120)]
    public string PaidByName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Notes { get; set; }
}

[Table("Accountabilities", Schema = DatabaseSchemas.Voucher)]
public class Accountability : BaseEntity
{
    public int RequisitionId { get; set; }
    public Requisition Requisition { get; set; } = default!;

    public AccountabilityStatus Status { get; set; } = AccountabilityStatus.Draft;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? ReviewComments { get; set; }

    public DateTimeOffset? SubmittedOn { get; set; }

    [MaxLength(120)]
    public string? SubmittedByName { get; set; }

    public List<AccountabilityLine> Lines { get; set; } = [];
}

[Table("AccountabilityLines", Schema = DatabaseSchemas.Voucher)]
public class AccountabilityLine : BaseEntity
{
    public int AccountabilityId { get; set; }
    public Accountability Accountability { get; set; } = default!;

    public int LineNumber { get; set; }

    public int? RequisitionLineId { get; set; }

    public DateOnly? SpentOn { get; set; }

    [Required, MaxLength(400)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? ReceiptNumber { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = Money.Ugx;

    [MaxLength(300)]
    public string? ReceiptFilePath { get; set; }

    [MaxLength(200)]
    public string? ReceiptFileName { get; set; }
}
