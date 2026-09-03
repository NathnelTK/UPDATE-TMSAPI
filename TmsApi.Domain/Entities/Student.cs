namespace TmsApi.Domain.Entities;

public class Student
{
    public int Id { get; set; } // surrogate primary key — internal, used by foreign keys
    public required string RegistrationNumber { get; set; } // natural key — human-readable
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false; // Soft-delete flag for Exercise 9
    public string? UserId { get; set; }

    // Row version for concurrency (Exercise 8) — mapped to PostgreSQL xmin by Npgsql
    public uint Version { get; set; }

    // Navigation properties for relationships
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
