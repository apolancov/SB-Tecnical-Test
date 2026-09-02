namespace Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    private Category()
    {
        Name = string.Empty;
    }

    public Category(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
    }
}