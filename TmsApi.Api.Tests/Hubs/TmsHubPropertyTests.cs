using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TmsApi.Api.Hubs;

namespace TmsApi.Api.Tests.Hubs;

/// <summary>
/// Property-based tests for TmsHub (task 5.4).
/// Validates: Requirements 7.1, 7.4
/// </summary>
public class TmsHubPropertyTests
{
    // -------------------------------------------------------------------
    // FakeHubCallerContext (same pattern as TmsHubUnitTests.cs)
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

        public override void Abort() { }

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
        Mock<IGroupManager> groupsMock
    ) CreateHub(HttpContext httpContext)
    {
        var context = new FakeHubCallerContext(httpContext);
        var groupsMock = new Mock<IGroupManager>();
        var clientsMock = new Mock<IHubCallerClients<ITmsHubClient>>();

        groupsMock
            .Setup(g => g.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new TmsHub(NullLogger<TmsHub>.Instance);
        hub.Context = context;
        hub.Groups = groupsMock.Object;
        hub.Clients = clientsMock.Object;

        return (hub, groupsMock);
    }

    // -------------------------------------------------------------------
    // Property 7: SignalR Connection Joins Student Group
    //
    // For any studentId string, connecting to the SignalR hub with query
    // parameter studentId={value} SHALL result in the connection being
    // added to a group named "student:{value}" — no transformation.
    //
    // Validates: Requirements 7.1, 7.4
    // -------------------------------------------------------------------

    /// <summary>
    /// **Validates: Requirements 7.1, 7.4**
    ///
    /// For any non-empty studentId, OnConnectedAsync SHALL call
    /// Groups.AddToGroupAsync exactly once with connectionId "test-connection-id"
    /// and group name "student:{studentId}" — no transformation of the value.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool StudentGroup_HasExactFormat(NonEmptyString studentId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString($"?studentId={Uri.EscapeDataString(studentId.Get)}");
        var (hub, groupsMock) = CreateHub(httpContext);

        // Act
        hub.OnConnectedAsync().GetAwaiter().GetResult();

        // Assert: AddToGroupAsync called exactly once with the exact group name
        var expectedGroupName = $"student:{studentId.Get}";
        try
        {
            groupsMock.Verify(
                g => g.AddToGroupAsync(
                    "test-connection-id",
                    expectedGroupName,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
