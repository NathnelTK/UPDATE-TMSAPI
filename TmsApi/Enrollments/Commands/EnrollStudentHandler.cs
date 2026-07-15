using MediatR;
using TmsApi.Common;
using TmsApi.Data;
using TmsApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace TmsApi.Enrollments.Commands;

/// <summary>
/// Handles EnrollStudentCommand with typed Result<T,E>.
/// Every domain failure is an early return — no exceptions for expected business outcomes.
/// </summary>
public class EnrollStudentHandler(
    TmsDbContext context,
    ILogger<EnrollStudentHandler> logger)
    : IRequestHandler<EnrollStudentCommand, Result<EnrollmentCreated, EnrollmentError>>
{
    public async Task<Result<EnrollmentCreated, EnrollmentError>> Handle(
        EnrollStudentCommand command, CancellationToken ct)
    {
        // Step 1: Find course by code — 404 if not found
        var course = await context.Courses
            .AsNoTracking()
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Code == command.CourseCode, ct);

        if (course is null)
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseNotFound(command.CourseCode));

        // Step 2: Check capacity — 409 if full
        if (course.Enrollments.Count >= course.MaxCapacity)
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.CourseFull(course.Title, course.MaxCapacity));

        // Step 3: Check for duplicate enrollment — 409 if already enrolled
        if (await context.Enrollments
                .AnyAsync(e => e.StudentId == command.StudentId && e.CourseId == course.Id, ct))
            return Result<EnrollmentCreated, EnrollmentError>.Failure(
                EnrollmentError.AlreadyEnrolled(command.StudentId, course.Code));

        // Step 4: Create enrollment
        var enrollment = new Enrollment
        {
            StudentId = command.StudentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Enrolled student {StudentId} in course {CourseCode} (enrollment {EnrollmentId})",
            command.StudentId, course.Code, enrollment.Id);

        return Result<EnrollmentCreated, EnrollmentError>.Success(
            new EnrollmentCreated(enrollment.Id, enrollment.StudentId, course.Code));
    }
}