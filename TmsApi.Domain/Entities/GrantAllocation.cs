namespace TmsApi.Domain.Entities;

public class GrantAllocation
{
    public int Id { get; set; }
    public int GrantApplicationId { get; set; }
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;
    public bool IsDisbursed { get; set; }
    public GrantApplication GrantApplication { get; set; } = null!;
}
