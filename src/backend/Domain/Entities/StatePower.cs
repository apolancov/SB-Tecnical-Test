namespace Domain.Entities;

public class StatePower
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    private StatePower()
    {
        Name = string.Empty;
    }

    public StatePower(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("StatePower name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
    }
}