using Domain.Entities;

namespace Application.Institutions.Classifications;

public interface ICategoryReadRepository
{
    Task<Category?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken);
}

public interface IStatePowerReadRepository
{
    Task<StatePower?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken);
}

public interface ISectorReadRepository
{
    Task<Sector?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken);
}