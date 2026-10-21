using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrbitCRM.Application.Activities.Interfaces;
using OrbitCRM.Application.Common.Interfaces;
using OrbitCRM.Application.Customers.Interfaces;
using OrbitCRM.Application.Dashboard.Interfaces;
using OrbitCRM.Application.Deals.Interfaces;
using OrbitCRM.Application.Leads.Interfaces;
using OrbitCRM.Application.Notifications.Interfaces;
using OrbitCRM.Application.Reports.Interfaces;
using OrbitCRM.Application.Settings.Interfaces;
using OrbitCRM.Application.Tasks.Interfaces;
using OrbitCRM.Application.Users.Interfaces;
using OrbitCRM.Infrastructure.Data;
using OrbitCRM.Infrastructure.Identity;
using OrbitCRM.Infrastructure.Services;
using OrbitCRM.Infrastructure.Services;
using OrbitCRM.Application.Audit.Interfaces;

namespace OrbitCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<CurrentTenant>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());
        services.AddScoped<TenantGuard>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Register DbContext WITHOUT the factory to avoid the root provider crash
        services.AddDbContext<OrbitCRMDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(OrbitCRMDbContext).Assembly.FullName));

            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();

            // Add this to prevent false-positive crashes during rapid dev
            options.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Application services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IDealService, DealService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEmailService, ConsoleEmailService>();

        return services;
    }
}