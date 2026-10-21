namespace OrbitCRM.Domain.Identity;

public static class Roles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesRep = "SalesRep";
    public const string Support = "Support";

    public static IEnumerable<string> All()
    {
        yield return Owner;
        yield return Admin;
        yield return Manager;
        yield return SalesRep;
        yield return Support;
    }
}