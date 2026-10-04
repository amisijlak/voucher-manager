using Microsoft.AspNetCore.Mvc;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web;

public static class CompanyLogos
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    public static string Root(IWebHostEnvironment environment, int organizationId)
    {
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "logos", organizationId.ToString()));
        Directory.CreateDirectory(root);
        return root;
    }

    public static async Task<(string? Path, string? Name, string? Error)> SaveAsync(string root, IFormFile file)
    {
        if (file.Length > 4 * 1024 * 1024)
        {
            return (null, null, "The logo must be 4 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return (null, null, "The logo must be a PNG, JPG, or WEBP image.");
        }

        var stored = Path.Combine(root, $"{Guid.NewGuid():N}{extension}");
        await using var stream = File.Create(stored);
        await file.CopyToAsync(stream);
        return (stored, Path.GetFileName(file.FileName), null);
    }

    public static IActionResult Open(IWebHostEnvironment environment, TradingCompany company, int organizationId)
    {
        if (string.IsNullOrWhiteSpace(company.LogoFilePath) || !File.Exists(company.LogoFilePath))
        {
            return new NotFoundResult();
        }

        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "logos", organizationId.ToString()));
        var full = Path.GetFullPath(company.LogoFilePath);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return new NotFoundResult();
        }

        var stream = File.OpenRead(full);
        return new FileStreamResult(stream, ContentType(full));
    }

    public static void Delete(IWebHostEnvironment environment, TradingCompany company, int organizationId)
    {
        if (string.IsNullOrWhiteSpace(company.LogoFilePath))
        {
            return;
        }

        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "logos", organizationId.ToString()));
        var full = Path.GetFullPath(company.LogoFilePath);
        if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
        {
            File.Delete(full);
        }
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
