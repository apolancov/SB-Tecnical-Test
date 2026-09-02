namespace Application.Institutions.GetInstitutions;

public sealed record InstitutionQueryCriteria(
    string? Name,
    string? Category,
    string? StatePower,
    string? Sector);