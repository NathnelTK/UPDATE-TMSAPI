using Microsoft.AspNetCore.Authorization;

namespace TmsApi.Api.Authorization;

/// <summary>
/// M11 Session 3 - Exercise 5 Step 1: marker requirement for resource-based
/// authorization. Carries no data — it simply names the rule ("the caller may edit
/// this course") that <see cref="CourseInstructorHandler"/> evaluates against a
/// specific <see cref="TmsApi.Domain.Entities.Course"/> resource.
/// </summary>
public class CourseInstructorRequirement : IAuthorizationRequirement { }
