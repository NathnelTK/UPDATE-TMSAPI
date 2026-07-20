using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration for the Assessment entity.
/// Defines table mapping, key constraints, property requirements, and relationships.
/// </summary>
public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        // --- Primary Key ---
        builder.HasKey(a => a.Id);

        // --- Property Constraints ---
        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.MaxScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(a => a.Weight)
            .HasColumnType("decimal(3,2)")
            .IsRequired();

        // --- Foreign Key: Assessment -> Course (many-to-one) ---
        builder.HasOne(a => a.Course)
            .WithMany(c => c.Assessments)          // Course has a collection of Assessments
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);     // If a course is deleted, its assessments are deleted too
            // Why Cascade? Assessments are owned by a course and have no meaning without it.
    }
}
