using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VoucherMgt.BLL;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;

namespace VoucherMgt.Web.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public IndexModel(IRepository repository, IOrganizationScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    public decimal RequestedUgx { get; private set; }
    public decimal RequestedUsd { get; private set; }
    public decimal ApprovedUgx { get; private set; }
    public decimal ApprovedUsd { get; private set; }
    public decimal IssuedUgx { get; private set; }
    public decimal IssuedUsd { get; private set; }
    public decimal UnpaidUgx { get; private set; }
    public decimal UnpaidUsd { get; private set; }
    public IReadOnlyList<StatusRow> ByStatus { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisitions = await _repository.Set<DAL.Entities.Requisition>()
            .Where(r => r.OrganizationId == organization.Id)
            .Select(r => new { r.Status, r.RequestedUgx, r.RequestedUsd, r.ApprovedUgx, r.ApprovedUsd })
            .ToListAsync(cancellationToken);
        RequestedUgx = requisitions.Sum(r => r.RequestedUgx);
        RequestedUsd = requisitions.Sum(r => r.RequestedUsd);
        ApprovedUgx = requisitions.Sum(r => r.ApprovedUgx);
        ApprovedUsd = requisitions.Sum(r => r.ApprovedUsd);
        var issued = await _repository.Set<DAL.Entities.Disbursement>()
            .Where(d => d.Requisition.OrganizationId == organization.Id)
            .GroupBy(_ => 1)
            .Select(g => new { Ugx = g.Sum(x => x.AmountUgx), Usd = g.Sum(x => x.AmountUsd) })
            .FirstOrDefaultAsync(cancellationToken);
        IssuedUgx = issued?.Ugx ?? 0;
        IssuedUsd = issued?.Usd ?? 0;
        var unpaid = await _repository.Set<DAL.Entities.Invoice>()
            .Where(i => i.OrganizationId == organization.Id && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
            .GroupBy(_ => 1)
            .Select(g => new { Ugx = g.Sum(x => x.GrossUgx - x.PaidUgx), Usd = g.Sum(x => x.GrossUsd - x.PaidUsd) })
            .FirstOrDefaultAsync(cancellationToken);
        UnpaidUgx = unpaid?.Ugx ?? 0;
        UnpaidUsd = unpaid?.Usd ?? 0;
        ByStatus = requisitions
            .GroupBy(r => r.Status)
            .Select(g => new StatusRow(g.Key, g.Count(), g.Sum(x => x.RequestedUgx), g.Sum(x => x.RequestedUsd)))
            .OrderBy(r => r.Status)
            .ToList();
    }

    public sealed record StatusRow(RequisitionStatus Status, int Count, decimal Ugx, decimal Usd);
}
