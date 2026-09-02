namespace Application.Institutions.UpdateInstitution;

public sealed record UpdateInstitutionCommand(
    Guid Id,
    string Name,
    string Category,
    string StatePower,
    string Sector);
