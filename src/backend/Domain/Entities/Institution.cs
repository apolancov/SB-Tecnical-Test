namespace Domain.Entities;

public class Institution
{
    public const int NameMaximumLength = 256;

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category Category { get; private set; }

    public Guid StatePowerId { get; private set; }

    public StatePower StatePower { get; private set; }

    public Guid SectorId { get; private set; }

    public Sector Sector { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Institution()
    {
        Name = string.Empty;
        Category = null!;
        StatePower = null!;
        Sector = null!;
    }

    public Institution(
        string name,
        Category category,
        StatePower statePower,
        Sector sector)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        if (category is null)
        {
            throw new ArgumentNullException(nameof(category), "Category cannot be null.");
        }

        if (statePower is null)
        {
            throw new ArgumentNullException(nameof(statePower), "StatePower cannot be null.");
        }

        if (sector is null)
        {
            throw new ArgumentNullException(nameof(sector), "Sector cannot be null.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        CategoryId = category.Id;
        Category = category;
        StatePowerId = statePower.Id;
        StatePower = statePower;
        SectorId = sector.Id;
        Sector = sector;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        Category category,
        StatePower statePower,
        Sector sector)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        if (category is null)
        {
            throw new ArgumentNullException(nameof(category), "Category cannot be null.");
        }

        if (statePower is null)
        {
            throw new ArgumentNullException(nameof(statePower), "StatePower cannot be null.");
        }

        if (sector is null)
        {
            throw new ArgumentNullException(nameof(sector), "Sector cannot be null.");
        }

        Name = name.Trim();
        CategoryId = category.Id;
        Category = category;
        StatePowerId = statePower.Id;
        StatePower = statePower;
        SectorId = sector.Id;
        Sector = sector;
    }
}