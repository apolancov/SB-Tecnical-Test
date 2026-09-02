namespace Domain.Entities;

public class Sector
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    private Sector()
    {
        Name = string.Empty;
    }

    public Sector(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sector name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
    }
}