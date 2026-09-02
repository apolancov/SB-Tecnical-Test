using Application.Requests.TestUtilities;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Requests.Dashboard;

public class GetDashboardSummaryHandlerTests
{
    private static (User requester, Area area, RequestType type) SeedCatalog()
    {
        var requester = new User("requester", "requester@example.local", "hash", UserRole.User);
        var area = new Area("Atención al Ciudadano");
        var type = new RequestType("Incidente", "Reporte");
        return (requester, area, type);
    }

    [Fact]
    public async Task HandleAsync_WithNoRequests_ReturnsZeroMetricsAndAllEnumKeys()
    {
        var repository = new InMemoryDashboardReadRepository();
        var handler = new GetDashboardSummaryHandler(repository, NullLogger<GetDashboardSummaryHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess, $"Error: {result.Error?.Code} - {result.Error?.Message}");
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value!.TotalRequests);
        Assert.Equal(0, result.Value.PendingRequests);
        Assert.Equal(0, result.Value.AssignedRequests);
        Assert.Equal(0, result.Value.UnassignedRequests);

        foreach (RequestStatus status in Enum.GetValues<RequestStatus>())
        {
            Assert.True(result.Value.RequestsByStatus.ContainsKey(status));
        }

        foreach (RequestPriority priority in Enum.GetValues<RequestPriority>())
        {
            Assert.True(result.Value.RequestsByPriority.ContainsKey(priority));
        }
    }

    [Fact]
    public async Task HandleAsync_WithMixedRequests_AggregatesCountsCorrectly()
    {
        var (requester, area, type) = SeedCatalog();
        var now = DateTime.UtcNow;

        var first = new Request("SOL-2026-0001", "First", "Desc", RequestPriority.High, requester, area, type, now, null);
        var second = new Request("SOL-2026-0002", "Second", "Desc", RequestPriority.Low, requester, area, type, now, null);

        var repository = new InMemoryDashboardReadRepository(new[] { first, second });
        var handler = new GetDashboardSummaryHandler(repository, NullLogger<GetDashboardSummaryHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalRequests);
        Assert.Equal(2, result.Value.PendingRequests);
        Assert.Equal(0, result.Value.AssignedRequests);
        Assert.Equal(2, result.Value.UnassignedRequests);

        Assert.Equal(2, result.Value.RequestsByStatus[RequestStatus.Submitted]);
        Assert.Equal(1, result.Value.RequestsByPriority[RequestPriority.High]);
        Assert.Equal(1, result.Value.RequestsByPriority[RequestPriority.Low]);
    }

    [Fact]
    public async Task HandleAsync_AssigningOneRequest_IncrementsAssignedAndDecrementsUnassigned()
    {
        var (requester, area, type) = SeedCatalog();
        var now = DateTime.UtcNow;

        var first = new Request("SOL-2026-0001", "First", "Desc", RequestPriority.High, requester, area, type, now, null);
        var second = new Request("SOL-2026-0002", "Second", "Desc", RequestPriority.High, requester, area, type, now, null);

        var agent = new User("agent", "agent@example.local", "hash", UserRole.User);
        first.AssignResponsible(agent, requester, now, "Take it.");

        var repository = new InMemoryDashboardReadRepository(new[] { first, second });
        var handler = new GetDashboardSummaryHandler(repository, NullLogger<GetDashboardSummaryHandler>.Instance);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalRequests);
        Assert.Equal(1, result.Value.AssignedRequests);
        Assert.Equal(1, result.Value.UnassignedRequests);
    }
}