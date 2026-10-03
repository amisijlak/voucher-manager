using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.Common;
using VoucherMgt.DAL;
using VoucherMgt.DAL.Entities;
using VoucherMgt.Web.Authorization;

namespace VoucherMgt.Web.Middleware;

public static class OrganizationSeeder
{
    public const string GeneralRequisitionNumber = "REQ-2026-0001";
    public const string SeptemberBudgetName = "Provisional Budget — 15th to 30th September 2026";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<VoucherMgtDbContext>();
        var code = (configuration["DefaultOrganization:Code"] ?? "status-one").Trim().ToLowerInvariant();
        var organization = await db.Organizations.Include(o => o.Licenses).FirstOrDefaultAsync(o => o.Code == code);
        if (organization is null)
        {
            organization = new Organization
            {
                Code = code,
                Name = configuration["DefaultOrganization:Name"] ?? "Status One Group",
                Email = configuration["DefaultOrganization:Email"] ?? "admin@statusone.local",
                Phone = configuration["DefaultOrganization:Phone"] ?? string.Empty,
                City = "Kampala",
                Country = "Uganda",
                IsActive = true,
                DefaultVatRate = 18m,
                PaymentNote = "All payments should be made to the respective company accounts."
            };
            db.Organizations.Add(organization);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (!organization.Licenses.Any(l => l.IsValidOn(today)))
        {
            var key = $"LIC-{code.ToUpperInvariant()}-{Guid.NewGuid():N}";
            organization.Licenses.Add(new OrganizationLicense
            {
                LicenseKey = key[..Math.Min(80, key.Length)],
                StartsOn = today,
                ExpiresOn = today.AddYears(1),
                Notes = "Default bootstrap license"
            });
        }

        await db.SaveChangesAsync();
        await SeedSecurityAsync(services, configuration, organization);
        await SeedCompaniesAsync(db, organization.Id);
        await SeedBudgetAsync(db, organization.Id);
        await SeedGeneralRequisitionAsync(db, organization.Id);
    }

    private static async Task SeedSecurityAsync(IServiceProvider services, IConfiguration configuration, Organization organization)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        await EnsureRoleAsync(roleManager, "User", [AppPermissions.DashboardView]);
        await EnsureRoleAsync(roleManager, "Requester",
        [
            AppPermissions.DashboardView,
            AppPermissions.RequisitionsView,
            AppPermissions.RequisitionsCreate,
            AppPermissions.RequisitionsSubmit,
            AppPermissions.AccountabilityView,
            AppPermissions.AccountabilitySubmit,
            AppPermissions.OrdersView,
            AppPermissions.BudgetsView
        ]);
        await EnsureRoleAsync(roleManager, "Approver",
        [
            AppPermissions.DashboardView,
            AppPermissions.RequisitionsView,
            AppPermissions.ApprovalsView,
            AppPermissions.ApprovalsDecide,
            AppPermissions.BudgetsView,
            AppPermissions.ReportsView
        ]);
        await EnsureRoleAsync(roleManager, "Accountant",
        [
            AppPermissions.DashboardView,
            AppPermissions.RequisitionsView,
            AppPermissions.DisbursementsRecord,
            AppPermissions.AccountabilityView,
            AppPermissions.AccountabilityReview,
            AppPermissions.InvoicesView,
            AppPermissions.InvoicesCreate,
            AppPermissions.InvoicesApprove,
            AppPermissions.InvoicesPay,
            AppPermissions.OrdersView,
            AppPermissions.BudgetsView,
            AppPermissions.BudgetsManage,
            AppPermissions.ReportsView
        ]);
        await EnsureRoleAsync(roleManager, "Procurement",
        [
            AppPermissions.DashboardView,
            AppPermissions.RequisitionsView,
            AppPermissions.OrdersView,
            AppPermissions.OrdersCreate,
            AppPermissions.OrdersIssue,
            AppPermissions.InvoicesView,
            AppPermissions.InvoicesCreate,
            AppPermissions.BudgetsView
        ]);

        await EnsureUserAsync(userManager, organization, configuration["DefaultAdmin:Email"] ?? "admin@statusone.local",
            configuration["DefaultAdmin:Password"] ?? "Admin@123", "System Administrator", "Admin");
        await EnsureUserAsync(userManager, organization, "requester@statusone.local", "User@123", "Office Requester", "Requester");
        await EnsureUserAsync(userManager, organization, "approver@statusone.local", "User@123", "Finance Approver", "Approver");
        await EnsureUserAsync(userManager, organization, "accountant@statusone.local", "User@123", "Group Accountant", "Accountant");
        await EnsureUserAsync(userManager, organization, "procurement@statusone.local", "User@123", "Procurement Officer", "Procurement");
    }

    private static async Task SeedCompaniesAsync(VoucherMgtDbContext db, int organizationId)
    {
        var companies = new (string Name, string ShortName)[]
        {
            ("Status Luxury Cars", "Status Luxury"),
            ("Price Locker International", "Price Locker"),
            ("Status One Consultants", "Status One")
        };

        foreach (var company in companies)
        {
            if (!await db.TradingCompanies.AnyAsync(c => c.OrganizationId == organizationId && c.Name == company.Name))
            {
                db.TradingCompanies.Add(new TradingCompany
                {
                    OrganizationId = organizationId,
                    Name = company.Name,
                    ShortName = company.ShortName,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedBudgetAsync(VoucherMgtDbContext db, int organizationId)
    {
        var categories = new (string Code, string Name, int Sort)[]
        {
            ("A", "Staff & HR obligations", 1),
            ("B", "Tax, statutory and accounting", 2),
            ("C", "Staff accommodation", 3),
            ("D", "Office operations", 4),
            ("E", "Staff welfare and daily sundry", 5),
            ("F", "Transport and field operations", 6),
            ("G", "Business development", 7),
            ("H", "Contingency", 8)
        };

        foreach (var category in categories)
        {
            if (!await db.BudgetCategories.AnyAsync(c => c.OrganizationId == organizationId && c.Code == category.Code))
            {
                db.BudgetCategories.Add(new BudgetCategory
                {
                    OrganizationId = organizationId,
                    Code = category.Code,
                    Name = category.Name,
                    SortOrder = category.Sort
                });
            }
        }

        await db.SaveChangesAsync();
        if (await db.BudgetPeriods.AnyAsync(p => p.OrganizationId == organizationId && p.Name == SeptemberBudgetName))
        {
            return;
        }

        var categoryIds = await db.BudgetCategories
            .Where(c => c.OrganizationId == organizationId)
            .ToDictionaryAsync(c => c.Code, c => c.Id);
        var luxuryId = await db.TradingCompanies
            .Where(c => c.OrganizationId == organizationId && c.Name == "Status Luxury Cars")
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();
        var lockerId = await db.TradingCompanies
            .Where(c => c.OrganizationId == organizationId && c.Name == "Price Locker International")
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        var lines = new (int No, string Code, string Description, decimal Amount, int? CompanyId)[]
        {
            (1, "A", "Staff salary — David — September", 850_000, null),
            (2, "A", "Staff salary — Sylvia — September", 500_000, null),
            (3, "A", "Staff salary — Ronald — September", 300_000, null),
            (4, "B", "Accountant — monthly filing", 100_000, null),
            (5, "B", "PAYE + NSSF + VAT — Status Luxury Cars", 373_500, luxuryId),
            (6, "B", "PAYE + NSSF + VAT — Price Locker Uganda", 217_000, lockerId),
            (7, "C", "Security + garbage — monthly provision", 100_000, null),
            (8, "C", "Electricity — September", 100_000, null),
            (9, "C", "Water — September provision", 105_000, null),
            (10, "C", "Internet — September", 130_000, null),
            (11, "D", "Office internet — monthly / renewal", 150_000, null),
            (12, "D", "Office electricity — 2 weeks provision", 150_000, null),
            (13, "D", "Garbage collection — office", 30_000, null),
            (14, "D", "Water / cleaning / consumables — office 2 weeks", 100_000, null),
            (15, "D", "Stationery — paper, printing, binding, files", 30_000, null),
            (16, "E", "Staff lunch / welfare — 16-day provision", 980_000, null),
            (17, "E", "Drinking water — 15-day provision", 100_000, null),
            (18, "E", "Miscellaneous office sundries — soap, tissue, kitchen items", 150_000, null),
            (19, "F", "Errands / business transport — FIA, errands, deliveries", 60_000, null),
            (20, "G", "Status branding work — current branding work", 100_000, null),
            (21, "H", "Operating contingency", 150_000, null)
        };

        db.BudgetPeriods.Add(new BudgetPeriod
        {
            OrganizationId = organizationId,
            Name = SeptemberBudgetName,
            StartsOn = new DateOnly(2026, 9, 15),
            EndsOn = new DateOnly(2026, 9, 30),
            Notes = "Loaded from the provisional budget. Total cash requirement UGX 4,775,500.",
            Lines = lines.Select(line => new BudgetLine
            {
                LineNumber = line.No,
                CategoryId = categoryIds[line.Code],
                TradingCompanyId = line.CompanyId,
                Description = line.Description,
                Amount = line.Amount,
                Currency = Money.Ugx
            }).ToList()
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedGeneralRequisitionAsync(VoucherMgtDbContext db, int organizationId)
    {
        var existing = await db.Requisitions
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Number == GeneralRequisitionNumber);
        if (existing is not null)
        {
            if (existing.Status == RequisitionStatus.Submitted && existing.RequestedUgx != 7_934_000m)
            {
                db.Requisitions.Remove(existing);
                await db.SaveChangesAsync();
            }
            else
            {
                return;
            }
        }

        var lines = new (string Description, decimal Amount, string Currency)[]
        {
            ("Staff salary — David — 01st – 31st Aug", 850_000, Money.Ugx),
            ("Staff salary — Sylvia (Chef) — 01st – 31st Aug", 500_000, Money.Ugx),
            ("Staff salary — Ronald — 01st – 31st Aug", 300_000, Money.Ugx),
            ("Staff accommodation — security + garbage for Aug", 100_000, Money.Ugx),
            ("Staff accommodation — electricity bill for Aug", 100_000, Money.Ugx),
            ("Staff accommodation — water bill for July – Aug", 105_000, Money.Ugx),
            ("Office — water bill (June – July)", 200_000, Money.Ugx),
            ("Reimbursement — office management 21 – 28 Aug", 452_000, Money.Ugx),
            ("Pending balance on general requisition of 20 Aug", 1_362_000, Money.Ugx),
            ("Staff accommodation — rent — September", 1_000, Money.Usd),
            ("Accountant — August filing", 100_000, Money.Ugx),
            ("Staff accommodation — internet", 130_000, Money.Ugx),
            ("PAYE + NSSF + VAT — Status Luxury Cars 366,500 + 7,000; Price Locker Uganda 210,000 + 7,000", 590_500, Money.Ugx),
            ("Reimbursement — office management 29 Aug – 7 Sept", 942_500, Money.Ugx),
            ("Staff salary — Diana — 1st – 31st Aug", 650_000, Money.Ugx),
            ("Staff salary — Brian — 1st – 31st Aug", 500_000, Money.Ugx),
            ("Staff salary — Patience — 14 Aug – 13 Sept", 500_000, Money.Ugx),
            ("Reimbursement — office management 8 – 14 Sept", 552_000, Money.Ugx)
        };

        db.Requisitions.Add(new Requisition
        {
            OrganizationId = organizationId,
            Number = GeneralRequisitionNumber,
            Title = "General Requisition — 27 Aug to 14 Sept 2026",
            PeriodStart = new DateOnly(2026, 8, 27),
            PeriodEnd = new DateOnly(2026, 9, 14),
            Department = "Office administration",
            Purpose = "Loaded from the general requisition for 27 August to 14 September 2026.",
            Status = RequisitionStatus.Submitted,
            RequestedByName = "Office administration",
            SubmittedOn = DateTimeOffset.UtcNow,
            RequestedUgx = lines.Where(l => l.Currency == Money.Ugx).Sum(l => l.Amount),
            RequestedUsd = lines.Where(l => l.Currency == Money.Usd).Sum(l => l.Amount),
            Lines = lines.Select((line, index) => new RequisitionLine
            {
                LineNumber = index + 1,
                Description = line.Description,
                Quantity = 1,
                UnitAmount = line.Amount,
                Amount = line.Amount,
                Currency = line.Currency
            }).ToList()
        });
        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, Organization organization, string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                OrganizationId = organization.Id,
                MustChangePassword = false
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        await EnsureClaimAsync(userManager, user, "OrganizationId", organization.Id.ToString());
        await EnsureClaimAsync(userManager, user, "OrganizationCode", organization.Code);
        await EnsureClaimAsync(userManager, user, "FullName", user.FullName);
    }

    private static async Task EnsureClaimAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string type, string value)
    {
        var claims = await userManager.GetClaimsAsync(user);
        if (!claims.Any(c => c.Type == type && c.Value == value))
        {
            await userManager.AddClaimAsync(user, new Claim(type, value));
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName, IEnumerable<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new IdentityRole(roleName);
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        var existing = await roleManager.GetClaimsAsync(role);
        foreach (var permission in permissions.Where(AppPermissions.IsValid))
        {
            if (!existing.Any(c => c.Type == AppPermissions.ClaimType && c.Value == permission))
            {
                await roleManager.AddClaimAsync(role, new Claim(AppPermissions.ClaimType, permission));
            }
        }
    }
}
