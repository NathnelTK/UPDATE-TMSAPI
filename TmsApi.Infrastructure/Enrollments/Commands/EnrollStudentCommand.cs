using MediatR;
using TmsApi.Application.Common;

namespace TmsApi.Enrollments.Commands;

/// <summary>
/// Command to enroll a student in a course by course code.
/// Returns Result<EnrollmentCreated, EnrollmentError> GÇö business outcomes are typed,
/// not thrown as exceptions.
/// </summary>
public record EnrollStudentCommand(int StudentId, string CourseCode)
    : IRequest<Result<EnrollmentCreated, EnrollmentError>>;

/// <summary>
/// Successful enrollment result GÇö returned when the student is enrolled.
/// </summary>
public record EnrollmentCreated(int EnrollmentId, int StudentId, string CourseCode);
