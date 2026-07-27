using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TmsApi.Api.Hubs;

namespace TmsApi.Api.Tests.Hubs;

/// <summary>
/// Unit tests for TmsHub (task 5.3).
/// Validates: Requirements 6.1–6.6, 7.1–7.5, 9.1–9.5
/// </summary>
public class TmsHubUnitTests
{
    // -------------------------------------------------------------------
    // FakeHubCallerContext
    //
    // Context.GetHttpContext() is an extension method that reads
    //   context.Features.Get<IHttpContextFeature>()?.HttpContext
    // Moq cannot mock extension methods, so we provide a hand-rolled
    // stub that exposes a real FeatureCollection pre-seeded with an
    // IHttpContextFeature backed by the supplied HttpContext.
    // -------------------------------------------------------------------

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly FeatureCollection _features = new();
        private bool _abortCalled;

        public bool AbortWasCalled => _abortCalled;

        public FakeHubCallerContext(HttpContext? httpContext = null)
        {
            if (httpContext != null)
            {
                var feature = new HttpContextFeature(httpContext);
                _features.Set<IHttpContextFeature>(feature);
            }
        }

        public override string ConnectionId => "test-connection-id";
        public override string? UserIdentifier => null;
        public override System.Security.Claims.ClaimsPrincipal User => new();
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features => _features;
        public override System.Threading.CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort() => _abortCalled = true;

        // Simple IHttpContextFeature implementation
        private sealed class HttpContextFeature : IHttpContextFeature
        {
            public HttpContextFeature(HttpContext ctx) => HttpContext = ctx;
            public HttpContext? HttpContext { get; set; }
        }
    }

    // -------------------------------------------------------------------
    // Helper: build a TmsHub with a FakeHubCallerContext + mocked infra
    // -------------------------------------------------------------------

    private static (
        TmsHub hub,
        FakeHubCallerContext context,
        Mock<IGroupManager> groupsMock,
        Mock<IHubCallerClients<ITmsHubClient>> clientsMock,
        Mock<ITmsHubClient> callerMock
    ) CreateHub(HttpContext? httpContext = null)
    {
        var context = new FakeHubCallerContext(httpContext);
        var groupsMock = new Mock<IGroupManager>();
        var clientsMock = new Mock<IHubCallerClients<ITmsHubClient>>();
        var callerMock = new Mock<ITmsHubClient>();

        // Default: caller client returns the callerMock
        clientsMock.Setup(c => c.Caller).Returns(callerMock.Object);

        // Default: caller sends course announcement (returns completed task)
        callerMock
            .Setup(c => c.CourseAnnouncement(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Default: group operations return completed tasks
        groupsMock
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groupsMock
            .Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new TmsHub(NullLogger<TmsHub>.Instance);
        hub.Context = context;
        hub.Groups = groupsMock.Object;
        hub.Clients = clientsMock.Object;

        return (hub, context, groupsMock, clientsMock, callerMock);
    }

    // -------------------------------------------------------------------
    // Test 1: OnConnectedAsync without studentId calls Context.Abort()
    // -------------------------------------------------------------------

    /// <summary>
    /// WHEN a client connects without a studentId in the query string,
    /// THE SignalR_Hub SHALL reject the connection by calling Context.Abort().
    /// Validates: Requirements 7.2
    /// </summary>
    [Fact]
    public async Task OnConnectedAsync_WithoutStudentId_CallsAbort()
    {
        // Arrange — HttpContext with no studentId query param
        var httpContext = new DefaultHttpContext();
        // QueryString is empty by default → Request.Query["studentId"] == StringValues.Empty
        var (hub, context, groupsMock, _, _) = CreateHub(httpContext);

        // Act
        await hub.OnConnectedAsync();

        // Assert: Abort was called
        Assert.True(context.AbortWasCalled, "Expected Context.Abort() to be called when studentId is missing.");

        // Assert: the connection was NOT added to any group
        groupsMock.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// WHEN a client connects with an explicit empty studentId value,
    /// THE SignalR_Hub SHALL reject the connection by calling Context.Abort().
    /// Validates: Requirements 7.2
    /// </summary>
    [Fact]
    public async Task OnConnectedAsync_WithEmptyStudentId_CallsAbort()
    {
        // Arrange — HttpContext with studentId=""
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?studentId=");
        var (hub, context, groupsMock, _, _) = CreateHub(httpContext);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        Assert.True(context.AbortWasCalled, "Expected Context.Abort() to be called when studentId is empty.");
        groupsMock.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------
    // Test 2: JoinCourseGroup adds connection to "course:{courseCode}" group
    // -------------------------------------------------------------------

    /// <summary>
    /// WHEN a client invokes JoinCourseGroup("CS-101"),
    /// THE SignalR_Hub SHALL add the connection to the group named "course:CS-101".
    /// Validates: Requirements 9.1, 9.3
    /// </summary>
    [Fact]
    public async Task JoinCourseGroup_AddsCourseGroupWithCorrectName()
    {
        // Arrange
        var (hub, _, groupsMock, _, _) = CreateHub();
        const string courseCode = "CS-101";
        const string expectedGroupName = "course:CS-101";

        // Act
        await hub.JoinCourseGroup(courseCode);

        // Assert: AddToGroupAsync was called with the exact group name
        groupsMock.Verify(
            g => g.AddToGroupAsync(
                "test-connection-id",
                expectedGroupName,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------
    // Test 3: LeaveCourseGroup removes connection from the correct group
    // -------------------------------------------------------------------

    /// <summary>
    /// WHEN a client invokes LeaveCourseGroup("CS-101"),
    /// THE SignalR_Hub SHALL remove the connection from the group named "course:CS-101".
    /// Validates: Requirements 9.2, 9.3
    /// </summary>
    [Fact]
    public async Task LeaveCourseGroup_RemovesCourseGroupWithCorrectName()
    {
        // Arrange
        var (hub, _, groupsMock, _, _) = CreateHub();
        const string courseCode = "CS-101";
        const string expectedGroupName = "course:CS-101";

        // Act
        await hub.LeaveCourseGroup(courseCode);

        // Assert: RemoveFromGroupAsync was called with the exact group name
        groupsMock.Verify(
            g => g.RemoveFromGroupAsync(
                "test-connection-id",
                expectedGroupName,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
