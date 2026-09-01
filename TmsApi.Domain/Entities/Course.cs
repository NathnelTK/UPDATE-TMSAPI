namespace TmsApi.Domain.Entities;

public class Course
{
    public int Id { get; set; } // surrogate primary key — internal, used by foreign keys
    public required string Code { get; set; } // natural key — human-readable
    public required string Title { get; set; }
    public int MaxCapacity { get; set; }

    // --- M11 Session 3 - Exercise 5: lead instructor for resource-based authorization ---
    // Nullable: legacy/seeded courses have no assigned instructor. Stores the Identity
    // user id (ClaimTypes.NameIdentifier) of the lead instructor; CourseInstructorHandler
    // compares it against the caller's id so instructors can only edit their own courses.
    public string? InstructorId { get; set; }

    // Navigation properties for relationships
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
