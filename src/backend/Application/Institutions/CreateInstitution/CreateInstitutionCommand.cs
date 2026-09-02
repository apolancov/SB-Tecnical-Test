namespace Application.Institutions.CreateInstitution;

public sealed record CreateInstitutionCommand(
    string Name,
    string Category,
    string StatePower,
    string Sector);
