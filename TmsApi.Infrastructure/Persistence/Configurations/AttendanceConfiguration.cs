using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.HasIndex(x => new { x.StudentId, x.CourseId, x.Date }).IsUnique();
        builder.HasOne(x => x.Student).WithMany(x => x.AttendanceRecords).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Course).WithMany(x => x.AttendanceRecords).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
    }
}
