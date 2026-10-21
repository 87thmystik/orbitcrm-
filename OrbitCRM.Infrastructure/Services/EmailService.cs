using Microsoft.Extensions.Logging;

namespace OrbitCRM.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "\n\n=== EMAIL (Dev Mode) ===\nTo: {To}\nSubject: {Subject}\n\n{Body}\n\n=== END EMAIL ===\n",
            to, subject, body);

        return Task.CompletedTask;
    }
}