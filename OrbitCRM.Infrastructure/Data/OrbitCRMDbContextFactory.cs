using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OrbitCRM.Infrastructure.Identity;

namespace OrbitCRM.Infrastructure.Data;

public class OrbitCRMDbContextFactory : IDesignTimeDbContextFactory<OrbitCRMDbContext>
{
    public OrbitCRMDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ORBITCRM_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=Orbitcrm;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<OrbitCRMDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(OrbitCRMDbContext).Assembly.FullName));

        var httpAccessor = new HttpContextAccessor();
        var currentTenant = new CurrentTenant(httpAccessor);
        var currentUser = new CurrentUser(httpAccessor);

        return new OrbitCRMDbContext(optionsBuilder.Options, currentTenant, currentUser);
    }
}