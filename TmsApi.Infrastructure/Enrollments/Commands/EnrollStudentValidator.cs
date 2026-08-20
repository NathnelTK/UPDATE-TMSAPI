using FluentValidation;

namespace TmsApi.Enrollments.Commands;

/// <summary>
/// Validates EnrollStudentCommand before the handler runs.
/// Validation failures throw ValidationException G�� caught by GlobalExceptionHandler
/// and translated to 400 Bad Request with field-level errors.
/// </summary>
public class EnrollStudentValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0)
            .WithMessage("Student ID must be a positive number.");

        RuleFor(x => x.CourseCode).NotEmpty()
            .WithMessage("Course code is required.");

        // Real course prefixes vary in length (CS, CSE, MATH), so accept 2–4
        // uppercase letters. This keeps every seeded course (CS-101, CSE-101,
        // MAT-101) enrollable instead of rejecting the 2-letter ones at the gate.
        RuleFor(x => x.CourseCode).Matches(@"^[A-Z]{2,4}-\d{3}$")
            .WithMessage("Course code must follow the format LL[LL]-000 (e.g., CS-101 or CSE-101).");
    }
}
