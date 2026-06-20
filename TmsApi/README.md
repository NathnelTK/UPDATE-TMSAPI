# TMS Web API — Module 5: Persistence Layer with Entity Framework Core & PostgreSQL

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791)](https://www.postgresql.org/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4)](https://learn.microsoft.com/ef/core/)

A **Training Management System (TMS)** Web API built with ASP.NET Core 10, Entity Framework Core 10, and PostgreSQL. This is the persistence layer evolution of the TMS project — data now survives restarts, system failures, and upgrades.

---

## 📦 Prerequisites

Before running this project, ensure you have the following installed:

### 1. .NET 10 SDK
```bash
dotnet --version
# Must return 10.x.y
```
Download from: [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

### 2. PostgreSQL 17+
```bash
psql --version
# Must return 17.x or later
```
Download from: [postgresql.org/download](https://www.postgresql.org/download/)

### 3. Entity Framework CLI Tool
```bash
dotnet tool install --global dotnet-ef
dotnet ef --version
# Must return 10.x.y matching your SDK
```

---

## 🚀 Quick Start (7 Steps)

### Step 1: Clone the Repository
```bash
git clone https://github.com/NathnelTK/UPDATE-TMSAPI.git
cd UPDATE-TMSAPI
git checkout m5-lab-session-1
```

### Step 2: (Optional — Highly Recommended) Create a PR
```bash
# Or create a Pull Request on GitHub from m5-lab-session-1 → main
```

### Step 3: Configure Your Database Password
The connection string file `appsettings.Development.json` is **gitignored** (never committed) for security. You need to create it or copy from the example below:

Create `TmsApi/appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "TmsDatabase": "Host=localhost;Database=TmsDb;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
  }
}
```
> ⚠️ Replace `YOUR_POSTGRES_PASSWORD` with the actual password you set during PostgreSQL installation.

### Step 4: Create the Database & Apply Migrations
```bash
cd TmsApi
dotnet ef database update
```
This runs both migrations:
1. **InitialCreate** — Creates `Students`, `Courses`, `Enrollments` tables
2. **AddAssessmentsAndCertificates** — Creates `Assessments`, `Certificates` tables with foreign keys
3. **RefineTmsModel** — Adds unique indexes, column constraints, FK delete behavior (Session 2)

### Step 5: Verify the Tables
```bash
psql -U postgres -d TmsDb -c "\dt"
```
Expected output:
```
          List of relations
 Schema |     Name      | Type  |  Owner
--------+---------------+-------+----------
 public | Assessments   | table | postgres
 public | Certificates  | table | postgres
 public | Courses       | table | postgres
 public | Enrollments   | table | postgres
 public | Students      | table | postgres
(5 rows)
```

### Step 6: Run the Application
```bash
dotnet run
```
The app starts on `http://localhost:5003` (configurable in `Properties/launchSettings.json`).

### Step 7: Test the Endpoints
```bash
# Session 1 — LINQ Experiments
curl http://localhost:5003/api/test/deferred
curl http://localhost:5003/api/test/translation-fail
curl http://localhost:5003/api/test/translation-resolved
curl http://localhost:5003/api/test/client-eval

# Session 1 — Registrar Business Queries
curl http://localhost:5003/api/registrar/queries/active-high-gpa-count
curl http://localhost:5003/api/registrar/queries/courses-by-enrollments
curl http://localhost:5003/api/registrar/queries/average-gpa-per-course
curl http://localhost:5003/api/registrar/queries/students-no-enrollments/subquery
curl http://localhost:5003/api/registrar/queries/students-no-enrollments/left-join

# Session 2 — Pagination & Top Courses
curl "http://localhost:5003/api/registrar/students/paged?page=1&pageSize=3"
curl http://localhost:5003/api/registrar/queries/top-courses
```

---

## 📚 Complete Code Explanation — Session 1 & Session 2

This section explains **every file** in the project **line-by-line** with the methods and concepts used.

---

## 🔷 SESSION 1: Make the Database Talk

### Session 1 Goal
Connect the existing TMS Web API to a PostgreSQL database using Entity Framework Core 10. Define database tables as C# classes, create migrations, and understand how LINQ queries are translated to SQL.

---

### 📁 Entities/Student.cs

```csharp
namespace TmsApi.Entities;

public class Student
{
    public int Id { get; set; }              // Surrogate primary key
    public required string RegistrationNumber { get; set; } // Natural key
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property: a student can have many enrollments
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    // Session 2 addition:
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
```

**Line-by-line explanation:**
- **`namespace TmsApi.Entities;`** — Organizes all entity classes under the Entities namespace (file-scoped namespace, C# 10 feature).
- **`public int Id`** — Surrogate primary key. EF Core by convention treats any property named `Id` as the primary key. PostgreSQL will auto-increment this (`SERIAL`/`IDENTITY` column).
- **`required string RegistrationNumber`** — Natural key: a human-readable identifier like "TMS-2026-0001". The `required` keyword (C# 11) enforces that this property must be set when creating a new Student object.
- **`required string Name`** — Student's full name, also required.
- **`decimal GPA`** — Grade point average. Stored as decimal for precision.
- **`bool IsActive = true`** — Whether the student is currently active. Defaults to `true`.
- **`ICollection<Enrollment> Enrollments`** — Navigation property. This tells EF Core that a Student has many Enrollments. EF Core uses this to generate JOINs in SQL when you LINQ-query across relationships.
- **`ICollection<Certificate> Certificates`** — Session 2 addition: navigation to Certificates.

**Key concept — Surrogate vs Natural keys:**
- `Id` is the **surrogate key** — internal, compact, never changes. Used by foreign keys.
- `RegistrationNumber` is the **natural key** — human-readable, unique. The unique constraint is configured in Session 2's Fluent API.

---

### 📁 Entities/Course.cs

```csharp
namespace TmsApi.Entities;

public class Course
{
    public int Id { get; set; }              // Surrogate primary key
    public required string Code { get; set; } // Natural key: "CS-101"
    public required string Title { get; set; }
    public int Capacity { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    // Session 2 additions:
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
```

**Line-by-line explanation:**
- Same pattern as Student — surrogate `Id`, natural `Code`, `Title`, `Capacity`.
- **`ICollection<Enrollment>`** — A course can have many enrollments (one-to-many).
- **`ICollection<Assessment>`** — Session 2: A course has many assessments (quizzes, exams).
- **`ICollection<Certificate>`** — Session 2: A course has many certificates issued to students.

---

### 📁 Entities/Enrollment.cs

```csharp
using System;

namespace TmsApi.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }        // Foreign key → Student.Id
    public int CourseId { get; set; }         // Foreign key → Course.Id
    public decimal? Grade { get; set; }       // Nullable: student may be currently enrolled
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // Navigation properties back to parent entities
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
```

**Line-by-line explanation:**
- **`int Id`** — Surrogate primary key for the enrollment record.
- **`int StudentId`** — Foreign key column. EF Core recognizes the pattern `[EntityName]Id` as a foreign key pointing to that entity's primary key.
- **`int CourseId`** — Foreign key pointing to Course.Id.
- **`decimal? Grade`** — Nullable (`?`) because the student may still be taking the course. Once graded, this gets a value.
- **`DateTime EnrolledAt = DateTime.UtcNow`** — When the enrollment was created. Defaults to UTC now.
- **`Student Student = null!`** — Navigation property to the related Student. `null!` tells the compiler "I know this will be null at initialization, but it will be set by EF Core when loaded from the database."
- **`Course Course = null!`** — Navigation property to the related Course.

**Key concept — The Join Table:**
Enrollment is the "join table" in a many-to-many relationship between Student and Course. Each student can enroll in many courses (multiple Enrollments), and each course can have many students (multiple Enrollments). The Enrollment table holds the foreign keys and additional data (Grade, EnrolledAt).

---

### 📁 Entities/Assessment.cs

```csharp
namespace TmsApi.Entities;

public class Assessment
{
    public int Id { get; set; }
    public required string Title { get; set; }  // e.g. "Midterm Exam"
    public decimal MaxScore { get; set; }        // e.g. 100.00
    public decimal Weight { get; set; }          // e.g. 0.30m = 30% of final grade

    public int CourseId { get; set; }            // FK → Course.Id
    public Course Course { get; set; } = null!;  // Navigation to the owning course
}
```

**Line-by-line:**
- Belongs to one Course (foreign key `CourseId`).
- `Title` is required (e.g., "Midterm Exam", "Final Project").
- `MaxScore` and `Weight` define the assessment's scoring parameters.

---

### 📁 Entities/Certificate.cs

```csharp
using System;

namespace TmsApi.Entities;

public class Certificate
{
    public int Id { get; set; }
    public required string SerialNumber { get; set; }  // Natural key
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public int StudentId { get; set; }   // FK → Student.Id
    public int CourseId { get; set; }    // FK → Course.Id
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
```

**Line-by-line:**
- Has two foreign keys: one to Student (who received it) and one to Course (what they completed).
- `SerialNumber` is the natural key — printed on the physical certificate.

---

### 📁 Data/TmsDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;

namespace TmsApi.Data;

public class TmsDbContext(DbContextOptions<TmsDbContext> options) : DbContext(options)
{
    // DbSet properties: each one represents a database table
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    // Session 2: Applies all Fluent API configurations automatically
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
    }
}
```

**Line-by-line explanation:**
- **`TmsDbContext(DbContextOptions<TmsDbContext> options) : DbContext(options)`** — Primary constructor (C# 12). Takes configuration options (like the connection string and provider) and passes them to the base DbContext class.
- **`DbSet<Student> Students => Set<Student>()`** — Each `DbSet<T>` property represents a database table. EF Core uses these to know which entity classes to include in the model. `Set<Student>()` is the expression-bodied method that returns the DbSet from the internal context state.
- **`DbSet<Course> Courses`**, **`DbSet<Enrollment> Enrollments`**, etc. — Same pattern for each entity. Without a DbSet property, EF Core ignores the class entirely (even if the file exists).
- **`OnModelCreating(ModelBuilder modelBuilder)`** — Session 2 addition. This method is called by EF Core when the model is being built for the first time. `ApplyConfigurationsFromAssembly` scans the assembly for any classes implementing `IEntityTypeConfiguration<T>` and applies them all automatically. This keeps the DbContext file clean — each entity's configuration is in its own file.

**Key concept — Why DbSet matters:**
A C# class only becomes a database table when it's registered as a `DbSet<T>` (or reachable via navigation properties). In Session 1, only Student, Course, and Enrollment were registered, so only 3 tables were created. The Extended Exercise added Assessment and Certificate DbSets, creating the final 5 tables.

---

### 📁 appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "TmsDatabase": "Host=localhost;Database=TmsDb;Username=postgres;Password=your_password"
  }
}
```

**Line-by-line:**
- **`ConnectionStrings.TmsDatabase`** — The connection string used by the DbContext. It specifies the PostgreSQL host, database name, username, and password.
- This file is in `.gitignore` so passwords are never committed to source control.

---

### 📁 Program.cs — DbContext Registration & SQL Logging

```csharp
// Register TmsDbContext with PostgreSQL provider and SQL logging
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information)          // Log SQL to console
        .EnableSensitiveDataLogging());                          // Show parameter values (dev only)
```

**Line-by-line explanation:**
- **`AddDbContext<TmsDbContext>`** — Registers the DbContext as a scoped service (created once per HTTP request). Scoped means each request gets a fresh context instance.
- **`UseNpgsql(...)`** — Configures the context to use the Npgsql provider, which translates EF Core commands to PostgreSQL-compatible SQL.
- **`.LogTo(Console.WriteLine, LogLevel.Information)`** — Tells EF Core to log all SQL queries at the Information level directly to the console. This is how you see the generated SQL in real-time.
- **`.EnableSensitiveDataLogging()`** — Shows parameter values in the logs (e.g., actual GPA values, names). ⚠️ Development only — never enable in production as it could leak sensitive data.

**Key concept — Database Provider Pattern:**
EF Core is database-agnostic. The same LINQ code can work with PostgreSQL (via Npgsql), SQL Server (via SqlServer provider), SQLite, etc. You just change the provider package and the `Use*` method.

---

### 📁 Program.cs — Auto-Seeder

```csharp
// Seed test data at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate(); // Apply pending migrations

    if (!context.Students.Any())
    {
        var students = new List<Student> { ... };      // 5 students
        context.Students.AddRange(students);
        var courses = new List<Course> { ... };         // 3 courses
        context.Courses.AddRange(courses);
        context.SaveChanges();                          // First save to get generated IDs

        var enrollments = new List<Enrollment> { ... }; // 4 enrollments
        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
    }
}
```

**Line-by-line explanation:**
- **`app.Services.CreateScope()`** — Creates a new DI scope to resolve scoped services (like DbContext) outside of a request context.
- **`context.Database.Migrate()`** — Applies any pending migrations automatically at startup. This means you don't need to manually run `dotnet ef database update` first. It also preserves the migration history table, unlike `EnsureCreated()`.
- **`if (!context.Students.Any())`** — Only seeds data if the Students table is empty. This prevents duplicate data on subsequent runs.
- **`AddRange(students)`** — Tracks multiple entities in the context for a single `SaveChanges()` call.
- **`SaveChanges()`** — Called twice: first to persist students and courses (so their IDs are generated), then to persist enrollments (which need those IDs for foreign keys).

**Why `Migrate()` and not `EnsureCreated()`?**
- `Database.Migrate()` respects the migration history. It applies pending migrations and updates the `__EFMigrationsHistory` table.
- `EnsureCreated()` creates the schema without recording any migration. Running `dotnet ef database update` after `EnsureCreated()` would fail with "table already exists".

---

### 📁 Controllers/TestController.cs — LINQ Experiments

```csharp
[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
```

#### Experiment 1: Deferred Execution

```csharp
[HttpGet("deferred")]
public IActionResult TestDeferred()
{
    Console.WriteLine(">>> STEP 1: Building the query object (no database contact)...");
    var query = context.Students.Where(s => s.GPA >= 3.0m);

    Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
    var orderedQuery = query.OrderBy(s => s.Name);

    Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
    var results = orderedQuery.ToList(); // Execution is triggered here

    Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
    return Ok(results);
}
```

**Line-by-line explanation:**
- **Step 1: `Where(s => s.GPA >= 3.0m)`** — This creates an `IQueryable<Student>` object. **No database call happens.** The Where clause is stored as an expression tree.
- **Step 2: `OrderBy(s => s.Name)`** — Appends sorting to the expression tree. Still no database call. The query is still just a recipe.
- **Step 3: `.ToList()`** — **This is where the SQL executes.** EF Core compiles the expression tree into `SELECT * FROM "Students" WHERE "GPA" >= 3.0 ORDER BY "Name"` and sends it to PostgreSQL. The results are loaded into memory as C# objects.
- **Console output order proves it:** Steps 1 and 2 complete instantly (no DB). Between Step 3 and Step 4, the database query and logs appear.

**Key concept — Deferred Execution:**
`IQueryable<T>` is just a description of what to query. The actual database call only happens when you "materialize" the results with `.ToList()`, `.First()`, `.CountAsync()`, `.SingleOrDefault()`, etc. This lets you build complex queries in multiple steps before executing.

#### Experiment 2: Translation Failure

```csharp
private static bool IsHonorRoll(decimal gpa) => gpa >= 3.5m;

[HttpGet("translation-fail")]
public IActionResult TestTranslationFail()
{
    try
    {
        var students = context.Students
            .Where(s => IsHonorRoll(s.GPA)) // C# method — can't translate!
            .ToList();
        return Ok(students);
    }
    catch (Exception ex)
    {
        return BadRequest(new { Message = ex.Message });
    }
}
```

**What happens:**
- EF Core tries to translate `IsHonorRoll(s.GPA)` into SQL but cannot. `IsHonorRoll` is compiled C# IL code — there's no PostgreSQL function equivalent.
- An `InvalidOperationException` is thrown: "The LINQ expression '...' could not be translated."

**Resolution pattern — Server-side (preferred):**
```csharp
var students = context.Students
    .Where(s => s.GPA >= 3.5m) // EF translates this to WHERE "GPA" >= 3.5
    .ToList();
```

**Resolution pattern — Client-side (performance warning):**
```csharp
var students = context.Students
    .AsEnumerable() // Pulls ALL rows into memory first
    .Where(s => IsHonorRoll(s.GPA)) // Then filters in C#
    .ToList();
```
⚠️ The SQL log shows `SELECT * FROM "Students"` — no WHERE clause. The entire table is loaded into RAM. For millions of rows, this would be disastrous.

**Key concept — Expression Trees vs IL Code:**
EF Core parses LINQ into an **expression tree** (AST), then translates AST nodes to SQL. Custom C# methods are compiled to IL, not expression trees, so EF Core cannot inspect or translate them. Always use inline expressions that EF Core can understand.

---

### 📁 Controllers/RegistrarController.cs — Business Queries

```csharp
[ApiController]
[Route("api/registrar")]
public class RegistrarController(TmsDbContext context) : ControllerBase
```

#### Query 1: Active students with high GPA

```csharp
[HttpGet("queries/active-high-gpa-count")]
public async Task<IActionResult> ActiveHighGpaCount()
{
    var count = await context.Students
        .Where(s => s.IsActive && s.GPA >= 3.0m)
        .CountAsync();

    return Ok(new { Description = "Active students with GPA >= 3.0", Count = count });
}
```

**Generated SQL:**
```sql
SELECT COUNT(*)::int4 FROM "Students" AS s
WHERE s."IsActive" AND s."GPA" >= 3.0
```

**Line-by-line:**
- **`async Task<IActionResult>`** — Async endpoint for non-blocking database operations.
- **`.Where(s => s.IsActive && s.GPA >= 3.0m)`** — Two filter conditions combined with AND. Both translate to SQL WHERE clause.
- **`.CountAsync()`** — Translates to `SELECT COUNT(*)`. The database counts rows — no data is transferred to the application. Only a single integer comes back.
- **`await`** — Releases the thread while waiting for the database response. The thread can serve other requests during this time (scalability).

#### Query 2: Courses by enrollment count

```csharp
[HttpGet("queries/courses-by-enrollments")]
public async Task<IActionResult> CoursesByEnrollments()
{
    var list = await context.Courses
        .Select(c => new {
            c.Title,
            EnrollmentCount = c.Enrollments.Count  // Count via navigation property
        })
        .OrderByDescending(x => x.EnrollmentCount)
        .ToListAsync();

    return Ok(new { Description = "Courses sorted by enrollment count (descending)", Results = list });
}
```

**Generated SQL:**
```sql
SELECT c."Title", (
    SELECT COUNT(*) FROM "Enrollments" e
    WHERE c."Id" = e."CourseId"
) AS "EnrollmentCount"
FROM "Courses" c
ORDER BY "EnrollmentCount" DESC
```

**Key technique — Navigation property in Select:**
`c.Enrollments.Count` uses the navigation property `Course.Enrollments`. EF Core translates this into a correlated subquery (or LEFT JOIN + GROUP BY). The COUNT and ORDER BY happen entirely in SQL, not in C#.

#### Query 3: Average GPA per course

```csharp
[HttpGet("queries/average-gpa-per-course")]
public async Task<IActionResult> AverageGpaPerCourse()
{
    var list = await context.Enrollments
        .GroupBy(e => e.Course.Title)
        .Select(g => new {
            Course = g.Key,
            AverageGPA = g.Average(e => e.Student.GPA)
        })
        .ToListAsync();

    return Ok(new { Description = "Average GPA per course", Results = list });
}
```

**Generated SQL:**
```sql
SELECT c."Title", AVG(s."GPA")
FROM "Enrollments" e
JOIN "Courses" c ON e."CourseId" = c."Id"
JOIN "Students" s ON e."StudentId" = s."Id"
GROUP BY c."Title"
```

**Line-by-line:**
- **`GroupBy(e => e.Course.Title)`** — Groups enrollments by the course title (traverses the Course navigation property). Translates to `GROUP BY`.
- **`g.Average(e => e.Student.GPA)`** — Within each group, calculates the average GPA (traverses the Student navigation property). Translates to `AVG(s."GPA")`.
- **`GroupBy` requires special rules:** After `GroupBy`, you can only use:
  - `g.Key` — the grouping value
  - Aggregate functions: `Count()`, `Average()`, `Sum()`, `Min()`, `Max()`
  - You cannot access individual entity properties (like `g.First().Name`)

#### Query 4: Students with zero enrollments

**Approach A — Subquery (NOT EXISTS):**
```csharp
var list = await context.Students
    .Where(s => !s.Enrollments.Any())
    .Select(s => s.Name)
    .ToListAsync();
```
```sql
SELECT s."Name" FROM "Students" AS s
WHERE NOT EXISTS (SELECT 1 FROM "Enrollments" e WHERE s."Id" = e."StudentId")
```

**Approach B — LEFT JOIN (IS NULL):**
```csharp
var list = await context.Students
    .LeftJoin(context.Enrollments,          // EF Core 10 LeftJoin method
        s => s.Id,                           // Student key
        e => e.StudentId,                    // Enrollment foreign key
        (s, e) => new { s, e })              // Combined result
    .Where(x => x.e == null)                 // No matching enrollment
    .Select(x => x.s.Name)
    .ToListAsync();
```
```sql
SELECT s."Name" FROM "Students" s
LEFT JOIN "Enrollments" e ON s."Id" = e."StudentId"
WHERE e."Id" IS NULL
```

**Key concept — Two approaches, same result:**
- **Subquery (Approach A):** Cleaner LINQ, generates `NOT EXISTS`. Often more readable.
- **Left Join (Approach B):** Uses `LeftJoin()` (EF Core 10 feature). Generates `LEFT JOIN ... WHERE ... IS NULL`. Sometimes performs better depending on the database.
- Both correctly execute in SQL — no rows are transferred to the application.

---

## 🔷 SESSION 2: Design the Schema

### Session 2 Goal
Evolve the schema with pagination, per-entity configuration classes, and deliberate relationship modeling.

---

### 📁 Controllers/RegistrarController.cs — Session 2 Additions

#### TODO 1: Paged Student List

```csharp
[HttpGet("students/paged")]
public async Task<IActionResult> GetPagedStudents(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    // Parameter validation
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 20;
    if (pageSize > 100) pageSize = 100;

    // Stable sort → offset → limit
    var students = await context.Students
        .OrderBy(s => s.Name)                           // MUST come before Skip/Take
        .Skip((page - 1) * pageSize)                    // Skip rows from previous pages
        .Take(pageSize)                                 // Take only the current page's rows
        .ToListAsync(cancellationToken);

    var totalCount = await context.Students
        .CountAsync(cancellationToken);

    return Ok(new {
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        Students = students
    });
}
```

**Generated SQL:**
```sql
SELECT ... FROM "Students"
ORDER BY "Name"
LIMIT 20 OFFSET 0
```

**Line-by-line explanation:**
- **`[FromQuery] int page = 1`** — Binds the `page` parameter from the URL query string (e.g., `/api/registrar/students/paged?page=2&pageSize=10`).
- **`CancellationToken cancellationToken = default`** — Passes the HTTP request cancellation token to EF Core. If the client disconnects, the database query is cancelled (saves resources).
- **`.OrderBy(s => s.Name)`** — **Critical: Must come before Skip/Take.** Without a stable sort order, PostgreSQL may return rows in any order between pages, causing duplicates or missing rows.
- **`.Skip((page - 1) * pageSize).Take(pageSize)`** — Standard offset pagination. Skip = `(page - 1) * pageSize` rows, then Take = `pageSize` rows.
- **`CountAsync()`** — A separate query to get the total count, needed for building pagination UI (total pages, next/previous buttons).
- **`(int)Math.Ceiling(totalCount / (double)pageSize)`** — Calculates total pages by rounding up.

**Key concept — Database-Level Pagination:**
The SQL uses `LIMIT` and `OFFSET`. The database only returns the rows for the requested page. Without `Skip/Take`, you'd be pulling all rows into memory and filtering in C# — wasteful for large datasets.

#### TODO 2: Top 5 Courses by Enrollment

```csharp
[HttpGet("queries/top-courses")]
public async Task<IActionResult> TopCoursesByEnrollment(
    CancellationToken cancellationToken = default)
{
    var topCourses = await context.Courses
        .Select(c => new {
            c.Title,
            EnrollmentCount = c.Enrollments.Count   // SQL subquery COUNT
        })
        .OrderByDescending(x => x.EnrollmentCount)   // Sort by count, highest first
        .Take(5)                                      // Only the top 5
        .ToListAsync(cancellationToken);

    return Ok(new {
        Description = "Top 5 courses by enrollment count",
        Results = topCourses
    });
}
```

**Generated SQL:**
```sql
SELECT c."Title", (
    SELECT COUNT(*) FROM "Enrollments" e
    WHERE c."Id" = e."CourseId"
) AS "EnrollmentCount"
FROM "Courses" c
ORDER BY "EnrollmentCount" DESC
LIMIT 5
```

**Key concept — Composition over Client Logic:**
The `OrderByDescending`, `Take(5)`, and `Count` all happen in SQL. The application only receives 5 rows with 2 columns each. For a database with 10,000 courses and millions of enrollments, this endpoint still returns instantly.

---

### 📁 Data/Configurations/ — Fluent API Configuration Classes

Session 2 introduces the **IEntityTypeConfiguration pattern** — each entity gets its own configuration class, keeping OnModelCreating clean.

#### 📁 StudentConfiguration.cs

```csharp
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);                                    // Primary key

        builder.Property(s => s.RegistrationNumber)
            .IsRequired()                                             // NOT NULL
            .HasMaxLength(50);                                        // Max 50 chars

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.GPA)
            .HasColumnType("decimal(4,2)");                               // e.g., 3.99

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);                                   // Default = true

        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique()                                               // No duplicate reg numbers
            .HasDatabaseName("IX_Students_RegistrationNumber");
    }
}
```

**Line-by-line explanation:**
- **`IEntityTypeConfiguration<Student>`** — Interface for configuring an entity. EF Core discovers and applies all implementations via `ApplyConfigurationsFromAssembly`.
- **`builder.HasKey(s => s.Id)`** — Explicitly sets the primary key. Not strictly needed here (Id is found by convention), but good practice.
- **`.IsRequired()`** — Adds `NOT NULL` constraint. `required` keyword on the property already enforces this in C#; `.IsRequired()` enforces it at the database level.
- **`.HasMaxLength(50)`** — Sets the column type to `VARCHAR(50)` instead of the default `TEXT`. Saves space and provides validation.
- **`.HasColumnType("decimal(4,2)")`** — Sets the SQL column type. `decimal(4,2)` means 4 digits total, 2 after the decimal (range: -99.99 to 99.99).
- **`.HasDefaultValue(true)`** — Sets a database-level default value for the column. If you insert a Student without setting `IsActive`, the database automatically sets it to `true`.
- **`.HasIndex(s => s.RegistrationNumber).IsUnique()`** — Creates a unique index on `RegistrationNumber`. This enforces uniqueness at the database level — no two students can have the same registration number.

#### 📁 CourseConfiguration.cs

```csharp
public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(20);              // "CS-101" fits in 20 chars

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.Code)
            .IsUnique()                     // No duplicate course codes
            .HasDatabaseName("IX_Courses_Code");
    }
}
```

**Same pattern as Student:**
- MaxLength constraints for string columns
- Unique index on the natural key (`Code`)

#### 📁 EnrollmentConfiguration.cs

```csharp
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Grade)
            .HasColumnType("decimal(4,2)");

        builder.Property(e => e.EnrolledAt)
            .HasDefaultValueSql("NOW()")    // Database function for current timestamp
            .IsRequired();

        // Relationship: Enrollment → Student (many-to-one)
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: Enrollment → Course (many-to-one)
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**Line-by-line explanation:**
- **`.HasDefaultValueSql("NOW()")`** — Uses a database function (`NOW()`) to set the default value. Unlike `.HasDefaultValue(DateTime.UtcNow)` which would bake in the app startup time, `NOW()` evaluates at row insertion time on the server. This ensures correct timestamps even if multiple rows are inserted.
- **`.HasOne(e => e.Student).WithMany(s => s.Enrollments)`** — Defines the one-to-many relationship:
  - `HasOne` = Enrollment has one Student (the dependent side)
  - `WithMany` = Student has many Enrollments (the principal side)
  - `HasForeignKey(e => e.StudentId)` = The FK column in the Enrollment table
- **`.OnDelete(DeleteBehavior.Restrict)`** — **Key decision:** Prevents deleting a Student or Course if they have related Enrollment records. If someone tries `DELETE FROM "Students" WHERE "Id" = 1` and that student has enrollments, PostgreSQL throws a foreign key violation error.

**Why Restrict and not Cascade?**
- **Cascade** — Deleting a Student would also delete all their Enrollments. This would destroy historical data.
- **SetNull** — Would set StudentId to NULL, breaking the relationship.
- **Restrict** — Prevents the deletion entirely. The application should deactivate students rather than deleting them. This preserves audit/historical records.

#### 📁 AssessmentConfiguration.cs

```csharp
builder.HasOne(a => a.Course)
    .WithMany(c => c.Assessments)
    .HasForeignKey(a => a.CourseId)
    .OnDelete(DeleteBehavior.Cascade);  // Assessments are owned by a course
```

**Why Cascade here?**
Assessments are **owned** by a course — they have no meaning without it. If a course is deleted, its assessments should be deleted too. This is safe because assessments don't have historical significance on their own.

#### 📁 CertificateConfiguration.cs

```csharp
builder.HasOne(c => c.Student)
    .WithMany(s => s.Certificates)
    .HasForeignKey(c => c.StudentId)
    .OnDelete(DeleteBehavior.Restrict);  // Preserve certificate records

builder.HasOne(c => c.Course)
    .WithMany(c => c.Certificates)
    .HasForeignKey(c => c.CourseId)
    .OnDelete(DeleteBehavior.Restrict);  // Preserve certificate records
```

**Why Restrict for Certificates?**
Certificates are important legal documents. Even if a student or course is deleted from the system, the certificate records should be preserved. The application should deactivate rather than delete.

---

### 📁 Migrations/20260620..._RefineTmsModel.cs

Generated by `dotnet ef migrations add RefineTmsModel`, this migration applies all the configuration changes from Session 2.

**Up() method applies:**
1. **Drops existing foreign keys** (to change their delete behavior)
2. **Alters column types** — sets max lengths (e.g., `VARCHAR(50)` for RegistrationNumber) and decimal precision (e.g., `numeric(4,2)` for GPA)
3. **Adds default values** — `NOW()` for timestamps, `true` for IsActive
4. **Creates unique indexes** — on RegistrationNumber, Code, SerialNumber
5. **Re-adds foreign keys** with `ON DELETE RESTRICT` (or `CASCADE` for Assessments)

**Down() method reverses** all the changes, restoring the old column types, removing indexes, and reverting to Cascade delete.

**Why separate migration file?**
Each migration captures a **change** to the model. Session 1 created the initial schema. Session 2 refines it. Keeping them separate means:
- You can review exactly what changed between sessions
- You can roll back Session 2 changes with `dotnet ef migrations remove` without losing Session 1 data
- Each migration has a clear purpose and name

---

## 🧪 Endpoint Reference

| Method | Endpoint | Session | Description |
|--------|----------|---------|-------------|
| GET | `/api/test/deferred` | 1 | LINQ deferred execution demo — builds query in steps, materializes at `.ToList()` |
| GET | `/api/test/translation-fail` | 1 | Throws on non-translatable C# method |
| GET | `/api/test/translation-resolved` | 1 | Inline lambda resolves cleanly |
| GET | `/api/test/client-eval` | 1 | `.AsEnumerable()` forces client-side evaluation |
| GET | `/api/registrar/queries/active-high-gpa-count` | 1 | `SELECT COUNT(*) ... WHERE IsActive AND GPA >= 3.0` |
| GET | `/api/registrar/queries/courses-by-enrollments` | 1 | Courses ranked by enrollment count (desc) |
| GET | `/api/registrar/queries/average-gpa-per-course` | 1 | `GROUP BY ... AVG(GPA)` |
| GET | `/api/registrar/queries/students-no-enrollments/subquery` | 1 | `NOT EXISTS` approach |
| GET | `/api/registrar/queries/students-no-enrollments/left-join` | 1 | `LEFT JOIN ... IS NULL` approach |
| GET | `/api/registrar/students/paged` | 2 | Paginated student list with `ORDER BY ... LIMIT ... OFFSET` |
| GET | `/api/registrar/queries/top-courses` | 2 | Top 5 courses by enrollment count |
| GET | `/api/enrollments` | — | List all enrollments |
| GET | `/api/enrollments/{id}` | — | Get enrollment by ID |
| POST | `/api/enrollments` | — | Create enrollment |
| DELETE | `/api/enrollments/{id}` | — | Delete enrollment |
| GET | `/api/assessments/results` | — | Secured sample endpoint (requires auth header) |
| GET | `/api/enrollments/worker-smoke` | — | Background worker smoke test |

---

## 🏗️ Project Structure (Session 2 Updated)

```
TmsApi/
├── Controllers/
│   ├── EnrollmentsController.cs        # CRUD for enrollments
│   ├── TestController.cs               # LINQ experiments (deferred, translation)
│   ├── RegistrarController.cs          # Business queries + pagination + top-courses
│   └── WeatherForecastController.cs
├── Data/
│   ├── Configurations/                 # NEW in Session 2 — Fluent API configs
│   │   ├── StudentConfiguration.cs    
│   │   ├── CourseConfiguration.cs      
│   │   ├── EnrollmentConfiguration.cs  
│   │   ├── AssessmentConfiguration.cs  
│   │   └── CertificateConfiguration.cs 
│   └── TmsDbContext.cs                 # EF Core database context + OnModelCreating
├── Entities/
│   ├── Student.cs                      # Student entity
│   ├── Course.cs                       # Course entity
│   ├── Enrollment.cs                   # Enrollment entity (many-to-many join)
│   ├── Assessment.cs                   # Assessment entity (belongs to Course)
│   └── Certificate.cs                  # Certificate entity (belongs to Student + Course)
├── Migrations/
│   ├── 20260618..._InitialCreate.cs                    # Session 1: 3 tables
│   └── 20260618..._AddAssessmentsAndCertificates.cs    # Session 1: +2 tables
│   └── 20260620..._RefineTmsModel.cs                  # Session 2: constraints, indexes, FKs
├── Properties/
│   └── launchSettings.json             # Launch configuration (gitignored)
├── Program.cs                          # App entry point, DI, middleware, seeder
├── EnrollmentService.cs                # Business logic (in-memory → DB)
├── EnrollmentWorker.cs                 # Background worker
├── TrainingAuthHandler.cs              # Custom authentication
├── RequestLoggingMiddleware.cs         # Request logging
├── TmsDatabaseException.cs             # Custom exception
├── PaymentOptions.cs                   # Options model
├── appsettings.json                    # Production settings (gitignored)
├── appsettings.Development.json        # Dev settings with connection string (gitignored)
└── TmsApi.csproj                       # Project file
```

---

## 🧠 Architecture Notes

### Entity Design Pattern
- **Surrogate keys** (`int Id`) — stable, auto-incremented primary keys used by foreign keys
- **Natural keys** (`RegistrationNumber`, `Code`, `SerialNumber`) — human-readable identifiers with unique indexes
- **Navigation properties** — enable EF Core to translate LINQ joins to SQL

### LINQ Execution Model (Session 1)
```
Build IQueryable ──► Append clauses ──► Materialize ──► SQL executes
  (no DB call)       (no DB call)       (.ToList())     (database)
```
- **IQueryable**: LINQ expressions compose an expression tree — NO database contact
- **Materialization**: `.ToList()`, `.CountAsync()`, `.FirstAsync()` trigger SQL execution
- **Translation**: EF Core translates expression trees to SQL — custom C# methods CANNOT be translated

### Fluent API Configuration Pattern (Session 2)
```
OnModelCreating
    └── ApplyConfigurationsFromAssembly()
            ├── StudentConfiguration.cs        # Key, columns, indexes, relationships
            ├── CourseConfiguration.cs         # Key, columns, indexes, relationships
            ├── EnrollmentConfiguration.cs     # Key, columns, relationships, delete behavior
            ├── AssessmentConfiguration.cs     # Key, columns, relationships
            └── CertificateConfiguration.cs    # Key, columns, indexes, relationships
```

### Delete Behavior Decisions (Session 2)
| Relationship | Behavior | Rationale |
|-------------|----------|-----------|
| Enrollment → Student | `Restrict` | Preserve historical enrollment records |
| Enrollment → Course | `Restrict` | Preserve historical enrollment records |
| Assessment → Course | `Cascade` | Assessments are owned by course, no standalone value |
| Certificate → Student | `Restrict` | Legal documents must be preserved |
| Certificate → Course | `Restrict` | Legal documents must be preserved |

### Seed Data
The app auto-seeds on first run if the `Students` table is empty:
- **5 Students**: Alice (GPA 3.8), Bob (2.9), Charlie (3.4, inactive), Diana (3.9), Evan (2.5)
- **3 Courses**: CS-101, CS-201, MAT-101
- **4 Enrollments**: Alice in CS-101 & CS-201, Bob in CS-101, Diana in CS-201

---

## 🔒 Security

- `appsettings.Development.json` is **gitignored** — your database password stays local
- Authentication is required for the `/api/assessments/results` endpoint (use header `Training-User: yourname`)
- SQL logging with sensitive data is **development-only** (`.EnableSensitiveDataLogging()`)
- Pagination endpoint caps page size at 100 to prevent abuse

---

## 📚 Key Technologies

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Application framework |
| ASP.NET Core | 10.0 | Web API & routing |
| Entity Framework Core | 10.0 | Object-relational mapping |
| Npgsql | 10.0 | PostgreSQL database provider |
| PostgreSQL | 17 | Relational database |
| Scalar | 2.16 | OpenAPI documentation UI |

---

## 🎯 Lab Checkpoints

### Session 1 Checkpoint
- [x] `dotnet ef database update` executes successfully
- [x] All 5 tables visible in PostgreSQL (`\dt`)
- [x] SQL logs show filters, aggregates, sorting, and joins running on the database server
- [x] Calling `.ToList()` before `.Where()` causes performance concern (client-side eval)
- [x] Assessments and Certificates wired through a named migration (not `EnsureCreated()`)
- [x] Deferred execution: SQL executes only at materialization (`.ToList()`)
- [x] Translation failure: custom C# methods cannot be translated to SQL
- [x] Business queries: COUNT, GROUP BY, subqueries, LEFT JOIN all run in SQL

### Session 2 Checkpoint
- [x] Paginated student endpoint logs SQL with `LIMIT 20 OFFSET 0`
- [x] Top-5 courses endpoint logs SQL with `GROUP BY` and `ORDER BY ... DESC LIMIT 5`
- [x] Five IEntityTypeConfiguration classes exist in `Data/Configurations/`
- [x] `OnModelCreating` contains only `ApplyConfigurationsFromAssembly()` — no giant config method
- [x] `OnDelete(DeleteBehavior.Restrict)` configured for Enrollment and Certificate FKs
- [x] Unique indexes on natural keys (RegistrationNumber, Code, SerialNumber)
- [x] Column type constraints (max lengths, decimal precision, database defaults)
- [x] Course entity has `ICollection<Assessment>` and `ICollection<Certificate>` navigation properties
- [x] Student entity has `ICollection<Certificate>` navigation property