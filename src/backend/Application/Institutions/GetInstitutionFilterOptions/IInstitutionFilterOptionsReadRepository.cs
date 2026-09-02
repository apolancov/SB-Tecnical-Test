namespace Application.Institutions.GetInstitutionFilterOptions;

public interface IInstitutionFilterOptionsReadRepository
{
    Task<InstitutionFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken);
}
