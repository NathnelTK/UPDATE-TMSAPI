namespace TmsApi.Domain.Entities;

public class GrantApplication
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int GrantProgramId { get; set; }
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    public GrantStatus Status { get; set; } = GrantStatus.Submitted;
    public decimal EligibilityScore { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Student Student { get; set; } = null!;
    public GrantProgram GrantProgram { get; set; } = null!;
    public GrantAllocation? Allocation { get; set; }
}
