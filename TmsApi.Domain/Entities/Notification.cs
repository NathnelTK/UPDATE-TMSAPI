namespace TmsApi.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string Type { get; set; } = "Information";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
