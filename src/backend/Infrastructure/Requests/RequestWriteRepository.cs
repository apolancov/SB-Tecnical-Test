using Application.Requests;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Requests;

public sealed class RequestWriteRepository : IRequestWriteRepository
{
    private readonly ApplicationDbContext _context;

    public RequestWriteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Request request, CancellationToken cancellationToken)
    {
        await _context.Requests.AddAsync(request, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Request request, CancellationToken cancellationToken)
    {
        // Force the change tracker to re-scan navigation properties so that
        // any entity added through the domain (Request.AddComment,
        // Request.ChangeStatus, Request.AssignResponsible) is recognised as
        // Added instead of Modified.
        _context.ChangeTracker.DetectChanges();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
