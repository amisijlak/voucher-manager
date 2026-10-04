using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using VoucherMgt.DAL;
using VoucherMgt.Web.Authorization;
using VoucherMgt.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    static string Policy(string permission) => AppPermissions.ToPolicyName(permission);

    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AuthorizePage("/Index", Policy(AppPermissions.DashboardView));
    options.Conventions.AuthorizePage("/Requisitions/Index", Policy(AppPermissions.RequisitionsView));
    options.Conventions.AuthorizePage("/Requisitions/Edit", Policy(AppPermissions.RequisitionsCreate));
    options.Conventions.AuthorizePage("/Requisitions/Details", Policy(AppPermissions.RequisitionsView));
    options.Conventions.AuthorizePage("/Approvals/Index", Policy(AppPermissions.ApprovalsView));
    options.Conventions.AuthorizePage("/Accountability/Index", Policy(AppPermissions.AccountabilityView));
    options.Conventions.AuthorizePage("/Accountability/Details", Policy(AppPermissions.AccountabilityView));
    options.Conventions.AuthorizePage("/Accountability/Edit", Policy(AppPermissions.AccountabilitySubmit));
    options.Conventions.AuthorizePage("/Orders/Index", Policy(AppPermissions.OrdersView));
    options.Conventions.AuthorizePage("/Orders/Edit", Policy(AppPermissions.OrdersCreate));
    options.Conventions.AuthorizePage("/Orders/Details", Policy(AppPermissions.OrdersView));
    options.Conventions.AuthorizePage("/Orders/Print", Policy(AppPermissions.OrdersView));
    options.Conventions.AuthorizePage("/Invoices/Index", Policy(AppPermissions.InvoicesView));
    options.Conventions.AuthorizePage("/Invoices/Edit", Policy(AppPermissions.InvoicesCreate));
    options.Conventions.AuthorizePage("/Invoices/Details", Policy(AppPermissions.InvoicesView));
    options.Conventions.AuthorizePage("/Budgets/Index", Policy(AppPermissions.BudgetsView));
    options.Conventions.AuthorizePage("/Budgets/Details", Policy(AppPermissions.BudgetsView));
    options.Conventions.AuthorizePage("/Reports/Index", Policy(AppPermissions.ReportsView));
    options.Conventions.AuthorizePage("/Settings/Index", Policy(AppPermissions.SettingsManage));
    options.Conventions.AuthorizePage("/Admin/Organizations", Policy(AppPermissions.OrganizationsManage));
    options.Conventions.AuthorizePage("/Account/Roles", Policy(AppPermissions.RolesView));
    options.Conventions.AuthorizePage("/Account/UserRoles", Policy(AppPermissions.UserRolesManage));
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/License/Expired");
    options.Conventions.AllowAnonymousToPage("/Error");
});
builder.Services.RegisterServices(builder.Configuration);
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(AppPermissions.ToPolicyName(permission.Value), policy => policy.Requirements.Add(new PermissionRequirement(permission.Value)));
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VoucherMgtDbContext>();
    await db.Database.MigrateAsync();
    await OrganizationSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

var culture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = [culture],
    SupportedUICultures = [culture]
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<LicenseGuardMiddleware>();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
