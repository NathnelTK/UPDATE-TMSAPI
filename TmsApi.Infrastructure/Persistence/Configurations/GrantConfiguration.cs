using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public sealed class GrantProgramConfiguration : IEntityTypeConfiguration<GrantProgram>
{
    public void Configure(EntityTypeBuilder<GrantProgram> b) { b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(200).IsRequired(); b.Property(x => x.Description).HasMaxLength(2000).IsRequired(); b.Property(x => x.FundingOrganization).HasMaxLength(200).IsRequired(); b.Property(x => x.TotalBudget).HasColumnType("decimal(14,2)"); b.Property(x => x.AmountPerStudent).HasColumnType("decimal(14,2)"); }
}
public sealed class GrantApplicationConfiguration : IEntityTypeConfiguration<GrantApplication>
{
    public void Configure(EntityTypeBuilder<GrantApplication> b) { b.HasKey(x => x.Id); b.Property(x => x.Status).HasConversion<int>(); b.Property(x => x.EligibilityScore).HasColumnType("decimal(5,2)"); b.HasIndex(x => new { x.StudentId, x.GrantProgramId }).IsUnique(); b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.GrantProgram).WithMany(x => x.Applications).HasForeignKey(x => x.GrantProgramId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class GrantAllocationConfiguration : IEntityTypeConfiguration<GrantAllocation>
{
    public void Configure(EntityTypeBuilder<GrantAllocation> b) { b.HasKey(x => x.Id); b.Property(x => x.Amount).HasColumnType("decimal(14,2)"); b.HasIndex(x => x.GrantApplicationId).IsUnique(); b.HasOne(x => x.GrantApplication).WithOne(x => x.Allocation).HasForeignKey<GrantAllocation>(x => x.GrantApplicationId).OnDelete(DeleteBehavior.Cascade); }
}
