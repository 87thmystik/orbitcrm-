using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrbitCRM.Application.Common.Interfaces;
using OrbitCRM.Domain.Entities;
using OrbitCRM.Domain.Identity;
using OrbitCRM.Infrastructure;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;
using OrbitCRM.Infrastructure.Services;
using OrbitCRM.Web.Components;
using System.Globalization;

// Set culture BEFORE builder creation so it applies to the whole app.
var culture = new CultureInfo("en-NG");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. Blazor + Auth cascading state
// ---------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// ---------------------------------------------------------
// 2. Infrastructure (DbContext, CurrentTenant, CurrentUser)
// ---------------------------------------------------------
builder.Services.AddInfrastructure(builder.Configuration);

// ---------------------------------------------------------
// 3. Authentication (cookie scheme)
// ---------------------------------------------------------
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// ---------------------------------------------------------
// 4. Authorization Policies
// ---------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All())
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim("permission", permission));
    }
});

// ---------------------------------------------------------
// 5. Identity with roles, stores, sign-in, claims factory
// ---------------------------------------------------------
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<OrbitCRMDbContext>()
.AddSignInManager()
.AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization();

// ---------------------------------------------------------
// 6. Build
// ---------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------
// 7. Seed (tenant, pipeline stages, demo user + membership)
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrbitCRMDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    await DbSeeder.SeedAsync(db);
    await DbSeeder.SeedDemoUserAsync(userManager, db);
}
// ---------------------------------------------------------
// 8. Pipeline
// ---------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------
// 9. Minimal API endpoints for auth (login/logout)
// ---------------------------------------------------------
app.MapPost("/account/login-post", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        return Results.Redirect("/Account/Login?error=missing");
    }

    var result = await signInManager.PasswordSignInAsync(
        email, password, isPersistent: true, lockoutOnFailure: false);

    if (result.Succeeded)
        return Results.Redirect("/");

    return Results.Redirect("/Account/Login?error=invalid");
}).DisableAntiforgery();

app.MapPost("/account/register-post", async (
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    OrbitCRMDbContext db,
    IEmailService emailService,
    IConfiguration config,      
    ILogger<Program> logger) =>
{
    var form = await context.Request.ReadFormAsync();

    var firstName = form["firstName"].ToString().Trim();
    var lastName = form["lastName"].ToString().Trim();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();
    var companyName = form["companyName"].ToString().Trim();

    if (string.IsNullOrWhiteSpace(firstName) ||
        string.IsNullOrWhiteSpace(lastName) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(password) ||
        string.IsNullOrWhiteSpace(companyName))
    {
        return Results.Redirect("/Account/Register?error=missing");
    }

    var existing = await userManager.FindByEmailAsync(email);
    if (existing is not null)
    {
        return Results.Redirect("/Account/Register?error=email-taken");
    }

    using var db = await dbFactory.CreateDbContextAsync();
    var tenant = await DbSeeder.CreateTenantAsync(db, companyName, email);

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        EmailConfirmed = true,
        FirstName = firstName,
        LastName = lastName,
        IsActive = true
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync();

        var errors = string.Join(",", createResult.Errors.Select(e => e.Code));
        return Results.Redirect($"/Account/Register?error={Uri.EscapeDataString(errors)}");
    }

    db.TenantMemberships.Add(new TenantMembership
    {
        TenantId = tenant.Id,
        UserId = user.Id,
        Role = Roles.Owner,
        IsActive = true
    });
    await db.SaveChangesAsync();

    // Send the welcome email
    try
    {
        var appUrl = config["AppUrl"] ?? "https://localhost:7070";
        await emailService.SendAsync(
            to: user.Email!,
            subject: "Welcome to OrbitCRM!",
            body: $@"
Hi {user.FirstName},

Welcome to OrbitCRM! Your workspace '{companyName}' is ready.

You can log in and start managing your customers, leads, and deals right away:

{appUrl}/Account/Login

Your account email is: {user.Email}

If you have any questions, just reply to this email.

Cheers,
The OrbitCRM Team
");
    }
    catch (Exception ex)
    {
        // Don't block registration if the email fails
        logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
    }

    // Sign them in (this writes the TenantId claim via the claims factory)
    await signInManager.SignInAsync(user, isPersistent: true);

    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapGet("/account/logout-post", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/Account/Login");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();