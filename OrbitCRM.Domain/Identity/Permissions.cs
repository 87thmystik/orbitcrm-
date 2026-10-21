namespace OrbitCRM.Domain.Identity;

public static class Permissions
{
    public static class Customers
    {
        public const string View = "Customers.View";
        public const string Create = "Customers.Create";
        public const string Edit = "Customers.Edit";
        public const string Delete = "Customers.Delete";
    }

    public static class Leads
    {
        public const string View = "Leads.View";
        public const string Create = "Leads.Create";
        public const string Edit = "Leads.Edit";
        public const string Delete = "Leads.Delete";
        public const string Convert = "Leads.Convert";
    }

    public static class Deals
    {
        public const string View = "Deals.View";
        public const string Create = "Deals.Create";
        public const string Edit = "Deals.Edit";
        public const string Delete = "Deals.Delete";
    }

    public static class Tasks
    {
        public const string View = "Tasks.View";
        public const string Create = "Tasks.Create";
        public const string Edit = "Tasks.Edit";
        public const string Delete = "Tasks.Delete";
    }

    public static class Activities
    {
        public const string View = "Activities.View";
        public const string Create = "Activities.Create";
        public const string Delete = "Activities.Delete";
    }

    public static class Reports
    {
        public const string View = "Reports.View";
    }

    public static class Users
    {
        public const string Manage = "Users.Manage";
    }

    public static class Tenant
    {
        public const string Manage = "Tenant.Manage";
    }

    public static IEnumerable<string> All()
    {
        yield return Customers.View; yield return Customers.Create;
        yield return Customers.Edit; yield return Customers.Delete;
        yield return Leads.View; yield return Leads.Create;
        yield return Leads.Edit; yield return Leads.Delete; yield return Leads.Convert;
        yield return Deals.View; yield return Deals.Create;
        yield return Deals.Edit; yield return Deals.Delete;
        yield return Tasks.View; yield return Tasks.Create;
        yield return Tasks.Edit; yield return Tasks.Delete;
        yield return Activities.View; yield return Activities.Create; yield return Activities.Delete;
        yield return Reports.View;
        yield return Users.Manage;
        yield return Tenant.Manage;
    }
}