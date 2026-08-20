using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration for the Enrollment entity.
/// Defines table mapping, key constraints, property requirements, and relationships.
/// </summary>
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        // --- Primary Key ---
        builder.HasKey(e => e.Id);

        // --- Property Constraints ---
        builder.Property(e => e.Grade)
            .HasColumnType("decimal(4,2)");            // Precision for grade values (e.g., 4.00)

        builder.Property(e => e.EnrolledAt)
            .HasDefaultValueSql("NOW()")           // Database-level default for enrollment timestamp
            .IsRequired();

        builder.Property(e => e.IsArchived)
            .HasDefaultValue(false);               // Default: not archived

        // Approval workflow status — stored as int with a DB default of Pending (0)
        // so rows created before this column existed read back as Pending.
        builder.Property(e => e.Status)
            .HasDefaultValue(EnrollmentStatus.Pending)
            .HasConversion<int>();

        // --- Foreign Key: Enrollment -> Student (many-to-one) ---
        builder.HasOne(e => e.Student)             // Each enrollment belongs to one student
            .WithMany(s => s.Enrollments)          // A student has many enrollments
            .HasForeignKey(e => e.StudentId)       // Foreign key property in Enrollment
            .OnDelete(DeleteBehavior.Restrict);    // Prevent deleting a student who has enrollments
            // Why Restrict? If a student is deleted, their enrollment records should be preserved
            // for historical/audit purposes. The application must handle student deactivation instead.

        // --- Foreign Key: Enrollment -> Course (many-to-one) ---
        builder.HasOne(e => e.Course)              // Each enrollment belongs to one course
            .WithMany(c => c.Enrollments)          // A course has many enrollments
            .HasForeignKey(e => e.CourseId)        // Foreign key property in Enrollment
            .OnDelete(DeleteBehavior.Restrict);    // Prevent deleting a course that has enrollments
            // Why Restrict? Deleting a course would orphan enrollment records. The registrar
            // should deactivate courses rather than delete them if enrollments exist.
    }
}
