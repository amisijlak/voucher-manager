using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace VoucherMgt.DAL.Entities;

[Table("Users", Schema = DatabaseSchemas.Voucher)]
public class ApplicationUser : IdentityUser
{
    [ForeignKey(nameof(Organization))]
    public int OrganizationId { get; set; }

    public virtual Organization Organization { get; set; } = default!;

    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; }
}

[Table("Organizations", Schema = DatabaseSchemas.Voucher)]
public class Organization : BaseEntity
{
    [Required, MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(80)]
    public string City { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Country { get; set; } = "Uganda";

    public bool IsActive { get; set; } = true;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DefaultVatRate { get; set; } = 18m;

    [MaxLength(300)]
    public string PaymentNote { get; set; } = "All payments should be made to the respective company accounts.";

    public virtual List<OrganizationLicense> Licenses { get; set; } = [];
    public virtual List<ApplicationUser> Users { get; set; } = [];
    public virtual List<TradingCompany> Companies { get; set; } = [];
}

[Table("OrganizationLicenses", Schema = DatabaseSchemas.Voucher)]
public class OrganizationLicense : BaseEntity
{
    [ForeignKey(nameof(Organization))]
    public int OrganizationId { get; set; }

    public virtual Organization Organization { get; set; } = default!;

    [Required, MaxLength(80)]
    public string LicenseKey { get; set; } = string.Empty;

    public DateOnly StartsOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly ExpiresOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(1));

    public bool IsRevoked { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public bool IsValidOn(DateOnly date) => !IsRevoked && StartsOn <= date && ExpiresOn >= date;
}

[Table("TradingCompanies", Schema = DatabaseSchemas.Voucher)]
public class TradingCompany : BaseEntity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string ShortName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Contact { get; set; }

    [MaxLength(300)]
    public string? LogoFilePath { get; set; }

    [MaxLength(200)]
    public string? LogoFileName { get; set; }

    [MaxLength(80)]
    public string? AccountNumber { get; set; }

    public bool IsActive { get; set; } = true;
}
