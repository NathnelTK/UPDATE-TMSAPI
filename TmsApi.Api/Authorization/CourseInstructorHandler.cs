using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Authorization;

/// <summary>
/// M11 Session 3 - Exercise 5 Step 2: resource-based authorization handler.
/// Role checks alone ([Authorize(Roles = "Instructor")]) cannot express "an
/// instructor may edit ONLY their own courses" — that decision depends on the
/// specific course being edited. This handler receives the <see cref="Course"/>
/// resource and grants the "CanEditCourse" policy when:
///   * the caller is an Admin (may manage any course), or
///   * the caller is an Instructor whose id matches the course's InstructorId.
/// Otherwise it stays silent and the policy fails → 403 Forbidden.
/// </summary>
public class CourseInstructorHandler
    : AuthorizationHandler<CourseInstructorRequirement, Course>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CourseInstructorRequirement requirement,
        Course resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isInstructor = context.User.IsInRole("Instructor");
        var isAdmin = context.User.IsInRole("Admin");

        // Admins can manage any course.
        if (isAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Instructors can only manage courses where InstructorId matches their user id.
        if (isInstructor && resource.InstructorId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
