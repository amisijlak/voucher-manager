namespace VoucherMgt.Web.Authorization;

public static class AppPermissions
{
    public const string ClaimType = "permission";
    public const string PolicyPrefix = "Permission:";

    public const string DashboardView = "Dashboard.View";
    public const string RequisitionsView = "Requisitions.View";
    public const string RequisitionsCreate = "Requisitions.Create";
    public const string RequisitionsSubmit = "Requisitions.Submit";
    public const string ApprovalsView = "Approvals.View";
    public const string ApprovalsDecide = "Approvals.Decide";
    public const string DisbursementsRecord = "Disbursements.Record";
    public const string AccountabilityView = "Accountability.View";
    public const string AccountabilitySubmit = "Accountability.Submit";
    public const string AccountabilityReview = "Accountability.Review";
    public const string OrdersView = "Orders.View";
    public const string OrdersCreate = "Orders.Create";
    public const string OrdersIssue = "Orders.Issue";
    public const string InvoicesView = "Invoices.View";
    public const string InvoicesCreate = "Invoices.Create";
    public const string InvoicesApprove = "Invoices.Approve";
    public const string InvoicesPay = "Invoices.Pay";
    public const string BudgetsView = "Budgets.View";
    public const string BudgetsManage = "Budgets.Manage";
    public const string ReportsView = "Reports.View";
    public const string SettingsManage = "Settings.Manage";
    public const string OrganizationsManage = "Organizations.Manage";
    public const string OrganizationsCreate = "Organizations.Create";
    public const string LicensesManage = "Licenses.Manage";
    public const string UsersManage = "Users.Manage";
    public const string UsersCreate = "Users.Create";
    public const string UserRolesManage = "UserRoles.Manage";
    public const string RolesView = "Roles.View";
    public const string RolesCreate = "Roles.Create";
    public const string RolesEdit = "Roles.Edit";

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(DashboardView, "Dashboard", "View dashboard"),
        new(RequisitionsView, "Petty cash", "View requisitions"),
        new(RequisitionsCreate, "Petty cash", "Create and edit requisitions"),
        new(RequisitionsSubmit, "Petty cash", "Submit requisitions"),
        new(ApprovalsView, "Petty cash", "View the approval queue"),
        new(ApprovalsDecide, "Petty cash", "Approve, return, or reject requisitions"),
        new(DisbursementsRecord, "Petty cash", "Record funds issued"),
        new(AccountabilityView, "Petty cash", "View accountability"),
        new(AccountabilitySubmit, "Petty cash", "Submit receipt accountability"),
        new(AccountabilityReview, "Petty cash", "Accept or return accountability"),
        new(OrdersView, "Procurement", "View order forms"),
        new(OrdersCreate, "Procurement", "Create order forms and LPOs"),
        new(OrdersIssue, "Procurement", "Issue or cancel order forms"),
        new(InvoicesView, "Procurement", "View invoices"),
        new(InvoicesCreate, "Procurement", "Create invoices"),
        new(InvoicesApprove, "Procurement", "Approve invoices"),
        new(InvoicesPay, "Procurement", "Record invoice payments"),
        new(BudgetsView, "Finance", "View budgets"),
        new(BudgetsManage, "Finance", "Create budget periods"),
        new(ReportsView, "Finance", "View reports"),
        new(SettingsManage, "Administration", "Manage voucher settings"),
        new(OrganizationsManage, "Administration", "Manage organizations and licenses"),
        new(OrganizationsCreate, "Administration", "Create organizations"),
        new(LicensesManage, "Administration", "Create or renew organization licenses"),
        new(UsersManage, "Administration", "Manage organization users"),
        new(UsersCreate, "Administration", "Create organization users"),
        new(UserRolesManage, "Administration", "Assign user roles"),
        new(RolesView, "Administration", "View roles and permissions"),
        new(RolesCreate, "Administration", "Create roles"),
        new(RolesEdit, "Administration", "Edit role permissions")
    ];

    public static bool IsValid(string permission) => All.Any(p => p.Value == permission);

    public static string ToPolicyName(string permission) => $"{PolicyPrefix}{permission}";
}

public sealed record PermissionDefinition(string Value, string Group, string DisplayName);
