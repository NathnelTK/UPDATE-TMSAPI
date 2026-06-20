using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

/// <summary>
/// Fluent API configuration for the Certificate entity.
/// Defines table mapping, key constraints, property requirements, and relationships.
/// </summary>
public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        // --- Primary Key ---
        builder.HasKey(c => c.Id);

        // --- Property Constraints ---
        builder.Property(c => c.SerialNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.IssuedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // --- Unique Index on Natural Key ---
        builder.HasIndex(c => c.SerialNumber)
            .IsUnique()
            .HasDatabaseName("IX_Certificates_SerialNumber");

        // --- Foreign Key: Certificate -> Student (many-to-one) ---
        builder.HasOne(c => c.Student)
            .WithMany(s => s.Certificates)       // Student has a collection of Certificates
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict);  // Preserve certificate records even if student is deleted

        // --- Foreign Key: Certificate -> Course (many-to-one) ---
        builder.HasOne(c => c.Course)
            .WithMany(c => c.Certificates)       // Course has a collection of Certificates
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict);  // Preserve certificate records even if course is deleted
    }
}