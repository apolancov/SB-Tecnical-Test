using Application.Institutions;
using Domain.Entities;

namespace Application.Institutions.TestUtilities;

public sealed class InMemoryInstitutionWriteRepository : IInstitutionWriteRepository
{
    private readonly Action<Institution>? _onAdd;
    private readonly Action<Institution>? _onUpdate;
    private readonly Action<Institution>? _onRemove;

    public InMemoryInstitutionWriteRepository(
        Action<Institution>? onAdd = null,
        Action<Institution>? onUpdate = null,
        Action<Institution>? onRemove = null)
    {
        _onAdd = onAdd;
        _onUpdate = onUpdate;
        _onRemove = onRemove;
    }

    public int AddCallCount { get; private set; }

    public int UpdateCallCount { get; private set; }

    public int RemoveCallCount { get; private set; }

    public Task AddAsync(Institution institution, CancellationToken cancellationToken)
    {
        AddCallCount++;
        _onAdd?.Invoke(institution);
        return Task.CompletedTask;
    }

    public void Update(Institution institution)
    {
        UpdateCallCount++;
        _onUpdate?.Invoke(institution);
    }

    public void Remove(Institution institution)
    {
        RemoveCallCount++;
        _onRemove?.Invoke(institution);
    }
}
