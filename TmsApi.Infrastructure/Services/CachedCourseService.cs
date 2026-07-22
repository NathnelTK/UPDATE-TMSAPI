using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Common;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

/// <summary>
/// Cached course service with stampede protection using HybridCache.
/// Uses GetOrCreateAsync for atomic refetching - only one concurrent request
/// will hit the database while others wait for the cached result.
/// </summary>
public class CachedCourseService(
    HybridCache cache,
    TmsDbContext context,
    ILogger<CachedCourseService> logger)
    : ICachedCourseService
{
    public async Task<CourseResponseDto> GetCourseAsync(string code, CancellationToken ct)
    {
        var key = CacheKeys.Course(code);
        var dbHit = false;

        var dto = await cache.GetOrCreateAsync(
            key,
            (code, context),
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} - fetching from DB", key);

                var course = await state.context.Courses
                    .AsNoTracking()
                    .Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.Code == state.code, token);

                return course is null
                    ? throw new NotFoundException($"Course {state.code} not found.")
                    : new CourseResponseDto(
                        course.Id,
                        course.Code,
                        course.Title,
                        course.MaxCapacity,
                        course.Enrollments.Count);
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
            logger.LogInformation("Cache HIT for {Key}", key);

        return dto;
    }

    public async Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var list = await cache.GetOrCreateAsync(
            key,
            context,
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} - fetching from DB", key);

                var courses = await state.Courses
                    .AsNoTracking()
                    .Include(c => c.Enrollments)
                    .ToListAsync(token);

                return courses.Select(c => new CourseResponseDto(
                    c.Id,
                    c.Code,
                    c.Title,
                    c.MaxCapacity,
                    c.Enrollments.Count)).ToList();
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
            logger.LogInformation("Cache HIT for {Key}", key);

        return list;
    }

    public async Task<CourseResponseDto?> GetCourseByIdAsync(int id, CancellationToken ct)
    {
        var key = CacheKeys.CourseById(id);
        var dbHit = false;

        var dto = await cache.GetOrCreateAsync(
            key,
            (id, context),
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} - fetching from DB", key);

                var course = await state.context.Courses
                    .AsNoTracking()
                    .Where(c => c.Id == state.id)
                    .Select(c => new CourseResponseDto(
                        c.Id,
                        c.Code,
                        c.Title,
                        c.MaxCapacity,
                        c.Enrollments.Count))
                    .FirstOrDefaultAsync(token);

                return course;
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
            logger.LogInformation("Cache HIT for {Key}", key);

        return dto;
    }

    public async Task InvalidateCourseCacheAsync(CancellationToken ct)
    {
        logger.LogInformation("Invalidating cache tag {Tag}", CacheKeys.CoursesTag);
        await cache.RemoveByTagAsync(CacheKeys.CoursesTag, ct);
    }
}