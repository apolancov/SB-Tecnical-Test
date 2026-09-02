using Application.Requests.Lookups;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Requests;

public sealed class StaffCandidateRepository : IStaffCandidateRepository
{
    private static readonly UserRole[] StaffRoles = { UserRole.Admin, UserRole.Analista };

    private readonly ApplicationDbContext _context;

    public StaffCandidateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<User>> ListStaffCandidatesAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive && StaffRoles.Contains(user.Role))
            .ToListAsync(cancellationToken);
    }
}