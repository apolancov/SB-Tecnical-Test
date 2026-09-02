using Application.Requests.Lookups;
using Application.Requests.TestUtilities;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.Lookups;

public class ListActiveAreasHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsActiveAreasSortedByName()
    {
        var areaActive1 = new Area("Atención al Ciudadano");
        var areaActive2 = new Area("Soporte Técnico");
        var areaInactive = new Area("Inactiva", isActive: false);

        var repository = new InMemoryAreaReadRepository(areaActive1, areaActive2, areaInactive);
        var handler = new ListActiveAreasHandler(repository, NullLogger<ListActiveAreasHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess, $"Error: {result.Error?.Code} - {result.Error?.Message}");
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("Atención al Ciudadano", result.Value[0].Name);
        Assert.Equal("Soporte Técnico", result.Value[1].Name);
    }

    [Fact]
    public async Task HandleAsync_WithNoAreas_ReturnsEmptyList()
    {
        var repository = new InMemoryAreaReadRepository();
        var handler = new ListActiveAreasHandler(repository, NullLogger<ListActiveAreasHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }
}

public class ListActiveRequestTypesHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsActiveRequestTypesSortedByName()
    {
        var typeActive1 = new RequestType("Incidente", "Reporte");
        var typeActive2 = new RequestType("Requerimiento", "Solicitud");
        var typeInactive = new RequestType("Tipo inactivo", "Inactivo", isActive: false);

        var repository = new InMemoryRequestTypeReadRepository(typeActive1, typeActive2, typeInactive);
        var handler = new ListActiveRequestTypesHandler(repository, NullLogger<ListActiveRequestTypesHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess, $"Error: {result.Error?.Code} - {result.Error?.Message}");
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("Incidente", result.Value[0].Name);
        Assert.Equal("Requerimiento", result.Value[1].Name);
    }

    [Fact]
    public async Task HandleAsync_WithNoRequestTypes_ReturnsEmptyList()
    {
        var repository = new InMemoryRequestTypeReadRepository();
        var handler = new ListActiveRequestTypesHandler(repository, NullLogger<ListActiveRequestTypesHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }
}