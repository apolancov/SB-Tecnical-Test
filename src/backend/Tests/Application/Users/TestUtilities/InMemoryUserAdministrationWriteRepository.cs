using Application.Users;
using Domain.Entities;

namespace Application.Users.TestUtilities;

public sealed class InMemoryUserAdministrationWriteRepository : IUserAdministrationWriteRepository
{
    private readonly List<User> _users;
    private readonly Action<User>? _onAdd;
    private readonly Action<User>? _onUpdate;
    private readonly Action<User>? _onRemove;

    public InMemoryUserAdministrationWriteRepository(
        IEnumerable<User> seed,
        Action<User>? onAdd = null,
        Action<User>? onUpdate = null,
        Action<User>? onRemove = null)
    {
        _users = seed.ToList();
        _onAdd = onAdd;
        _onUpdate = onUpdate;
        _onRemove = onRemove;
    }

    public IReadOnlyList<User> Users => _users;

    public int AddCallCount { get; private set; }

    public int UpdateCallCount { get; private set; }

    public int RemoveCallCount { get; private set; }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        AddCallCount++;
        _onAdd?.Invoke(user);
        _users.Add(user);
        return Task.CompletedTask;
    }

    public void Update(User user)
    {
        UpdateCallCount++;
        _onUpdate?.Invoke(user);
    }

    public void Remove(User user)
    {
        RemoveCallCount++;
        _onRemove?.Invoke(user);
        _users.Remove(user);
    }
}
