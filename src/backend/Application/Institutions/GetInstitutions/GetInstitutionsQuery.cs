namespace Application.Institutions.GetInstitutions;

public sealed record GetInstitutionsQuery(
    int Page,
    int PageSize,
    string? Name,
    string? Category,
    string? StatePower,
    string? Sector);