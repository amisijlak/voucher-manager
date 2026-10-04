using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Accountability;

public class DetailsModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;
    private readonly IWebHostEnvironment _environment;

    public DetailsModel(IRepository repository, IOrganizationScope scope, IWebHostEnvironment environment)
    {
        _repository = repository;
        _scope = scope;
        _environment = environment;
    }

    public Requisition? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(int requisitionId, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(requisitionId, cancellationToken) || Item!.Disbursement is null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnGetProofAsync(int requisitionId, int lineId, bool download, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var line = await _repository.Set<AccountabilityLine>()
            .Include(l => l.Accountability)
            .ThenInclude(a => a.Requisition)
            .FirstOrDefaultAsync(l => l.Id == lineId
                && l.Accountability.RequisitionId == requisitionId
                && l.Accountability.Requisition.OrganizationId == organization.Id, cancellationToken);
        if (line?.ReceiptFilePath is null || !System.IO.File.Exists(line.ReceiptFilePath))
        {
            return NotFound();
        }

        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "receipts", organization.Id.ToString()));
        var full = Path.GetFullPath(line.ReceiptFilePath);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var stream = System.IO.File.OpenRead(full);
        var result = new FileStreamResult(stream, ContentType(full));
        if (download)
        {
            result.FileDownloadName = line.ReceiptFileName ?? Path.GetFileName(full);
        }

        return result;
    }

    private async Task<bool> LoadAsync(int requisitionId, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Item = await _repository.Set<Requisition>()
            .Include(r => r.Lines)
            .Include(r => r.Disbursement)
            .Include(r => r.Accountability)!.ThenInclude(a => a!.Lines)
            .FirstOrDefaultAsync(r => r.Id == requisitionId && r.OrganizationId == organization.Id, cancellationToken);
        return Item is not null;
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
