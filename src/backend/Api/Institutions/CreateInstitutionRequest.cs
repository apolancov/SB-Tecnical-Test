namespace Api.Institutions;

public sealed record CreateInstitutionRequest(
    string? Name,
    string? Category,
    string? StatePower,
    string? Sector);
