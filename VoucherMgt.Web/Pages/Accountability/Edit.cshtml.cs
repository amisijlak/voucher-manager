using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.BLL.Models;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;

namespace VoucherMgt.Web.Pages.Accountability;

public class EditModel : PageModel
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".webp"];
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;
    private readonly IPettyCashService _pettyCash;
    private readonly IWebHostEnvironment _environment;

    public EditModel(IRepository repository, IOrganizationScope scope, IPettyCashService pettyCash, IWebHostEnvironment environment)
    {
        _repository = repository;
        _scope = scope;
        _pettyCash = pettyCash;
        _environment = environment;
    }

    public Requisition? Item { get; private set; }
    [BindProperty] public AccountabilityInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int requisitionId, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(requisitionId, cancellationToken))
        {
            return NotFound();
        }

        Input.Notes = Item!.Accountability?.Notes;
        Input.Lines = Item.Accountability?.Lines.OrderBy(l => l.LineNumber).Select(l => new AccountabilityLineInput
        {
            SpentOn = l.SpentOn,
            Description = l.Description,
            ReceiptNumber = l.ReceiptNumber,
            Amount = l.Amount,
            Currency = l.Currency,
            ExistingPath = l.ReceiptFilePath,
            ExistingName = l.ReceiptFileName
        }).ToList() ?? [];
        if (Input.Lines.Count == 0)
        {
            Input.Lines.Add(new AccountabilityLineInput { SpentOn = DateOnly.FromDateTime(DateTime.Today) });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int requisitionId, CancellationToken cancellationToken)
    {
        return await SaveAsync(requisitionId, submit: false, cancellationToken);
    }

    public async Task<IActionResult> OnPostSubmitAsync(int requisitionId, CancellationToken cancellationToken)
    {
        return await SaveAsync(requisitionId, submit: true, cancellationToken);
    }

    private async Task<IActionResult> SaveAsync(int requisitionId, bool submit, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        if (!await LoadAsync(requisitionId, cancellationToken))
        {
            return NotFound();
        }

        var root = ReceiptRoot(organization.Id);
        var drafts = new List<AccountabilityLineDraft>();
        foreach (var line in Input.Lines)
        {
            string? path = null;
            string? name = null;
            if (!string.IsNullOrWhiteSpace(line.ExistingPath))
            {
                var existing = Path.GetFullPath(line.ExistingPath);
                if (existing.StartsWith(root, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(existing))
                {
                    path = existing;
                    name = line.ExistingName;
                }
            }

            if (line.File is { Length: > 0 })
            {
                var saved = await SaveFileAsync(root, line.File);
                if (saved.Error is not null)
                {
                    TempData["Error"] = saved.Error;
                    return Page();
                }

                path = saved.Path;
                name = saved.Name;
            }

            drafts.Add(new AccountabilityLineDraft
            {
                SpentOn = line.SpentOn,
                Description = line.Description,
                ReceiptNumber = line.ReceiptNumber,
                Amount = line.Amount,
                Currency = line.Currency,
                ReceiptFilePath = path,
                ReceiptFileName = name
            });
        }

        var actor = PageAlerts.Actor(User);
        var savedResult = await _pettyCash.SaveAccountabilityAsync(requisitionId, new AccountabilityDraft
        {
            Notes = Input.Notes,
            Lines = drafts
        }, actor, cancellationToken);
        if (!savedResult.Ok)
        {
            PageAlerts.Set(this, savedResult);
            return Page();
        }

        if (submit)
        {
            PageAlerts.Set(this, await _pettyCash.SubmitAccountabilityAsync(requisitionId, actor, cancellationToken));
            return RedirectToPage("/Requisitions/Details", new { id = requisitionId });
        }

        PageAlerts.Set(this, savedResult);
        return RedirectToPage(new { requisitionId });
    }

    private async Task<bool> LoadAsync(int requisitionId, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        Item = await _repository.Set<Requisition>()
            .Include(r => r.Disbursement)
            .Include(r => r.Accountability)!.ThenInclude(a => a!.Lines)
            .FirstOrDefaultAsync(r => r.Id == requisitionId && r.OrganizationId == organization.Id, cancellationToken);
        return Item is not null;
    }

    private string ReceiptRoot(int organizationId)
    {
        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "receipts", organizationId.ToString()));
        Directory.CreateDirectory(root);
        return root;
    }

    private static async Task<(string? Path, string? Name, string? Error)> SaveFileAsync(string root, IFormFile file)
    {
        if (file.Length > 8 * 1024 * 1024)
        {
            return (null, null, "Each receipt must be 8 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return (null, null, "Receipts must be a PDF, JPG, PNG, or WEBP file.");
        }

        var stored = Path.Combine(root, $"{Guid.NewGuid():N}{extension}");
        await using var stream = System.IO.File.Create(stored);
        await file.CopyToAsync(stream);
        return (stored, Path.GetFileName(file.FileName), null);
    }

    public sealed class AccountabilityInput
    {
        public string? Notes { get; set; }
        public List<AccountabilityLineInput> Lines { get; set; } = [];
    }

    public sealed class AccountabilityLineInput
    {
        public DateOnly SpentOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string Description { get; set; } = string.Empty;
        public string? ReceiptNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = Money.Ugx;
        public string? ExistingPath { get; set; }
        public string? ExistingName { get; set; }
        public IFormFile? File { get; set; }
    }
}
