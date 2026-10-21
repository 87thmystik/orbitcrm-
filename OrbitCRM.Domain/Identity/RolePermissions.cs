namespace OrbitCRM.Domain.Identity;

public static class RolePermissions
{
    private static readonly Dictionary<string, HashSet<string>> Map = new()
    {
        [Roles.Owner] = new(Permissions.All()),

        [Roles.Admin] = new()
        {
            Permissions.Customers.View, Permissions.Customers.Create,
            Permissions.Customers.Edit, Permissions.Customers.Delete,

            Permissions.Leads.View, Permissions.Leads.Create,
            Permissions.Leads.Edit, Permissions.Leads.Delete, Permissions.Leads.Convert,

            Permissions.Deals.View, Permissions.Deals.Create,
            Permissions.Deals.Edit, Permissions.Deals.Delete,

            Permissions.Tasks.View, Permissions.Tasks.Create,
            Permissions.Tasks.Edit, Permissions.Tasks.Delete,

            Permissions.Activities.View, Permissions.Activities.Create, Permissions.Activities.Delete,

            Permissions.Reports.View,
            Permissions.Users.Manage
        },

        [Roles.Manager] = new()
        {
            Permissions.Customers.View, Permissions.Customers.Create,
            Permissions.Customers.Edit, Permissions.Customers.Delete,

            Permissions.Leads.View, Permissions.Leads.Create,
            Permissions.Leads.Edit, Permissions.Leads.Delete, Permissions.Leads.Convert,

            Permissions.Deals.View, Permissions.Deals.Create,
            Permissions.Deals.Edit, Permissions.Deals.Delete,

            Permissions.Tasks.View, Permissions.Tasks.Create,
            Permissions.Tasks.Edit, Permissions.Tasks.Delete,

            Permissions.Activities.View, Permissions.Activities.Create, Permissions.Activities.Delete,

            Permissions.Reports.View
        },

        [Roles.SalesRep] = new()
        {
            Permissions.Customers.View, Permissions.Customers.Create, Permissions.Customers.Edit,

            Permissions.Leads.View, Permissions.Leads.Create,
            Permissions.Leads.Edit, Permissions.Leads.Convert,

            Permissions.Deals.View, Permissions.Deals.Create, Permissions.Deals.Edit,

            Permissions.Tasks.View, Permissions.Tasks.Create, Permissions.Tasks.Edit,

            Permissions.Activities.View, Permissions.Activities.Create,

            Permissions.Reports.View
        },

        [Roles.Support] = new()
        {
            Permissions.Customers.View,
            Permissions.Leads.View,
            Permissions.Deals.View,
            Permissions.Tasks.View,
            Permissions.Activities.View,
            Permissions.Activities.Create
        }
    };

    public static IEnumerable<string> GetPermissionsForRole(string role) =>
        Map.TryGetValue(role, out var set) ? set : Enumerable.Empty<string>();

    public static IEnumerable<string> GetPermissionsForRoles(IEnumerable<string> roles) =>
        roles.SelectMany(GetPermissionsForRole).Distinct();
}