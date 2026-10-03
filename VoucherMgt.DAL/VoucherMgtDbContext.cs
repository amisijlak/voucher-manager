using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.Common;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.DAL;

public class VoucherMgtDbContext : IdentityDbContext<ApplicationUser>
{
    public VoucherMgtDbContext(DbContextOptions<VoucherMgtDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationLicense> OrganizationLicenses => Set<OrganizationLicense>();
    public DbSet<TradingCompany> TradingCompanies => Set<TradingCompany>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<BudgetPeriod> BudgetPeriods => Set<BudgetPeriod>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<Requisition> Requisitions => Set<Requisition>();
    public DbSet<RequisitionLine> RequisitionLines => Set<RequisitionLine>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();
    public DbSet<Disbursement> Disbursements => Set<Disbursement>();
    public DbSet<Accountability> Accountabilities => Set<Accountability>();
    public DbSet<AccountabilityLine> AccountabilityLines => Set<AccountabilityLine>();
    public DbSet<OrderForm> OrderForms => Set<OrderForm>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DatabaseSchemas.Voucher);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FullName).HasMaxLength(120);
            entity.HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<IdentityRole>(entity => entity.ToTable("Roles"));
        modelBuilder.Entity<IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
        modelBuilder.Entity<IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
        modelBuilder.Entity<IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
        modelBuilder.Entity<IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));
        modelBuilder.Entity<IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasIndex(o => o.Code).IsUnique();
            entity.Property(o => o.Code).HasMaxLength(80).IsRequired();
            entity.Property(o => o.Name).HasMaxLength(200).IsRequired();
            entity.Property(o => o.DefaultVatRate).HasPrecision(5, 2);
        });

        modelBuilder.Entity<OrganizationLicense>(entity =>
        {
            entity.ToTable("OrganizationLicenses");
            entity.HasIndex(l => l.LicenseKey).IsUnique();
            entity.HasOne(l => l.Organization)
                .WithMany(o => o.Licenses)
                .HasForeignKey(l => l.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TradingCompany>(entity =>
        {
            entity.ToTable("TradingCompanies");
            entity.HasIndex(c => new { c.OrganizationId, c.Name }).IsUnique();
            entity.HasOne(c => c.Organization)
                .WithMany(o => o.Companies)
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetCategory>(entity =>
        {
            entity.ToTable("BudgetCategories");
            entity.HasIndex(c => new { c.OrganizationId, c.Code }).IsUnique();
            entity.HasOne(c => c.Organization)
                .WithMany()
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetPeriod>(entity =>
        {
            entity.ToTable("BudgetPeriods");
            entity.HasOne(p => p.Organization)
                .WithMany()
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BudgetLine>(entity =>
        {
            entity.ToTable("BudgetLines");
            entity.Property(l => l.Amount).HasPrecision(18, 2);
            entity.HasIndex(l => new { l.BudgetPeriodId, l.LineNumber }).IsUnique();
            entity.HasOne(l => l.BudgetPeriod)
                .WithMany(p => p.Lines)
                .HasForeignKey(l => l.BudgetPeriodId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(l => l.Category)
                .WithMany()
                .HasForeignKey(l => l.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(l => l.TradingCompany)
                .WithMany()
                .HasForeignKey(l => l.TradingCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Requisition>(entity =>
        {
            entity.ToTable("Requisitions");
            entity.HasIndex(r => new { r.OrganizationId, r.Number }).IsUnique();
            entity.Property(r => r.RequestedUgx).HasPrecision(18, 2);
            entity.Property(r => r.RequestedUsd).HasPrecision(18, 2);
            entity.Property(r => r.ApprovedUgx).HasPrecision(18, 2);
            entity.Property(r => r.ApprovedUsd).HasPrecision(18, 2);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(r => r.Organization)
                .WithMany()
                .HasForeignKey(r => r.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.BudgetPeriod)
                .WithMany()
                .HasForeignKey(r => r.BudgetPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequisitionLine>(entity =>
        {
            entity.ToTable("RequisitionLines");
            entity.Property(l => l.Quantity).HasPrecision(18, 2);
            entity.Property(l => l.UnitAmount).HasPrecision(18, 2);
            entity.Property(l => l.Amount).HasPrecision(18, 2);
            entity.HasIndex(l => new { l.RequisitionId, l.LineNumber }).IsUnique();
            entity.HasOne(l => l.Requisition)
                .WithMany(r => r.Lines)
                .HasForeignKey(l => l.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(l => l.BudgetLine)
                .WithMany()
                .HasForeignKey(l => l.BudgetLineId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalDecision>(entity =>
        {
            entity.ToTable("ApprovalDecisions");
            entity.Property(d => d.ApprovedUgx).HasPrecision(18, 2);
            entity.Property(d => d.ApprovedUsd).HasPrecision(18, 2);
            entity.Property(d => d.Action).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(d => d.Requisition)
                .WithMany(r => r.Decisions)
                .HasForeignKey(d => d.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Disbursement>(entity =>
        {
            entity.ToTable("Disbursements");
            entity.Property(d => d.AmountUgx).HasPrecision(18, 2);
            entity.Property(d => d.AmountUsd).HasPrecision(18, 2);
            entity.HasIndex(d => d.RequisitionId).IsUnique();
            entity.HasOne(d => d.Requisition)
                .WithOne(r => r.Disbursement)
                .HasForeignKey<Disbursement>(d => d.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Accountability>(entity =>
        {
            entity.ToTable("Accountabilities");
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(a => a.RequisitionId).IsUnique();
            entity.HasOne(a => a.Requisition)
                .WithOne(r => r.Accountability)
                .HasForeignKey<Accountability>(a => a.RequisitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountabilityLine>(entity =>
        {
            entity.ToTable("AccountabilityLines");
            entity.Property(l => l.Amount).HasPrecision(18, 2);
            entity.HasOne(l => l.Accountability)
                .WithMany(a => a.Lines)
                .HasForeignKey(l => l.AccountabilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderForm>(entity =>
        {
            entity.ToTable("OrderForms");
            entity.HasIndex(o => new { o.OrganizationId, o.Number }).IsUnique();
            entity.Property(o => o.Kind).HasConversion<string>().HasMaxLength(20);
            entity.Property(o => o.Direction).HasConversion<string>().HasMaxLength(20);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(o => o.VatMode).HasConversion<string>().HasMaxLength(20);
            entity.Property(o => o.VatRate).HasPrecision(5, 2);
            foreach (var amount in new[] { "NetUgx", "VatUgx", "GrossUgx", "NetUsd", "VatUsd", "GrossUsd" })
            {
                entity.Property(amount).HasPrecision(18, 2);
            }

            entity.HasOne(o => o.Organization)
                .WithMany()
                .HasForeignKey(o => o.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.TradingCompany)
                .WithMany()
                .HasForeignKey(o => o.TradingCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.Requisition)
                .WithMany()
                .HasForeignKey(o => o.RequisitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.ToTable("OrderLines");
            entity.Property(l => l.Quantity).HasPrecision(18, 2);
            entity.Property(l => l.UnitPrice).HasPrecision(18, 2);
            entity.Property(l => l.Amount).HasPrecision(18, 2);
            entity.HasOne(l => l.OrderForm)
                .WithMany(o => o.Lines)
                .HasForeignKey(l => l.OrderFormId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasIndex(i => new { i.OrganizationId, i.Number }).IsUnique();
            entity.Property(i => i.Direction).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.VatMode).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.VatRate).HasPrecision(5, 2);
            foreach (var amount in new[] { "NetUgx", "VatUgx", "GrossUgx", "NetUsd", "VatUsd", "GrossUsd", "PaidUgx", "PaidUsd" })
            {
                entity.Property(amount).HasPrecision(18, 2);
            }

            entity.HasOne(i => i.Organization)
                .WithMany()
                .HasForeignKey(i => i.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.OrderForm)
                .WithMany(o => o.Invoices)
                .HasForeignKey(i => i.OrderFormId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.TradingCompany)
                .WithMany()
                .HasForeignKey(i => i.TradingCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.ToTable("InvoiceLines");
            entity.Property(l => l.Quantity).HasPrecision(18, 2);
            entity.Property(l => l.UnitPrice).HasPrecision(18, 2);
            entity.Property(l => l.Amount).HasPrecision(18, 2);
            entity.HasOne(l => l.Invoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn ??= now;
                entry.Entity.LastUpdatedOn = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastUpdatedOn = now;
            }
        }
    }
}
