using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

// M11 Session 1 - Exercise 2: inherit IdentityDbContext<TmsUser> so EF Core
// creates and manages the seven AspNet* Identity tables (users, roles, claims,
// tokens, logins, user-roles, user-claims) alongside the TMS entities.
public class TmsDbContext(DbContextOptions<TmsDbContext> options) : IdentityDbContext<TmsUser>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    // M11 Session 2 - Exercise 5: refresh tokens for JWT rotation + theft detection.
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Attendance> AttendanceRecords => Set<Attendance>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<GrantProgram> GrantPrograms => Set<GrantProgram>();
    public DbSet<GrantApplication> GrantApplications => Set<GrantApplication>();
    public DbSet<GrantAllocation> GrantAllocations => Set<GrantAllocation>();

    /// <summary>
    /// Applies all entity configurations found in the assembly containing TmsDbContext.
    /// This keeps OnModelCreating clean — each entity has its own configuration class.
    /// Configurations are discovered automatically via ApplyConfigurationsFromAssembly.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Discover and apply all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
    }
}
