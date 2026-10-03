using VoucherMgt.Common;

namespace VoucherMgt.Web;

public static class StatusText
{
    public static string Label(RequisitionStatus status) => status switch
    {
        RequisitionStatus.AccountabilitySubmitted => "Accountability submitted",
        RequisitionStatus.AccountabilityReturned => "Accountability returned",
        _ => status.ToString()
    };

    public static string Badge(RequisitionStatus status) => status switch
    {
        RequisitionStatus.Submitted or RequisitionStatus.AccountabilitySubmitted => "bg-info",
        RequisitionStatus.Approved or RequisitionStatus.Disbursed => "bg-primary",
        RequisitionStatus.Returned or RequisitionStatus.AccountabilityReturned => "bg-warning",
        RequisitionStatus.Rejected => "bg-danger",
        RequisitionStatus.Accounted or RequisitionStatus.Closed => "bg-success",
        _ => "bg-secondary"
    };

    public static string Badge(OrderStatus status) => status switch
    {
        OrderStatus.Issued => "bg-primary",
        OrderStatus.PartiallyInvoiced => "bg-warning",
        OrderStatus.Invoiced or OrderStatus.Closed => "bg-success",
        OrderStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };

    public static string Badge(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Issued => "bg-info",
        InvoiceStatus.Approved => "bg-primary",
        InvoiceStatus.Paid => "bg-success",
        InvoiceStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };
}
