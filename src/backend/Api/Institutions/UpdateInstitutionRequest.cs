namespace Api.Institutions;

public sealed record UpdateInstitutionRequest(
    string? Name,
    string? Category,
    string? StatePower,
    string? Sector);
