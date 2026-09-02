using Application.Common.Pagination;
using Application.Institutions.GetInstitutionFilterOptions;
using Application.Institutions.GetInstitutions;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Institutions;

public sealed class InstitutionReadRepository
    : IInstitutionReadRepository, IInstitutionFilterOptionsReadRepository
{
    private readonly ApplicationDbContext _context;

    public InstitutionReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<InstitutionDto>> SearchAsync(
        InstitutionQueryCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Institutions
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, criteria);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(institution => institution.Name)
            .ThenBy(institution => institution.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(institution => new InstitutionDto(
                institution.Id,
                institution.Name,
                institution.Category.Name,
                institution.StatePower.Name,
                institution.Sector.Name))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<InstitutionDto>(items, page, pageSize, totalItems);
    }

    private static IQueryable<Institution> ApplyFilters(
        IQueryable<Institution> query,
        InstitutionQueryCriteria criteria)
    {
        if (criteria.Name is not null)
        {
            var nameFilter = criteria.Name;
            query = query.Where(institution =>
                EF.Functions.Like(institution.Name, $"%{nameFilter}%"));
        }

        if (criteria.Category is not null)
        {
            var categoryFilter = criteria.Category;
            query = query.Where(institution => institution.Category.Name == categoryFilter);
        }

        if (criteria.StatePower is not null)
        {
            var statePowerFilter = criteria.StatePower;
            query = query.Where(institution => institution.StatePower.Name == statePowerFilter);
        }

        if (criteria.Sector is not null)
        {
            var sectorFilter = criteria.Sector;
            query = query.Where(institution => institution.Sector.Name == sectorFilter);
        }

        return query;
    }

    public async Task<InstitutionFilterOptionsDto> GetFilterOptionsAsync(
        CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => category.Name)
            .ToListAsync(cancellationToken);

        var statePowers = await _context.StatePowers
            .AsNoTracking()
            .OrderBy(statePower => statePower.Name)
            .Select(statePower => statePower.Name)
            .ToListAsync(cancellationToken);

        var sectors = await _context.Sectors
            .AsNoTracking()
            .OrderBy(sector => sector.Name)
            .Select(sector => sector.Name)
            .ToListAsync(cancellationToken);

        return new InstitutionFilterOptionsDto(categories, statePowers, sectors);
    }

    public Task<Institution?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Task.FromResult<Institution?>(null);
        }

        return _context.Institutions
            .Include(institution => institution.Category)
            .Include(institution => institution.StatePower)
            .Include(institution => institution.Sector)
            .FirstOrDefaultAsync(institution => institution.Id == id, cancellationToken);
    }
}