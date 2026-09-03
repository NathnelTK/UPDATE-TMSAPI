namespace TmsApi.Domain.Entities;

public class GrantProgram
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string FundingOrganization { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal AmountPerStudent { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<GrantApplication> Applications { get; set; } = new List<GrantApplication>();
}
