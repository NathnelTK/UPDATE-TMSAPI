using MediatR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Enrollments.Queries;

/// <summary>
/// Handles GetAllEnrollmentsQuery — projects every enrollment (with its student and
/// course display names) into the approval-queue DTO. Pending rows surface first so the
/// registrar sees outstanding work at the top; ties break on most-recent enrolment.
/// </summary>
public class GetAllEnrollmentsHandler(TmsDbContext context)
    : IRequestHandler<GetAllEnrollmentsQuery, List<EnrollmentListItemDto>>
{
    public async Task<List<EnrollmentListItemDto>> Handle(
        GetAllEnrollmentsQuery query, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .OrderBy(e => e.Status)          // Pending (0) first, then Approved, Rejected
            .ThenByDescending(e => e.EnrolledAt)
            .Select(e => new EnrollmentListItemDto(
                e.Id,
                e.StudentId,
                e.Student.Name,
                e.CourseId,
                e.Course.Code,
                e.Course.Title,
                e.Status.ToString(),
                e.EnrolledAt))
            .ToListAsync(ct);
    }
}
