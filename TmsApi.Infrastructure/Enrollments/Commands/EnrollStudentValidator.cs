using FluentValidation;

namespace TmsApi.Enrollments.Commands;

/// <summary>
/// Validates EnrollStudentCommand before the handler runs.
/// Validation failures throw ValidationException GÇö caught by GlobalExceptionHandler
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

        RuleFor(x => x.CourseCode).Matches(@"^[A-Z]{3}-\d{3}$")
            .WithMessage("Course code must follow the format XXX-000 (e.g., CSE-101).");
    }
}
