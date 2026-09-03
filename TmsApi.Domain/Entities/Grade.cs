namespace TmsApi.Domain.Entities;

public class Grade
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public int StudentId { get; set; }
    public decimal Score { get; set; }
    public string? Remarks { get; set; }
    public DateTime GradedAt { get; set; } = DateTime.UtcNow;
    public string? GradedBy { get; set; }
    public Assessment Assessment { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
