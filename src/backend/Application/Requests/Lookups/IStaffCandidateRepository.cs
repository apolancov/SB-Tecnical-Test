using Domain.Entities;

namespace Application.Requests.Lookups;

public interface IStaffCandidateRepository
{
    Task<IReadOnlyList<User>> ListStaffCandidatesAsync(CancellationToken cancellationToken);
}