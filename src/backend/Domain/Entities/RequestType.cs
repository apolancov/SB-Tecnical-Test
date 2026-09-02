namespace Domain.Entities;

public class RequestType
{
    public const int NameMaximumLength = 128;
    public const int DescriptionMaximumLength = 512;

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public bool IsActive { get; private set; }

    private RequestType()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    public RequestType(string name)
        : this(name, description: string.Empty, isActive: true)
    {
    }

    public RequestType(string name, string description)
        : this(name, description, isActive: true)
    {
    }

    public RequestType(string name, string description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Request type name cannot be null or empty.", nameof(name));
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > NameMaximumLength)
        {
            throw new ArgumentException(
                $"Request type name cannot exceed {NameMaximumLength} characters.",
                nameof(name));
        }

        var descriptionText = description ?? string.Empty;
        var normalizedDescription = descriptionText.Trim();
        if (normalizedDescription.Length > DescriptionMaximumLength)
        {
            throw new ArgumentException(
                $"Request type description cannot exceed {DescriptionMaximumLength} characters.",
                nameof(description));
        }

        Id = Guid.NewGuid();
        Name = normalizedName;
        Description = normalizedDescription;
        IsActive = isActive;
    }

    public void UpdateDescription(string description)
    {
        var normalizedDescription = (description ?? string.Empty).Trim();
        if (normalizedDescription.Length > DescriptionMaximumLength)
        {
            throw new ArgumentException(
                $"Request type description cannot exceed {DescriptionMaximumLength} characters.",
                nameof(description));
        }

        Description = normalizedDescription;
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
