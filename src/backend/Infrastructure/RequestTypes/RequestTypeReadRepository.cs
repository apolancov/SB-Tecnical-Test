using Application.Requests;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.RequestTypes;

public sealed class RequestTypeReadRepository : IRequestTypeReadRepository
{
    private readonly ApplicationDbContext _context;

    public RequestTypeReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<RequestType?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Task.FromResult<RequestType?>(null);
        }

        return _context.RequestTypes
            .FirstOrDefaultAsync(requestType => requestType.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestType>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var types = await _context.RequestTypes
            .AsNoTracking()
            .Where(requestType => requestType.IsActive)
            .OrderBy(requestType => requestType.Name)
            .ToListAsync(cancellationToken);

        return types;
    }
}
