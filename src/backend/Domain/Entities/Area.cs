namespace Domain.Entities;

public class Area
{
    public const int NameMaximumLength = 128;

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    private Area()
    {
        Name = string.Empty;
    }

    public Area(string name)
        : this(name, isActive: true)
    {
    }

    public Area(string name, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Area name cannot be null or empty.", nameof(name));
        }

        var normalized = name.Trim();

        if (normalized.Length > NameMaximumLength)
        {
            throw new ArgumentException(
                $"Area name cannot exceed {NameMaximumLength} characters.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        Name = normalized;
        IsActive = isActive;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
