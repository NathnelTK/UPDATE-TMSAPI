using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration for the Course entity.
/// Defines table mapping, key constraints, property requirements, and relationships.
/// </summary>
public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        // --- Primary Key ---
        builder.HasKey(c => c.Id);

        // --- Property Constraints ---
        builder.Property(c => c.Code)
            .IsRequired()                          // NOT NULL
            .HasMaxLength(20);                     // Max length for course code (e.g., "CS-101")

        builder.Property(c => c.Title)
            .IsRequired()                          // NOT NULL
            .HasMaxLength(200);                    // Max length for course title

        builder.Property(c => c.MaxCapacity)
            .IsRequired();                         // NOT NULL

        // --- Unique Index on Natural Key ---
        builder.HasIndex(c => c.Code)
            .IsUnique()                            // Ensure no duplicate course codes
            .HasDatabaseName("IX_Courses_Code");

        // --- Relationship: Course -> Enrollments (one-to-many) ---
        // Configured in EnrollmentConfiguration to avoid duplication.
        // Course has the collection navigation property, Enrollment has the FK.
    }
}
