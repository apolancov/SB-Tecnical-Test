using Application.Institutions.Classifications;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Institutions;

public sealed class CategoryReadRepository : ICategoryReadRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Category?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Task.FromResult<Category?>(null);
        }

        return _context.Categories
            .FirstOrDefaultAsync(category => category.Name == normalizedName, cancellationToken);
    }
}

public sealed class StatePowerReadRepository : IStatePowerReadRepository
{
    private readonly ApplicationDbContext _context;

    public StatePowerReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<StatePower?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Task.FromResult<StatePower?>(null);
        }

        return _context.StatePowers
            .FirstOrDefaultAsync(statePower => statePower.Name == normalizedName, cancellationToken);
    }
}

public sealed class SectorReadRepository : ISectorReadRepository
{
    private readonly ApplicationDbContext _context;

    public SectorReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Sector?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Task.FromResult<Sector?>(null);
        }

        return _context.Sectors
            .FirstOrDefaultAsync(sector => sector.Name == normalizedName, cancellationToken);
    }
}