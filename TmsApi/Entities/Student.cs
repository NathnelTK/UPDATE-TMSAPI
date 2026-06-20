namespace TmsApi.Entities;

public class Student
{
    public int Id { get; set; } // surrogate primary key — internal, used by foreign keys
    public required string RegistrationNumber { get; set; } // natural key — human-readable
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties for relationships
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
