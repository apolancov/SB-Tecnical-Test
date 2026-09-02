using Domain.Entities;
using Xunit;

namespace Domain;

public class InstitutionTests
{
    private const string Name = "Acuario Nacional";
    private const string CategoryName = "Organismo Descentralizado Funcionalmente";
    private const string StatePowerName = "Poder Ejecutivo";
    private const string SectorName = "Medio Ambiente y Recursos Naturales";

    private static Institution CreateInstitution(
        string name = Name,
        Category? category = null,
        StatePower? statePower = null,
        Sector? sector = null) =>
        new(
            name,
            category ?? new Category(CategoryName),
            statePower ?? new StatePower(StatePowerName),
            sector ?? new Sector(SectorName));

    [Fact]
    public void Constructor_WithValidArguments_AssignsProperties()
    {
        var institution = CreateInstitution();

        Assert.Equal(Name, institution.Name);
        Assert.Equal(CategoryName, institution.Category.Name);
        Assert.Equal(StatePowerName, institution.StatePower.Name);
        Assert.Equal(SectorName, institution.Sector.Name);
        Assert.NotEqual(Guid.Empty, institution.Id);
    }

    [Fact]
    public void Constructor_TrimsWhitespaceOnNameProperty()
    {
        var institution = new Institution(
            "  Archivo General de la Nación  ",
            new Category(CategoryName),
            new StatePower(StatePowerName),
            new Sector(SectorName));

        Assert.Equal("Archivo General de la Nación", institution.Name);
    }

    [Fact]
    public void Constructor_AssignsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var institution = CreateInstitution(name: "Sample Institution");

        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(institution.CreatedAt, before, after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyName_Throws(string? invalidName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Institution(
                invalidName!,
                new Category(CategoryName),
                new StatePower(StatePowerName),
                new Sector(SectorName)));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCategory_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new Institution(Name, null!, new StatePower(StatePowerName), new Sector(SectorName)));

        Assert.Equal("category", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullStatePower_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new Institution(Name, new Category(CategoryName), null!, new Sector(SectorName)));

        Assert.Equal("statePower", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSector_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new Institution(Name, new Category(CategoryName), new StatePower(StatePowerName), null!));

        Assert.Equal("sector", exception.ParamName);
    }

    [Fact]
    public void EachInstance_HasUniqueIdentifier()
    {
        var first = CreateInstitution(name: "Name A");
        var second = CreateInstitution(name: "Name B");

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Update_WithValidArguments_AssignsProperties()
    {
        var institution = CreateInstitution(name: "Initial Name");

        var beforeCreatedAt = institution.CreatedAt;

        institution.Update(
            "Updated Name",
            new Category("Updated Category"),
            new StatePower("Updated Power"),
            new Sector("Updated Sector"));

        Assert.Equal("Updated Name", institution.Name);
        Assert.Equal("Updated Category", institution.Category.Name);
        Assert.Equal("Updated Power", institution.StatePower.Name);
        Assert.Equal("Updated Sector", institution.Sector.Name);
        Assert.Equal(beforeCreatedAt, institution.CreatedAt);
    }

    [Fact]
    public void Update_TrimsWhitespaceOnNameProperty()
    {
        var institution = CreateInstitution(name: "Initial Name");

        institution.Update(
            "  Updated Name  ",
            new Category("Updated Category"),
            new StatePower("Updated Power"),
            new Sector("Updated Sector"));

        Assert.Equal("Updated Name", institution.Name);
    }

    [Fact]
    public void Update_DoesNotChangeIdentifier()
    {
        var institution = CreateInstitution(name: "Initial Name");

        var originalId = institution.Id;

        institution.Update(
            "Updated Name",
            new Category("Updated Category"),
            new StatePower("Updated Power"),
            new Sector("Updated Sector"));

        Assert.Equal(originalId, institution.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithNullOrEmptyName_Throws(string? invalidName)
    {
        var institution = CreateInstitution();

        var exception = Assert.Throws<ArgumentException>(() =>
            institution.Update(
                invalidName!,
                new Category("Updated Category"),
                new StatePower("Updated Power"),
                new Sector("Updated Sector")));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Update_WithNullCategory_Throws()
    {
        var institution = CreateInstitution();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            institution.Update(Name, null!, new StatePower("Updated Power"), new Sector("Updated Sector")));

        Assert.Equal("category", exception.ParamName);
    }

    [Fact]
    public void Update_WithNullStatePower_Throws()
    {
        var institution = CreateInstitution();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            institution.Update(Name, new Category("Updated Category"), null!, new Sector("Updated Sector")));

        Assert.Equal("statePower", exception.ParamName);
    }

    [Fact]
    public void Update_WithNullSector_Throws()
    {
        var institution = CreateInstitution();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            institution.Update(Name, new Category("Updated Category"), new StatePower("Updated Power"), null!));

        Assert.Equal("sector", exception.ParamName);
    }

    [Fact]
    public void NameMaximumLength_IsExposedOnEntity()
    {
        Assert.Equal(256, Institution.NameMaximumLength);
    }
}