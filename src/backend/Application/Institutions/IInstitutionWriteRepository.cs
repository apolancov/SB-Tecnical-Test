using Domain.Entities;

namespace Application.Institutions;

public interface IInstitutionWriteRepository
{
    Task AddAsync(Institution institution, CancellationToken cancellationToken);

    void Update(Institution institution);

    void Remove(Institution institution);
}
