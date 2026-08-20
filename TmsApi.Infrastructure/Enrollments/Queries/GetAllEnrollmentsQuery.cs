using MediatR;
using TmsApi.Application.DTOs;

namespace TmsApi.Enrollments.Queries;

/// <summary>
/// Query for the full enrollment approval queue. Reads have no business-rule failure
/// modes, so this returns raw DTOs (no Result wrapper), matching GetStudentScheduleQuery.
/// </summary>
public record GetAllEnrollmentsQuery() : IRequest<List<EnrollmentListItemDto>>;
