using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VoucherMgt.DAL;

public class VoucherMgtDbContextFactory : IDesignTimeDbContextFactory<VoucherMgtDbContext>
{
    public VoucherMgtDbContext CreateDbContext(string[] args)
    {
        var webPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "VoucherMgt.Web"));
        if (!Directory.Exists(webPath))
        {
            webPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "VoucherMgt.Web"));
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<VoucherMgtDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString(ConnectionStringNames.DefaultConnection));
        return new VoucherMgtDbContext(optionsBuilder.Options);
    }
}
