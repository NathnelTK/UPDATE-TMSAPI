using Microsoft.AspNetCore.SignalR;

namespace TmsApi.Api.Hubs;

public class TmsHub : Hub<ITmsHubClient>
{
    private readonly ILogger<TmsHub> _logger;

    public TmsHub(ILogger<TmsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var studentId = Context.GetHttpContext()?.Request.Query["studentId"].ToString();

        if (string.IsNullOrEmpty(studentId))
        {
            _logger.LogWarning("Connection {ConnectionId} rejected: missing studentId",
                Context.ConnectionId);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"student:{studentId}");
        _logger.LogInformation("Connection {ConnectionId} joined student group for studentId {StudentId}",
            Context.ConnectionId, studentId);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task<string> JoinCourseGroup(string courseCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"course:{courseCode}");
        await Clients.Caller.CourseAnnouncement(courseCode, $"Joined course {courseCode}");
        return $"Joined course group {courseCode}";
    }

    public async Task<string> LeaveCourseGroup(string courseCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"course:{courseCode}");
        return $"Left course group {courseCode}";
    }
}
