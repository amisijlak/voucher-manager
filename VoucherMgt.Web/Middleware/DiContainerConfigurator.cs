using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Core;
using VoucherMgt.DAL;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using VoucherMgt.DAL.Repositories;

namespace VoucherMgt.Web.Middleware;

public static class DiContainerConfigurator
{
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VoucherMgtDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString(ConnectionStringNames.DefaultConnection));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentOrganizationContext, CurrentOrganizationContext>();
        services.AddScoped<IRepository, VoucherMgtDbRepository>();
        services.AddScoped<IOrganizationScope, OrganizationScope>();
        services.AddScoped<IPettyCashService, PettyCashService>();
        services.AddScoped<IProcurementService, ProcurementService>();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.SignIn.RequireConfirmedAccount = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<VoucherMgtDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.HttpOnly = true;
            options.SlidingExpiration = true;
        });
    }
}
