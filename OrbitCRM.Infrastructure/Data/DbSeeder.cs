using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Domain.Identity;

namespace OrbitCRM.Infrastructure.Data;

public static class DbSeeder
{
    // ... existing SeedAsync ...

    public static async Task SeedDemoUserAsync(
        UserManager<ApplicationUser> userManager,
        OrbitCRMDbContext db)
    {
        const string email = "admin@orbitcrm.local";
        const string password = "Admin123!";

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return;

        var tenant = await db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync();
        if (tenant is null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Orbit",
            LastName = "Admin",
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to seed demo user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // DEBUG: verify the password was hashed correctly
        var passwordCheck = await userManager.CheckPasswordAsync(user, password);
        Console.WriteLine($"[SEED] User created. Password check = {passwordCheck}");

        db.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = Roles.Owner,
            IsActive = true
        });

        await db.SaveChangesAsync();
    }
    public static async Task SeedAsync(OrbitCRMDbContext db)
    {
        // Apply any pending migrations automatically in dev
        await db.Database.MigrateAsync();

        // Create a demo tenant if none exists
        if (!await db.Tenants.AnyAsync())
        {
            var tenant = new Tenant
            {
                Name = "OrbitCRM Demo",
                Slug = "demo",
                Email = "demo@orbitcrm.local",
                IsActive = true
            };

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            // Default pipeline stages
            var stages = new List<PipelineStage>
            {
                new() { TenantId = tenant.Id, Name = "New",         DisplayOrder = 1, Probability = 10,  Color = "#6b7280" },
                new() { TenantId = tenant.Id, Name = "Qualified",   DisplayOrder = 2, Probability = 30,  Color = "#3b82f6" },
                new() { TenantId = tenant.Id, Name = "Proposal",    DisplayOrder = 3, Probability = 60,  Color = "#eab308" },
                new() { TenantId = tenant.Id, Name = "Negotiation", DisplayOrder = 4, Probability = 80,  Color = "#f97316" },
                new() { TenantId = tenant.Id, Name = "Won",         DisplayOrder = 5, Probability = 100, Color = "#22c55e" },
                new() { TenantId = tenant.Id, Name = "Lost",        DisplayOrder = 6, Probability = 0,   Color = "#ef4444" }
            };

            db.PipelineStages.AddRange(stages);
            await db.SaveChangesAsync();
        }
    }
    public static async Task<Tenant> CreateTenantAsync(
    OrbitCRMDbContext db,
    string companyName,
    string adminEmail)
    {
        // Generate a URL-safe slug from the company name
        var slug = GenerateSlug(companyName);

        // Ensure slug is unique (append a counter if needed)
        var baseSlug = slug;
        var counter = 1;
        while (await db.Tenants.AnyAsync(t => t.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var tenant = new Tenant
        {
            Name = companyName,
            Slug = slug,
            Email = adminEmail,
            IsActive = true
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // Default pipeline stages for every new tenant
        var stages = new List<PipelineStage>
    {
        new() { TenantId = tenant.Id, Name = "New",         DisplayOrder = 1, Probability = 10,  Color = "#6b7280" },
        new() { TenantId = tenant.Id, Name = "Qualified",   DisplayOrder = 2, Probability = 30,  Color = "#3b82f6" },
        new() { TenantId = tenant.Id, Name = "Proposal",    DisplayOrder = 3, Probability = 60,  Color = "#eab308" },
        new() { TenantId = tenant.Id, Name = "Negotiation", DisplayOrder = 4, Probability = 80,  Color = "#f97316" },
        new() { TenantId = tenant.Id, Name = "Won",         DisplayOrder = 5, Probability = 100, Color = "#22c55e" },
        new() { TenantId = tenant.Id, Name = "Lost",        DisplayOrder = 6, Probability = 0,   Color = "#ef4444" }
    };

        db.PipelineStages.AddRange(stages);
        await db.SaveChangesAsync();

        return tenant;
    }

    private static string GenerateSlug(string name)
    {
        var slug = name
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Aggregate(string.Empty, (acc, c) => acc + c);

        // Collapse multiple dashes
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        slug = slug.Trim('-');

        return string.IsNullOrEmpty(slug) ? "tenant" : slug;
    }
}