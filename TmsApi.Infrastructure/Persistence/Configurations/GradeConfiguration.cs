using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public sealed class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Score).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.HasIndex(x => new { x.AssessmentId, x.StudentId }).IsUnique();
        builder.HasOne(x => x.Assessment).WithMany(x => x.Grades).HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Student).WithMany(x => x.Grades).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}
