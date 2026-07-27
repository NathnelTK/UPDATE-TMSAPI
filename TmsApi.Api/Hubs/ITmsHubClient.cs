namespace TmsApi.Api.Hubs;

public interface ITmsHubClient
{
    Task TranscriptReady(string reportId, string studentId, string downloadUrl);
    Task CourseAnnouncement(string courseCode, string message);
}
