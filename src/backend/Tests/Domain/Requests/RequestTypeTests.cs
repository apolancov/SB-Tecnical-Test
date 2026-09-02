using Domain.Entities;
using Xunit;

namespace Domain.Requests;

public class RequestTypeTests
{
    [Fact]
    public void Constructor_WithValidName_AssignsProperties()
    {
        var requestType = new RequestType("Incidente", "Reporte de un incidente");

        Assert.Equal("Incidente", requestType.Name);
        Assert.Equal("Reporte de un incidente", requestType.Description);
        Assert.True(requestType.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyName_Throws(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() => new RequestType(invalidName!));
    }

    [Fact]
    public void Constructor_WithNullDescription_StoresEmptyString()
    {
        var requestType = new RequestType("Incidente", null!);

        Assert.Equal(string.Empty, requestType.Description);
    }

    [Fact]
    public void Constructor_WithNameLongerThanMaximum_Throws()
    {
        var longName = new string('a', RequestType.NameMaximumLength + 1);

        Assert.Throws<ArgumentException>(() => new RequestType(longName));
    }

    [Fact]
    public void Constructor_WithDescriptionLongerThanMaximum_Throws()
    {
        var longDescription = new string('a', RequestType.DescriptionMaximumLength + 1);

        Assert.Throws<ArgumentException>(() => new RequestType("Incidente", longDescription));
    }

    [Fact]
    public void UpdateDescription_ReplacesDescription()
    {
        var requestType = new RequestType("Incidente", "Reporte inicial");

        requestType.UpdateDescription("Reporte actualizado");

        Assert.Equal("Reporte actualizado", requestType.Description);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var requestType = new RequestType("Incidente", "Reporte");

        requestType.Deactivate();

        Assert.False(requestType.IsActive);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrue()
    {
        var requestType = new RequestType("Incidente", "Reporte", isActive: false);

        requestType.Activate();

        Assert.True(requestType.IsActive);
    }
}
