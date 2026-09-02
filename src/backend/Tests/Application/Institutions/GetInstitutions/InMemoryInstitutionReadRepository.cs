using Application.Common.Pagination;
using Application.Institutions.GetInstitutionFilterOptions;
using Application.Institutions.GetInstitutions;
using Domain.Entities;

namespace Application.Institutions.GetInstitutions.TestUtilities;

public sealed class InMemoryInstitutionReadRepository
    : IInstitutionReadRepository, IInstitutionFilterOptionsReadRepository
{
    private readonly IReadOnlyList<Institution> _items;

    public InMemoryInstitutionReadRepository(IEnumerable<Institution> institutions)
    {
        _items = institutions.ToList();
    }

    public int SearchCallCount { get; private set; }

    public int FilterOptionsCallCount { get; private set; }

    public int FindByIdCallCount { get; private set; }

    public Task<PaginatedResult<InstitutionDto>> SearchAsync(
        InstitutionQueryCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        SearchCallCount++;

        var filtered = _items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.Name))
        {
            var term = criteria.Name;
            filtered = filtered.Where(item =>
                item.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            var value = criteria.Category;
            filtered = filtered.Where(item =>
                string.Equals(item.GetCategoryName(), value, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(criteria.StatePower))
        {
            var value = criteria.StatePower;
            filtered = filtered.Where(item =>
                string.Equals(item.GetStatePowerName(), value, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Sector))
        {
            var value = criteria.Sector;
            filtered = filtered.Where(item =>
                string.Equals(item.GetSectorName(), value, StringComparison.Ordinal));
        }

        var ordered = filtered
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Id)
            .ToList();

        var totalItems = ordered.Count;
        var paged = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new InstitutionDto(
                item.Id,
                item.Name,
                item.GetCategoryName(),
                item.GetStatePowerName(),
                item.GetSectorName()))
            .ToList();

        return Task.FromResult(new PaginatedResult<InstitutionDto>(
            paged, page, pageSize, totalItems));
    }

    public Task<InstitutionFilterOptionsDto> GetFilterOptionsAsync(
        CancellationToken cancellationToken)
    {
        FilterOptionsCallCount++;

        var categories = _items
            .Select(item => item.GetCategoryName())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        var statePowers = _items
            .Select(item => item.GetStatePowerName())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        var sectors = _items
            .Select(item => item.GetSectorName())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(new InstitutionFilterOptionsDto(
            categories, statePowers, sectors));
    }

    public Task<Institution?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        FindByIdCallCount++;

        if (id == Guid.Empty)
        {
            return Task.FromResult<Institution?>(null);
        }

        Institution? match = null;
        foreach (var item in _items)
        {
            if (item.Id == id)
            {
                match = Clone(item);
                break;
            }
        }

        return Task.FromResult(match);
    }

    private static Institution Clone(Institution source)
    {
        var clone = new Institution(
            source.Name,
            source.Category,
            source.StatePower,
            source.Sector);
        SetId(clone, source.Id);
        return clone;
    }

    private static void SetId(Institution institution, Guid id)
    {
        var property = typeof(Institution).GetProperty(
            nameof(Institution.Id),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        property?.SetValue(institution, id);
    }
}

internal static class InstitutionLookupExtensions
{
    public static string GetCategoryName(this Institution institution) =>
        institution.Category.Name;

    public static string GetStatePowerName(this Institution institution) =>
        institution.StatePower.Name;

    public static string GetSectorName(this Institution institution) =>
        institution.Sector.Name;
}