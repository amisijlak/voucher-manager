namespace VoucherMgt.Common;

public enum RequisitionStatus
{
    Draft = 0,
    Submitted = 1,
    Returned = 2,
    Rejected = 3,
    Approved = 4,
    Disbursed = 5,
    AccountabilitySubmitted = 6,
    AccountabilityReturned = 7,
    Accounted = 8,
    Closed = 9
}

public enum AccountabilityStatus
{
    Draft = 0,
    Submitted = 1,
    Returned = 2,
    Approved = 3
}

public enum OrderKind
{
    General = 0,
    Advertising = 1
}

public enum OrderDirection
{
    Purchase = 0,
    ClientOrder = 1
}

public enum OrderStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyInvoiced = 2,
    Invoiced = 3,
    Closed = 4,
    Cancelled = 5
}

public enum InvoiceDirection
{
    Payable = 0,
    Receivable = 1
}

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Approved = 2,
    Paid = 3,
    Cancelled = 4
}

public enum VatMode
{
    Exclusive = 0,
    Inclusive = 1,
    Exempt = 2
}

public enum ApprovalAction
{
    Approve = 0,
    Reject = 1,
    Return = 2
}
