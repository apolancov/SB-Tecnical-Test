namespace Application.Institutions.GetInstitutions;

public sealed record InstitutionDto(
    Guid Id,
    string Name,
    string Category,
    string StatePower,
    string Sector);