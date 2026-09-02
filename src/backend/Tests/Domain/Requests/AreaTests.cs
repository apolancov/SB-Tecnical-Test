using Domain.Entities;
using Xunit;

namespace Domain.Requests;

public class AreaTests
{
    [Fact]
    public void Constructor_WithValidName_AssignsProperties()
    {
        var area = new Area("Atención al Ciudadano");

        Assert.Equal("Atención al Ciudadano", area.Name);
        Assert.True(area.IsActive);
        Assert.NotEqual(Guid.Empty, area.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyName_Throws(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() => new Area(invalidName!));
    }

    [Fact]
    public void Constructor_WithNameLongerThanMaximum_Throws()
    {
        var longName = new string('a', Area.NameMaximumLength + 1);

        Assert.Throws<ArgumentException>(() => new Area(longName));
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var area = new Area("  Atención al Ciudadano  ");

        Assert.Equal("Atención al Ciudadano", area.Name);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var area = new Area("Atención al Ciudadano");

        area.Deactivate();

        Assert.False(area.IsActive);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        var area = new Area("Atención al Ciudadano", isActive: false);

        area.Activate();

        Assert.True(area.IsActive);
    }
}
