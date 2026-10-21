using Microsoft.EntityFrameworkCore;

namespace OrbitCRM.Infrastructure.Data;

public class ScopedDbContextFactory : IDbContextFactory<OrbitCRMDbContext>
{
    private readonly OrbitCRMDbContext _context;

    public ScopedDbContextFactory(OrbitCRMDbContext context)
    {
        _context = context;
    }

    public OrbitCRMDbContext CreateDbContext()
    {
        return _context;
    }
}