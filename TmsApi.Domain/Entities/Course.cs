namespace TmsApi.Domain.Entities;

public class Course
{
    public int Id { get; set; } // surrogate primary key — internal, used by foreign keys
    public required string Code { get; set; } // natural key — human-readable
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General Skills";
    public string Duration { get; set; } = "8 weeks";
    public string MinimumRequirements { get; set; } = "Basic computer literacy";
    public string Prerequisites { get; set; } = "None";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? EnrollmentStartDate { get; set; }
    public DateOnly? EnrollmentEndDate { get; set; }
    public string Status { get; set; } = "Published";
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
    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
}
