namespace TmsApi.Domain.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Remarks { get; set; }
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
