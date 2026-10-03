using VoucherMgt.BLL.Models;
using VoucherMgt.Common;
using VoucherMgt.DAL.Contracts;
using VoucherMgt.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace VoucherMgt.BLL;

public interface IPettyCashService
{
    Task<WorkflowResult> SaveRequisitionAsync(RequisitionDraft draft, Actor actor, CancellationToken cancellationToken = default);
    Task<WorkflowResult> SubmitRequisitionAsync(int id, Actor actor, CancellationToken cancellationToken = default);
    Task<WorkflowResult> DecideAsync(int id, DecisionDraft draft, Actor actor, CancellationToken cancellationToken = default);
    Task<WorkflowResult> DisburseAsync(int id, DisbursementDraft draft, Actor actor, CancellationToken cancellationToken = default);
    Task<WorkflowResult> SaveAccountabilityAsync(int requisitionId, AccountabilityDraft draft, Actor actor, CancellationToken cancellationToken = default);
    Task<WorkflowResult> SubmitAccountabilityAsync(int requisitionId, Actor actor, CancellationToken cancellationToken = default);
    Task<WorkflowResult> ReviewAccountabilityAsync(int requisitionId, bool approve, string? comments, Actor actor, CancellationToken cancellationToken = default);
    Task<WorkflowResult> CloseRequisitionAsync(int id, Actor actor, CancellationToken cancellationToken = default);
    Task<WorkflowResult> SaveBudgetPeriodAsync(BudgetPeriodDraft draft, CancellationToken cancellationToken = default);
    Task<DashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public sealed class PettyCashService : IPettyCashService
{
    private static readonly RequisitionStatus[] OpenStatuses =
    [
        RequisitionStatus.Draft,
        RequisitionStatus.Submitted,
        RequisitionStatus.Returned,
        RequisitionStatus.Approved,
        RequisitionStatus.Disbursed,
        RequisitionStatus.AccountabilitySubmitted,
        RequisitionStatus.AccountabilityReturned
    ];

    private static readonly RequisitionStatus[] CommittedStatuses =
    [
        RequisitionStatus.Approved,
        RequisitionStatus.Disbursed,
        RequisitionStatus.AccountabilitySubmitted,
        RequisitionStatus.AccountabilityReturned,
        RequisitionStatus.Accounted,
        RequisitionStatus.Closed
    ];

    private readonly IRepository _repository;
    private readonly IOrganizationScope _scope;

    public PettyCashService(IRepository repository, IOrganizationScope scope)
    {
        _repository = repository;
        _scope = scope;
    }

    public async Task<WorkflowResult> SaveRequisitionAsync(RequisitionDraft draft, Actor actor, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var lines = NormalizeLines(draft.Lines);
        if (lines.Count == 0)
        {
            return WorkflowResult.Fail("Add at least one item with a description and amount.");
        }

        if (string.IsNullOrWhiteSpace(draft.Title))
        {
            return WorkflowResult.Fail("Give the requisition a title.");
        }

        if (draft.PeriodEnd < draft.PeriodStart)
        {
            return WorkflowResult.Fail("The period end date must be on or after the start date.");
        }

        if (draft.BudgetPeriodId is int periodId)
        {
            var periodOk = await _repository.Set<BudgetPeriod>()
                .AnyAsync(p => p.Id == periodId && p.OrganizationId == organization.Id, cancellationToken);
            if (!periodOk)
            {
                return WorkflowResult.Fail("The selected budget period is not available.");
            }
        }

        foreach (var line in lines.Where(l => l.BudgetLineId is not null))
        {
            var budgetLine = await _repository.Set<BudgetLine>()
                .Include(l => l.BudgetPeriod)
                .FirstOrDefaultAsync(l => l.Id == line.BudgetLineId, cancellationToken);
            if (budgetLine is null || budgetLine.BudgetPeriod.OrganizationId != organization.Id || budgetLine.BudgetPeriodId != draft.BudgetPeriodId)
            {
                return WorkflowResult.Fail($"Budget line for \"{line.Description}\" does not belong to the selected budget.");
            }
        }

        Requisition requisition;
        if (draft.Id is int id)
        {
            requisition = await OwnedRequisition(id, organization.Id)
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Requisition was not found.");

            if (requisition.Status is not (RequisitionStatus.Draft or RequisitionStatus.Returned))
            {
                return WorkflowResult.Fail("Only a draft or a returned requisition can be edited.");
            }

            foreach (var existing in requisition.Lines.ToList())
            {
                _repository.Remove(existing);
            }

            requisition.Lines.Clear();
        }
        else
        {
            requisition = new Requisition
            {
                OrganizationId = organization.Id,
                Number = await NextNumberAsync(organization.Id, "REQ", cancellationToken),
                RequestedByUserId = actor.UserId,
                RequestedByName = actor.Name,
                Status = RequisitionStatus.Draft
            };
            _repository.Add(requisition);
        }

        requisition.Title = draft.Title.Trim();
        requisition.PeriodStart = draft.PeriodStart;
        requisition.PeriodEnd = draft.PeriodEnd;
        requisition.Department = Clean(draft.Department);
        requisition.Purpose = Clean(draft.Purpose);
        requisition.BudgetPeriodId = draft.BudgetPeriodId;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            requisition.Lines.Add(new RequisitionLine
            {
                LineNumber = index + 1,
                Description = line.Description.Trim(),
                Quantity = line.Quantity,
                UnitAmount = line.UnitAmount,
                Amount = line.Amount,
                Currency = line.Currency,
                BudgetLineId = line.BudgetLineId
            });
        }
        requisition.RequestedUgx = requisition.Lines.Where(l => l.Currency == Money.Ugx).Sum(l => l.Amount);
        requisition.RequestedUsd = requisition.Lines.Where(l => l.Currency == Money.Usd).Sum(l => l.Amount);

        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success("Requisition saved.", requisition.Id);
    }

    public async Task<WorkflowResult> SubmitRequisitionAsync(int id, Actor actor, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisition = await OwnedRequisition(id, organization.Id)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(cancellationToken);
        if (requisition is null)
        {
            return WorkflowResult.Fail("Requisition was not found.");
        }

        if (requisition.Status is not (RequisitionStatus.Draft or RequisitionStatus.Returned))
        {
            return WorkflowResult.Fail("This requisition cannot be submitted from its current status.");
        }

        if (requisition.Lines.Count == 0)
        {
            return WorkflowResult.Fail("Add the items that make up the total before submitting.");
        }

        requisition.Status = RequisitionStatus.Submitted;
        requisition.SubmittedOn = DateTimeOffset.UtcNow;
        requisition.DecisionNote = null;
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"{requisition.Number} was submitted for approval.", requisition.Id);
    }

    public async Task<WorkflowResult> DecideAsync(int id, DecisionDraft draft, Actor actor, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisition = await OwnedRequisition(id, organization.Id).FirstOrDefaultAsync(cancellationToken);
        if (requisition is null)
        {
            return WorkflowResult.Fail("Requisition was not found.");
        }

        if (requisition.Status != RequisitionStatus.Submitted)
        {
            return WorkflowResult.Fail("Only a submitted requisition can be approved, returned, or rejected.");
        }

        var comments = Clean(draft.Comments);
        if (draft.Action != ApprovalAction.Approve && string.IsNullOrWhiteSpace(comments))
        {
            return WorkflowResult.Fail("A comment is required when returning or rejecting a requisition.");
        }

        decimal approvedUgx = 0;
        decimal approvedUsd = 0;
        if (draft.Action == ApprovalAction.Approve)
        {
            approvedUgx = Money.Round(draft.ApprovedUgx, Money.Ugx);
            approvedUsd = Money.Round(draft.ApprovedUsd, Money.Usd);
            if (approvedUgx < 0 || approvedUsd < 0 || approvedUgx > requisition.RequestedUgx || approvedUsd > requisition.RequestedUsd)
            {
                return WorkflowResult.Fail("The approved amount cannot be negative or higher than the amount requested.");
            }

            if (approvedUgx == 0 && approvedUsd == 0)
            {
                return WorkflowResult.Fail("Approve at least one currency amount, or reject the requisition.");
            }

            var reduced = approvedUgx != requisition.RequestedUgx || approvedUsd != requisition.RequestedUsd;
            if (reduced && string.IsNullOrWhiteSpace(comments))
            {
                return WorkflowResult.Fail("Explain why the approved amount is lower than the amount requested.");
            }
        }

        requisition.Decisions.Add(new ApprovalDecision
        {
            Action = draft.Action,
            ApprovedUgx = approvedUgx,
            ApprovedUsd = approvedUsd,
            Comments = comments,
            DecidedByUserId = actor.UserId,
            DecidedByName = actor.Name
        });
        requisition.DecisionNote = comments;
        requisition.Status = draft.Action switch
        {
            ApprovalAction.Approve => RequisitionStatus.Approved,
            ApprovalAction.Return => RequisitionStatus.Returned,
            _ => RequisitionStatus.Rejected
        };
        requisition.ApprovedUgx = approvedUgx;
        requisition.ApprovedUsd = approvedUsd;

        await _repository.SaveChangesAsync(cancellationToken);
        var verb = draft.Action switch
        {
            ApprovalAction.Approve => "approved",
            ApprovalAction.Return => "returned",
            _ => "rejected"
        };
        return WorkflowResult.Success($"{requisition.Number} was {verb}.", requisition.Id);
    }

    public async Task<WorkflowResult> DisburseAsync(int id, DisbursementDraft draft, Actor actor, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisition = await OwnedRequisition(id, organization.Id)
            .Include(r => r.Disbursement)
            .FirstOrDefaultAsync(cancellationToken);
        if (requisition is null)
        {
            return WorkflowResult.Fail("Requisition was not found.");
        }

        if (requisition.Status != RequisitionStatus.Approved || requisition.Disbursement is not null)
        {
            return WorkflowResult.Fail("Funds can be issued only after approval, and only once.");
        }

        var ugx = Money.Round(draft.AmountUgx, Money.Ugx);
        var usd = Money.Round(draft.AmountUsd, Money.Usd);
        if (ugx < 0 || usd < 0 || ugx > requisition.ApprovedUgx || usd > requisition.ApprovedUsd || (ugx == 0 && usd == 0))
        {
            return WorkflowResult.Fail("The amount issued must be greater than zero and cannot exceed the approved amount.");
        }

        if (string.IsNullOrWhiteSpace(draft.Method))
        {
            return WorkflowResult.Fail("Choose how the money was issued.");
        }

        requisition.Disbursement = new Disbursement
        {
            AmountUgx = ugx,
            AmountUsd = usd,
            Method = draft.Method.Trim(),
            Reference = Clean(draft.Reference),
            PaidOn = draft.PaidOn,
            PaidByUserId = actor.UserId,
            PaidByName = actor.Name,
            Notes = Clean(draft.Notes)
        };
        requisition.Accountability = new Accountability { Status = AccountabilityStatus.Draft };
        requisition.Status = RequisitionStatus.Disbursed;
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"Funds for {requisition.Number} were recorded. Accountability is now open.", requisition.Id);
    }

    public async Task<WorkflowResult> SaveAccountabilityAsync(int requisitionId, AccountabilityDraft draft, Actor actor, CancellationToken cancellationToken = default)
    {
        var accountability = await LoadEditableAccountability(requisitionId, cancellationToken);
        if (accountability is null)
        {
            return WorkflowResult.Fail("Accountability can be edited only after funds are issued, and before it is approved.");
        }

        var lines = NormalizeAccountability(draft.Lines);
        if (lines.Count == 0)
        {
            return WorkflowResult.Fail("Add the receipts or items that show how the money was used.");
        }

        foreach (var existing in accountability.Lines.ToList())
        {
            _repository.Remove(existing);
        }

        accountability.Lines.Clear();
        accountability.Notes = Clean(draft.Notes);
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            accountability.Lines.Add(new AccountabilityLine
            {
                LineNumber = index + 1,
                SpentOn = line.SpentOn,
                Description = line.Description.Trim(),
                ReceiptNumber = Clean(line.ReceiptNumber),
                Amount = Money.Round(line.Amount, line.Currency),
                Currency = Money.Normalize(line.Currency),
                ReceiptFilePath = line.ReceiptFilePath,
                ReceiptFileName = line.ReceiptFileName
            });
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success("Accountability saved.", requisitionId);
    }

    public async Task<WorkflowResult> SubmitAccountabilityAsync(int requisitionId, Actor actor, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisition = await OwnedRequisition(requisitionId, organization.Id)
            .Include(r => r.Accountability)!.ThenInclude(a => a!.Lines)
            .Include(r => r.Disbursement)
            .FirstOrDefaultAsync(cancellationToken);
        if (requisition?.Accountability is null || requisition.Disbursement is null)
        {
            return WorkflowResult.Fail("There is no open accountability for this requisition.");
        }

        if (requisition.Accountability.Status is not (AccountabilityStatus.Draft or AccountabilityStatus.Returned))
        {
            return WorkflowResult.Fail("This accountability has already been submitted.");
        }

        if (requisition.Accountability.Lines.Count == 0)
        {
            return WorkflowResult.Fail("Add receipt lines before submitting accountability.");
        }

        requisition.Accountability.Status = AccountabilityStatus.Submitted;
        requisition.Accountability.SubmittedOn = DateTimeOffset.UtcNow;
        requisition.Accountability.SubmittedByName = actor.Name;
        requisition.Status = RequisitionStatus.AccountabilitySubmitted;
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"{requisition.Number} accountability was submitted for review.", requisition.Id);
    }

    public async Task<WorkflowResult> ReviewAccountabilityAsync(int requisitionId, bool approve, string? comments, Actor actor, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisition = await OwnedRequisition(requisitionId, organization.Id)
            .Include(r => r.Accountability)!.ThenInclude(a => a!.Lines)
            .Include(r => r.Disbursement)
            .FirstOrDefaultAsync(cancellationToken);
        if (requisition?.Accountability is null || requisition.Disbursement is null || requisition.Accountability.Status != AccountabilityStatus.Submitted)
        {
            return WorkflowResult.Fail("Only a submitted accountability can be reviewed.");
        }

        var spentUgx = requisition.Accountability.Lines.Where(l => l.Currency == Money.Ugx).Sum(l => l.Amount);
        var spentUsd = requisition.Accountability.Lines.Where(l => l.Currency == Money.Usd).Sum(l => l.Amount);
        var variance = spentUgx != requisition.Disbursement.AmountUgx || spentUsd != requisition.Disbursement.AmountUsd;
        if ((variance || !approve) && string.IsNullOrWhiteSpace(comments))
        {
            return WorkflowResult.Fail(approve
                ? "The receipts do not match the amount issued. Explain how the difference is being cleared."
                : "Explain what must be corrected before accountability can be accepted.");
        }

        requisition.Accountability.ReviewComments = Clean(comments);
        if (approve)
        {
            requisition.Accountability.Status = AccountabilityStatus.Approved;
            requisition.Status = RequisitionStatus.Accounted;
        }
        else
        {
            requisition.Accountability.Status = AccountabilityStatus.Returned;
            requisition.Status = RequisitionStatus.AccountabilityReturned;
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success(approve
            ? $"{requisition.Number} is fully accounted."
            : $"{requisition.Number} accountability was returned.", requisition.Id);
    }

    public async Task<WorkflowResult> CloseRequisitionAsync(int id, Actor actor, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisition = await OwnedRequisition(id, organization.Id).FirstOrDefaultAsync(cancellationToken);
        if (requisition is null || requisition.Status != RequisitionStatus.Accounted)
        {
            return WorkflowResult.Fail("A requisition can be closed only after accountability is accepted.");
        }

        requisition.Status = RequisitionStatus.Closed;
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success($"{requisition.Number} was closed.", requisition.Id);
    }

    public async Task<WorkflowResult> SaveBudgetPeriodAsync(BudgetPeriodDraft draft, CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            return WorkflowResult.Fail("Name the budget period.");
        }

        if (draft.EndsOn < draft.StartsOn)
        {
            return WorkflowResult.Fail("The budget end date must be on or after the start date.");
        }

        var lines = draft.Lines
            .Where(l => !string.IsNullOrWhiteSpace(l.Description) && l.Amount > 0)
            .ToList();
        if (lines.Count == 0)
        {
            return WorkflowResult.Fail("Add at least one budget line.");
        }

        var period = new BudgetPeriod
        {
            OrganizationId = organization.Id,
            Name = draft.Name.Trim(),
            StartsOn = draft.StartsOn,
            EndsOn = draft.EndsOn,
            Notes = Clean(draft.Notes),
            Lines = lines.Select((line, index) => new BudgetLine
            {
                LineNumber = index + 1,
                Description = line.Description.Trim(),
                Amount = Money.Round(line.Amount, line.Currency),
                Currency = Money.Normalize(line.Currency),
                CategoryId = line.CategoryId,
                TradingCompanyId = line.TradingCompanyId
            }).ToList()
        };
        _repository.Add(period);
        await _repository.SaveChangesAsync(cancellationToken);
        return WorkflowResult.Success("Budget period saved.", period.Id);
    }

    public async Task<DashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisitions = _repository.Set<Requisition>().Where(r => r.OrganizationId == organization.Id);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var budget = await _repository.Set<BudgetPeriod>()
            .Include(p => p.Lines)
            .Where(p => p.OrganizationId == organization.Id && p.StartsOn <= today && p.EndsOn >= today)
            .OrderByDescending(p => p.StartsOn)
            .FirstOrDefaultAsync(cancellationToken);

        var committed = await _repository.Set<RequisitionLine>()
            .Where(l => l.BudgetLine!.BudgetPeriodId == (budget == null ? 0 : budget.Id)
                && CommittedStatuses.Contains(l.Requisition.Status)
                && l.Currency == Money.Ugx)
            .SumAsync(l => (decimal?)l.Amount, cancellationToken) ?? 0;

        var openAccountability = await requisitions
            .Where(r => r.Status == RequisitionStatus.Disbursed
                || r.Status == RequisitionStatus.AccountabilitySubmitted
                || r.Status == RequisitionStatus.AccountabilityReturned)
            .Select(r => new
            {
                r.Disbursement!.AmountUgx,
                r.Disbursement.AmountUsd,
                SpentUgx = r.Accountability!.Lines.Where(l => l.Currency == Money.Ugx).Sum(l => (decimal?)l.Amount) ?? 0,
                SpentUsd = r.Accountability.Lines.Where(l => l.Currency == Money.Usd).Sum(l => (decimal?)l.Amount) ?? 0
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummary
        {
            OpenRequisitions = await requisitions.CountAsync(r => OpenStatuses.Contains(r.Status), cancellationToken),
            AwaitingApproval = await requisitions.CountAsync(r => r.Status == RequisitionStatus.Submitted, cancellationToken),
            AwaitingAccountability = await requisitions.CountAsync(r =>
                r.Status == RequisitionStatus.Disbursed
                || r.Status == RequisitionStatus.AccountabilitySubmitted
                || r.Status == RequisitionStatus.AccountabilityReturned, cancellationToken),
            OpenOrders = await _repository.Set<OrderForm>().CountAsync(o =>
                o.OrganizationId == organization.Id
                && o.Status != OrderStatus.Cancelled
                && o.Status != OrderStatus.Closed, cancellationToken),
            UnpaidInvoices = await _repository.Set<Invoice>().CountAsync(i =>
                i.OrganizationId == organization.Id
                && i.Status != InvoiceStatus.Paid
                && i.Status != InvoiceStatus.Cancelled
                && i.Status != InvoiceStatus.Draft, cancellationToken),
            UnaccountedUgx = openAccountability.Sum(r => r.AmountUgx - r.SpentUgx),
            UnaccountedUsd = openAccountability.Sum(r => r.AmountUsd - r.SpentUsd),
            BudgetUgx = budget?.Lines.Where(l => l.Currency == Money.Ugx).Sum(l => l.Amount) ?? 0,
            BudgetCommittedUgx = committed,
            ActiveBudgetName = budget?.Name
        };
    }

    private async Task<Accountability?> LoadEditableAccountability(int requisitionId, CancellationToken cancellationToken)
    {
        var organization = await _scope.GetRequiredAsync(cancellationToken);
        var requisition = await OwnedRequisition(requisitionId, organization.Id)
            .Include(r => r.Accountability)!.ThenInclude(a => a!.Lines)
            .FirstOrDefaultAsync(cancellationToken);
        if (requisition?.Accountability is null)
        {
            return null;
        }

        if (requisition.Status is not (RequisitionStatus.Disbursed or RequisitionStatus.AccountabilityReturned))
        {
            return null;
        }

        return requisition.Accountability.Status is AccountabilityStatus.Draft or AccountabilityStatus.Returned
            ? requisition.Accountability
            : null;
    }

    private IQueryable<Requisition> OwnedRequisition(int id, int organizationId) =>
        _repository.Set<Requisition>().Where(r => r.Id == id && r.OrganizationId == organizationId);

    private async Task<string> NextNumberAsync(int organizationId, string prefix, CancellationToken cancellationToken)
    {
        var head = $"{prefix}-{DateTime.Today.Year}-";
        var numbers = await _repository.Set<Requisition>()
            .Where(r => r.OrganizationId == organizationId && r.Number.StartsWith(head))
            .Select(r => r.Number)
            .ToListAsync(cancellationToken);
        return $"{head}{NextSequence(numbers, head):0000}";
    }

    internal static int NextSequence(IEnumerable<string> numbers, string head)
    {
        var max = 0;
        foreach (var number in numbers)
        {
            if (number.StartsWith(head) && int.TryParse(number[head.Length..], out var sequence))
            {
                max = Math.Max(max, sequence);
            }
        }

        return max + 1;
    }

    private static List<(string Description, decimal Quantity, decimal UnitAmount, decimal Amount, string Currency, int? BudgetLineId)> NormalizeLines(IEnumerable<MoneyLineDraft> drafts)
    {
        var lines = new List<(string, decimal, decimal, decimal, string, int?)>();
        foreach (var draft in drafts)
        {
            if (string.IsNullOrWhiteSpace(draft.Description))
            {
                continue;
            }

            var currency = Money.Normalize(draft.Currency);
            var quantity = draft.Quantity <= 0 ? 1 : draft.Quantity;
            var unit = Money.Round(draft.UnitAmount, currency);
            var amount = Money.Round(quantity * unit, currency);
            if (amount <= 0)
            {
                continue;
            }

            lines.Add((draft.Description, quantity, unit, amount, currency, draft.BudgetLineId));
        }

        return lines;
    }

    private static List<AccountabilityLineDraft> NormalizeAccountability(IEnumerable<AccountabilityLineDraft> drafts) =>
        drafts.Where(l => !string.IsNullOrWhiteSpace(l.Description) && l.Amount > 0).ToList();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
