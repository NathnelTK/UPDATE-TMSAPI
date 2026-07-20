using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration for the Student entity.
/// Defines table mapping, key constraints, property requirements, and relationships.
/// </summary>
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        // --- Primary Key ---
        builder.HasKey(s => s.Id);

        // --- Property Constraints ---
        builder.Property(s => s.RegistrationNumber)
            .IsRequired()                          // NOT NULL
            .HasMaxLength(50);                     // Max length for the natural key

        builder.Property(s => s.Name)
            .IsRequired()                          // NOT NULL
            .HasMaxLength(200);                    // Max length for student name

        builder.Property(s => s.GPA)
            .HasColumnType("decimal(4,2)");            // Precision: 4 digits total, 2 after decimal (e.g., 3.99)

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);                // Default value for active status

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);               // Default: not deleted

        // --- Row Version / Concurrency Token (Exercise 8) ---
        // Npgsql maps IsRowVersion() to PostgreSQL's built-in xmin system column
        builder.Property(s => s.Version)
            .IsRowVersion();

        // --- Shadow Property: LastUpdated audit stamp (Exercise 8) ---
        builder.Property<DateTime>("LastUpdated")
            .HasDefaultValueSql("NOW()");          // Database-level default

        // --- Soft-Delete Query Filter (Exercise 9) ---
        // Automatically excludes soft-deleted students from normal queries
        builder.HasQueryFilter(s => !s.IsDeleted);

        // --- Unique Index on Natural Key ---
        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique()                            // Ensure no duplicate registration numbers
            .HasDatabaseName("IX_Students_RegistrationNumber");

        // --- Relationship: Student -> Enrollments (one-to-many) ---
        // Configured in EnrollmentConfiguration to avoid duplication.
        // Student has the collection navigation property, Enrollment has the FK.
    }
}
