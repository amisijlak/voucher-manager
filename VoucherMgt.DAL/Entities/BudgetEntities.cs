using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VoucherMgt.Common;

namespace VoucherMgt.DAL.Entities;

[Table("BudgetCategories", Schema = DatabaseSchemas.Voucher)]
public class BudgetCategory : BaseEntity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    [Required, MaxLength(8)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

[Table("BudgetPeriods", Schema = DatabaseSchemas.Voucher)]
public class BudgetPeriod : BaseEntity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public List<BudgetLine> Lines { get; set; } = [];
}

[Table("BudgetLines", Schema = DatabaseSchemas.Voucher)]
public class BudgetLine : BaseEntity
{
    public int BudgetPeriodId { get; set; }
    public BudgetPeriod BudgetPeriod { get; set; } = default!;

    public int? CategoryId { get; set; }
    public BudgetCategory? Category { get; set; }

    public int? TradingCompanyId { get; set; }
    public TradingCompany? TradingCompany { get; set; }

    public int LineNumber { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = Money.Ugx;
}
