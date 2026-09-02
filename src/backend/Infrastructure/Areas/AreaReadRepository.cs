using Application.Requests;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Areas;

public sealed class AreaReadRepository : IAreaReadRepository
{
    private readonly ApplicationDbContext _context;

    public AreaReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Area?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Task.FromResult<Area?>(null);
        }

        return _context.Areas
            .FirstOrDefaultAsync(area => area.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Area>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var areas = await _context.Areas
            .AsNoTracking()
            .Where(area => area.IsActive)
            .OrderBy(area => area.Name)
            .ToListAsync(cancellationToken);

        return areas;
    }
}
