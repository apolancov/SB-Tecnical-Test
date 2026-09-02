namespace Application.Institutions.GetInstitutionFilterOptions;

public sealed record InstitutionFilterOptionsDto(
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> StatePowers,
    IReadOnlyList<string> Sectors);
