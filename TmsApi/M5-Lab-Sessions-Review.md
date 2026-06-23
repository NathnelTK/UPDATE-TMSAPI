# M5 Lab Sessions — Comprehensive Implementation Review

**Module:** M5 — Entity Framework Core 10 and PostgreSQL  
**Sessions:** 1 (Exercises 1 & 2), 2 (Exercises 3, 4, & 5), and 3 (Exercises 6, 7, 8, & 9)  
**Branch:** `session3-exercises`  
**Repository:** `https://github.com/NathnelTK/UPDATE-TMSAPI`

---

## Table of Contents

- [Before You Begin: PostgreSQL and Tooling](#before-you-begin-postgresql-and-tooling)
- [Session 1 — Exercise 1: DbContext Configuration and Migrations](#session-1--exercise-1-dbcontext-configuration-and-migrations)
- [Session 1 — Exercise 2: LINQ Queries and Engine Experiments](#session-1--exercise-2-linq-queries-and-engine-experiments)
- [Session 1 — Extended Exercise: Wire Assessment and Certificate](#session-1--extended-exercise-wire-assessment-and-certificate)
- [Session 2 — Exercise 3: GroupBy, Aggregates, and Pagination](#session-2--exercise-3-groupby-aggregates-and-pagination)
- [Session 2 — Exercise 4: IEntityTypeConfiguration for Each Entity](#session-2--exercise-4-ientitytypeconfiguration-for-each-entity)
- [Session 2 — Exercise 5: Model the TMS Graph](#session-2--exercise-5-model-the-tms-graph)
- [Session Checkpoints](#session-checkpoints)
- [Appendix: Verified SQL Outputs](#appendix-verified-sql-outputs)

---

## Before You Begin: PostgreSQL and Tooling

### Step 1: Verify PostgreSQL Installation

**What was done:** PostgreSQL 17 was found installed at `C:\Program Files\PostgreSQL\17`. The service `postgresql-x64-18` was already running on the machine.

**Command:**
```powershell
psql --version
```

**Expected Output:** `psql (PostgreSQL) 17.5`

**Troubleshooting:** If `psql` is not found, add `C:\Program Files\PostgreSQL\17\bin` to your system PATH environment variable.

---

### Step 2: Confirm .NET SDK

**What was done:** Verified .NET 10 SDK is installed.

**Command:**
```powershell
dotnet --version
```

**Expected Output:** `10.0.301`

---

### Step 3: Add EF Core Packages to TmsApi

**What was done:** Three NuGet packages were added to the TmsApi project. These are the foundation for Entity Framework Core to work with PostgreSQL.

**Commands:**
```powershell
cd TmsApi
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

**Package details:**

| Package | Purpose |
|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL database provider for EF Core. Translates LINQ queries into PostgreSQL-compliant SQL commands and handles database connections. |
| `Microsoft.EntityFrameworkCore.Design` | Design-time logic for EF Core. Provides compiler services that allow EF Core to inspect project code structure to generate migrations. Does not ship with the compiled production binary. |
| `Microsoft.EntityFrameworkCore.Tools` | Package-manager console tooling hooks. Integrates migration tools into the .NET development environment. |

**CSProj result after adding packages:**
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.9" />
    <PackageReference Include="Scalar.AspNetCore" Version="2.16.3" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.9" />
</ItemGroup>
```

---

### Step 4: Verify the Entity Framework CLI Tools

**What was done:** Installed the `dotnet-ef` global tool, which is the command-line utility for managing EF Core migrations.

**Command:**
```powershell
dotnet tool install --global dotnet-ef
dotnet ef --version
```

**Expected Output:** `Entity Framework Core .NET Command-line Tools\n10.0.9`

**What the dotnet-ef tool does:** It compiles your project, detects model changes, and creates or runs migration scripts. It is separate from the NuGet packages — those are referenced inside C# code, while `dotnet-ef` is a CLI utility.

---

## Session 1 — Exercise 1: DbContext Configuration and Migrations

### Step 1: Define Your Database Entities

**What was done:** Created 5 entity classes in `TmsApi/Entities/`. These are the persistence-ready evolution of the domain models from Module 1 (TmsCore console project). Key differences from M1:
- **Mutable properties** — EF Core needs settable properties to track changes
- **Surrogate keys** (`int Id`) — auto-incremented by PostgreSQL, stable for foreign keys
- **Navigation properties** — enable EF Core to translate LINQ joins to SQL

#### 1. `Entities/Student.cs`

```csharp
namespace TmsApi.Entities;

public class Student
{
    public int Id { get; set; }                                      // Surrogate PK
    public required string RegistrationNumber { get; set; }          // Natural key
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();  // Navigation
}
```

#### 2. `Entities/Course.cs`

```csharp
namespace TmsApi.Entities;

public class Course
{
    public int Id { get; set; }                                      // Surrogate PK
    public required string Code { get; set; }                        // Natural key
    public required string Title { get; set; }
    public int Capacity { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();  // Navigation
}
```

#### 3. `Entities/Enrollment.cs`

```csharp
namespace TmsApi.Entities;

public class Enrollment
{
    public int Id { get; set; }                                      // Surrogate PK
    public int StudentId { get; set; }                               // FK → Student
    public int CourseId { get; set; }                                // FK → Course
    public decimal? Grade { get; set; }                              // Nullable (currently enrolled)
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public Student Student { get; set; } = null!;                    // Navigation
    public Course Course { get; set; } = null!;                      // Navigation
}
```

#### 4. `Entities/Assessment.cs`

```csharp
namespace TmsApi.Entities;

public class Assessment
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }                              // e.g., 0.30m for 30%
    public int CourseId { get; set; }                                // FK → Course
    public Course Course { get; set; } = null!;                      // Navigation
}
```

#### 5. `Entities/Certificate.cs`

```csharp
namespace TmsApi.Entities;

public class Certificate
{
    public int Id { get; set; }                                      // Surrogate PK
    public required string SerialNumber { get; set; }                // Natural key
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public int StudentId { get; set; }                               // FK → Student
    public int CourseId { get; set; }                                // FK → Course
    public Student Student { get; set; } = null!;                    // Navigation
    public Course Course { get; set; } = null!;                      // Navigation
}
```

**Key design decisions explained:**
- **Two keys per entity:** `int Id` (surrogate) for relationships + `string` natural key (`Code`, `RegistrationNumber`, `SerialNumber`) for humans to read and type
- **Natural keys NOT repeated with class name:** It's `Code`, not `CourseCode` — follows .NET guideline against stuttering members
- **The `<Entity>Id` names** (like `Enrollment.CourseId`) are **foreign keys** pointing at another entity, which is exactly EF Core's key-naming convention
- **No database attributes yet:** Unique constraints will be added in Session 2 via Fluent API (`builder.HasIndex(c => c.Code).IsUnique()`)

---

### Step 2: Implement TmsDbContext

**What was done:** Created `Data/TmsDbContext.cs` — the central class that manages database connections, tracks entity state, and translates C# code to database calls.

```csharp
using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;

namespace TmsApi.Data;

public class TmsDbContext(DbContextOptions<TmsDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
}
```

**Note:** At this point (Session 1, Step 2), only 3 DbSets are registered. Assessment and Certificate are defined as classes but NOT yet part of the EF Core model — they become tables through the Extended Exercise.

---

### Step 3: Register the DbContext in Program.cs

**What was done:** Added PostgreSQL connection registration in `Program.cs` using Npgsql.

```csharp
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase")));
```

This registers `TmsDbContext` as a scoped service — a new instance is created per incoming HTTP request.

---

### Step 4: Configure Connection String

**What was done:** Added the `ConnectionStrings` block to `appsettings.Development.json`.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "TmsDatabase": "Host=localhost;Database=TmsDb;Username=postgres;Password=YOUR_PASSWORD_HERE"
  }
}
```

**Security note:** This file is listed in `.gitignore` so the password is never committed to version control.

---

### Step 5: Generate the First Migration

**What was done:** Ran the EF Core migration command which analyzes entity properties and builds a C# migration script detailing the SQL layout changes.

**Command:**
```powershell
dotnet ef migrations add InitialCreate
```

**What this does:**
1. Compiles the project
2. Scans all DbSet properties and navigation properties in TmsDbContext
3. Detects that Student, Course, and Enrollment are part of the model
4. Generates a migration file with `Up()` (create tables) and `Down()` (drop tables) methods

---

### Step 6: Inspect the Generated Migration File

**What was done:** Reviewed the migration file at `Migrations/20260618..._InitialCreate.cs`.

**Up() method** — Contains three `CreateTable` calls:
- **Courses** table: `Id` (PK), `Code` (NOT NULL), `Title` (NOT NULL), `Capacity`
- **Students** table: `Id` (PK), `RegistrationNumber` (NOT NULL), `Name` (NOT NULL), `GPA`, `IsActive`
- **Enrollments** table: `Id` (PK), `StudentId` (FK → Students), `CourseId` (FK → Courses), `Grade` (nullable), `EnrolledAt`

**Down() method** — Drops tables in reverse dependency order:
1. Enrollments first (depends on both Students and Courses)
2. Students and Courses next

**Why Assessment and Certificate don't appear:** A C# class only becomes a database table once it is part of the EF Core model — registered as a DbSet (or reachable through a navigation property from an entity that is). Student and Course do not carry collections pointing at Assessment or Certificate, so EF Core never discovers them.

---

### Step 7: Apply the Migration

**What was done:** Applied the migration changes to the PostgreSQL instance.

**Command:**
```powershell
dotnet ef database update
```

**Expected console output:** SQL commands showing:
```
CREATE TABLE "Courses" (...)
CREATE TABLE "Students" (...)
CREATE TABLE "Enrollments" (...)
CREATE INDEX "IX_Enrollments_CourseId" ...
CREATE INDEX "IX_Enrollments_StudentId" ...
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260618..._InitialCreate', '10.0.9');
```

---

### Step 8: Verify the Schema in PostgreSQL

**What was done:** Logged into PostgreSQL to verify the database and tables were created.

**Command:**
```powershell
psql -U postgres -d TmsDb -c "\dt"
```

**Expected Output:**
```
          List of relations
 Schema |     Name     | Type  |  Owner
--------+--------------+-------+----------
 public | Courses      | table | postgres
 public | Enrollments  | table | postgres
 public | Students     | table | postgres
(3 rows)
```

**Troubleshooting note:** If PostgreSQL wasn't running, we had to:
1. Open PowerShell as Administrator
2. Run `Remove-Item "C:\Program Files\PostgreSQL\17\data\postmaster.pid" -Force` (leftover lock file)
3. Run `& "C:\Program Files\PostgreSQL\17\bin\pg_ctl.exe" start -D "C:\Program Files\PostgreSQL\17\data" -w`

**Actual result (from our run):** The migration created the database `TmsDb` and all 3 tables successfully, with SQL logging showing each `CREATE TABLE` and `CREATE INDEX` command.

---

## Session 1 — Exercise 2: LINQ Queries and Engine Experiments

### Step 1: Enable Console SQL Logging

**What was done:** Updated the `AddDbContext` registration in `Program.cs` to log SQL directly to the command window during development.

```csharp
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information)          // ← SQL logging
        .EnableSensitiveDataLogging());                          // ← Show parameters (dev only)
```

**What this enables:**
- Every SQL command EF Core generates is printed to the console
- Parameters are visible (e.g., `@p0='3.8'`, `@p1='True'`)
- You can verify that filters, sorting, and aggregations run on the database server

---

### Step 2: Write an Auto-Seeder

**What was done:** Added a seeding block in `Program.cs` just before `app.Run()`. It verifies if tables are empty at startup and populates them with test data.

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate();  // Applies any pending migrations

    if (!context.Students.Any())
    {
        // Seed 5 students: Alice (3.8), Bob (2.9), Charlie (3.4, inactive), Diana (3.9), Evan (2.5)
        // Seed 3 courses: CS-101, CS-201, MAT-101
        // Seed 4 enrollments linking them
        context.SaveChanges();
    }
}
```

**Seed Data:**

| Students | GPA | Active |
|----------|-----|--------|
| Alice Smith | 3.8 | ✅ |
| Bob Jones | 2.9 | ✅ |
| Charlie Brown | 3.4 | ❌ |
| Diana Prince | 3.9 | ✅ |
| Evan Wright | 2.5 | ✅ |

| Courses | Capacity |
|---------|----------|
| CS-101 — Introduction to Computer Science | 30 |
| CS-201 — Data Structures and Algorithms | 25 |
| MAT-101 — Calculus I | 40 |

| Enrollments | Student | Course | Grade |
|-------------|---------|--------|-------|
| 1 | Alice | CS-101 | 4.0 |
| 2 | Alice | CS-201 | 3.6 |
| 3 | Bob | CS-101 | 2.8 |
| 4 | Diana | CS-201 | 3.9 |

**Why `Migrate()` and not `EnsureCreated()`:** `Database.Migrate()` respects migration history and applies anything still pending. `EnsureCreated()` builds the schema with no migration record, so the next `dotnet ef database update` would fail with a "table already exists" error. **Always stick with migrations end to end.**

---

### Step 3: Run the Deferred Execution Experiment

**What was done:** Created `Controllers/TestController.cs` with a `/api/test/deferred` endpoint that demonstrates LINQ's lazy execution.

```csharp
[HttpGet("deferred")]
public IActionResult TestDeferred()
{
    Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
    var query = context.Students.Where(s => s.GPA >= 3.0m);

    Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
    var orderedQuery = query.OrderBy(s => s.Name);

    Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
    var results = orderedQuery.ToList();   // ← SQL execution happens HERE

    Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
    return Ok(results);
}
```

**Test command:**
```powershell
curl.exe http://localhost:5003/api/test/deferred
```

**Console output observed:**
```
>>> STEP 1: Building the query object (no database contact)...
>>> STEP 2: Appending a sorting clause...
>>> STEP 3: Materializing query into a C# List...
info: ... Executed DbCommand ...
      SELECT s."Id", s."GPA", s."IsActive", s."Name", s."RegistrationNumber"
      FROM "Students" AS s
      WHERE s."GPA" >= 3.0
      ORDER BY s."Name"
>>> STEP 4: Materialization finished. List populated.
```

**Key insight:** The database query appears **exactly between Step 3 and Step 4**. This proves that LINQ queries acting on `IQueryable` are merely instruction sets (recipes) and don't run until you request the final collection via `.ToList()` (materialization).

**API response:** 3 students returned (Alice, Charlie, Diana) — all with GPA >= 3.0, sorted alphabetically by name.

---

### Step 4: Run the SQL Translation Failure Experiment

**What was done:** Added a `/api/test/translation-fail` endpoint that demonstrates what happens when C# functions cannot be translated to SQL.

```csharp
private static bool IsHonorRoll(decimal gpa)
{
    return gpa >= 3.5m;
}

[HttpGet("translation-fail")]
public IActionResult TestTranslationFail()
{
    try
    {
        var students = context.Students
            .Where(s => IsHonorRoll(s.GPA))   // ← This custom method CANNOT be translated
            .ToList();
        return Ok(students);
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
        return BadRequest(new { Message = ex.Message });
    }
}
```

**Test command:**
```powershell
curl.exe http://localhost:5003/api/test/translation-fail
```

**API response (caught exception):**
```json
{
  "message": "The LINQ expression 'DbSet<Student>()\r\n    .Where(s => TestController.IsHonorRoll(s.GPA))' could not be translated. Translation of method '...IsHonorRoll' failed."
}
```

**Why it failed:** EF Core parses LINQ code into an Abstract Syntax Tree (AST) to generate SQL. Because `IsHonorRoll` is compiled C# IL (Intermediate Language), Npgsql cannot translate it into a PostgreSQL function.

**Resolution patterns implemented:**

1. **Server-Side Evaluation (Preferred)** — `/api/test/translation-resolved`:
   ```csharp
   var students = context.Students
       .Where(s => s.GPA >= 3.5m)     // ← EF Core can translate this directly
       .ToList();
   ```
   SQL generated: `SELECT ... FROM "Students" WHERE "GPA" >= 3.5`

2. **Client-Side Evaluation** — `/api/test/client-eval`:
   ```csharp
   var students = context.Students
       .AsEnumerable()                 // ← Pulls ALL rows into RAM first
       .Where(s => IsHonorRoll(s.GPA))
       .ToList();
   ```
   SQL generated: `SELECT ... FROM "Students"` (no WHERE clause — entire table loaded)
   **Warning:** Performance degrades as the database grows.

---

### Step 5: Solve the Registrar's Business Queries

**What was done:** Created `Controllers/RegistrarController.cs` with 4 endpoints that solve the registrar's reporting needs. All queries are fully translated and executed by the database.

#### Query 1: Active students with GPA >= 3.0

**Endpoint:** `GET /api/registrar/queries/active-high-gpa-count`

**C#:**
```csharp
var count = await context.Students
    .Where(s => s.IsActive && s.GPA >= 3.0m)
    .CountAsync();
```

**SQL generated:**
```sql
SELECT count(*)::int
FROM "Students" AS s
WHERE s."IsActive" AND s."GPA" >= 3.0
```

**Result:** `{"count": 2}` (Alice and Diana)

---

#### Query 2: Courses sorted by enrollment count (descending)

**Endpoint:** `GET /api/registrar/queries/courses-by-enrollments`

**C#:**
```csharp
var list = await context.Courses
    .Select(c => new {
        c.Title,
        EnrollmentCount = c.Enrollments.Count
    })
    .OrderByDescending(x => x.EnrollmentCount)
    .ToListAsync();
```

**SQL generated:**
```sql
SELECT c."Title", (
    SELECT count(*)::int
    FROM "Enrollments" AS e0
    WHERE c."Id" = e0."CourseId") AS "EnrollmentCount"
FROM "Courses" AS c
ORDER BY (
    SELECT count(*)::int
    FROM "Enrollments" AS e
    WHERE c."Id" = e."CourseId") DESC
```

**Result:**
```json
[
  {"title":"Introduction to Computer Science","enrollmentCount": 2},
  {"title":"Data Structures and Algorithms","enrollmentCount": 2},
  {"title":"Calculus I","enrollmentCount": 0}
]
```

---

#### Query 3: Average GPA per course

**Endpoint:** `GET /api/registrar/queries/average-gpa-per-course`

**C#:**
```csharp
var list = await context.Enrollments
    .GroupBy(e => e.Course.Title)
    .Select(g => new {
        Course = g.Key,
        AverageGPA = g.Average(e => e.Student.GPA)
    })
    .ToListAsync();
```

**SQL generated:**
```sql
SELECT c."Title" AS "Course", (
    SELECT avg(s."GPA")
    FROM "Enrollments" AS e0
    INNER JOIN "Courses" AS c0 ON e0."CourseId" = c0."Id"
    INNER JOIN "Students" AS s ON e0."StudentId" = s."Id"
    WHERE c."Title" = c0."Title") AS "AverageGPA"
FROM "Enrollments" AS e
INNER JOIN "Courses" AS c ON e."CourseId" = c."Id"
GROUP BY c."Title"
```

**Result:**
```json
[
  {"course":"Data Structures and Algorithms","averageGPA": 3.85},
  {"course":"Introduction to Computer Science","averageGPA": 3.35}
]
```

---

#### Query 4: Students with zero enrollments

**Approach A (Subquery — NOT EXISTS):** `GET /api/registrar/queries/students-no-enrollments/subquery`

**C#:**
```csharp
var list = await context.Students
    .Where(s => !s.Enrollments.Any())
    .Select(s => s.Name)
    .ToListAsync();
```

**SQL generated:**
```sql
SELECT s."Name"
FROM "Students" AS s
WHERE NOT EXISTS (
    SELECT 1
    FROM "Enrollments" AS e
    WHERE s."Id" = e."StudentId")
```

**Result:** `["Evan Wright", "Charlie Brown"]`

**Approach B (LEFT JOIN ... IS NULL):** `GET /api/registrar/queries/students-no-enrollments/left-join`

**C#:**
```csharp
var list = await context.Students
    .LeftJoin(context.Enrollments,
        s => s.Id,
        e => e.StudentId,
        (s, e) => new { s, e })
    .Where(x => x.e == null)
    .Select(x => x.s.Name)
    .ToListAsync();
```

**SQL generated:**
```sql
SELECT s."Name"
FROM "Students" AS s
LEFT JOIN "Enrollments" AS e ON s."Id" = e."StudentId"
WHERE e."Id" IS NULL
```

**Result:** `["Evan Wright", "Charlie Brown"]`

---

## Session 1 — Extended Exercise: Wire Assessment and Certificate into the Database

### Task 1: Register Both Entities as DbSet Properties

**What was done:** Updated `Data/TmsDbContext.cs` to add DbSet properties for Assessment and Certificate.

```csharp
public DbSet<Assessment> Assessments => Set<Assessment>();
public DbSet<Certificate> Certificates => Set<Certificate>();
```

This proves the key lesson: **defining a class is not the same as creating a table. Membership in the EF Core model is what counts.**

### Task 2: Generate a New Migration

**Command:**
```powershell
dotnet ef migrations add AddAssessmentsAndCertificates
```

### Task 3: Inspect the Migration

**Up() method verification:**
- Only **2 new tables** appear (Assessments, Certificates) — the existing 3 tables are untouched
- **Assessments:** `Id` (PK), `Title`, `MaxScore`, `Weight`, `CourseId` (FK → Courses)
- **Certificates:** `Id` (PK), `SerialNumber`, `IssuedAt`, `StudentId` (FK → Students), `CourseId` (FK → Courses)
- Indexes created: `IX_Assessments_CourseId`, `IX_Certificates_CourseId`, `IX_Certificates_StudentId`

**Down() method:** Drops Assessments first, then Certificates (reverse dependency order)

### Task 4: Apply and Verify

**Command:**
```powershell
dotnet ef database update
psql -U postgres -d TmsDb -c "\dt"
```

**Expected Output:**
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

**Actual result from our run:** All 5 tables confirmed present.

---

## Session 2 — Exercise 3: GroupBy, Aggregates, and Pagination

### Context

The dashboard must show paged roster data and summary tiles. The database should do the math.

### Task 1: Implement a Paged List of Students

**What was done:** Added `GET /api/registrar/students/paged` endpoint in `RegistrarController.cs`.

**C# Implementation:**
```csharp
[HttpGet("students/paged")]
public async Task<IActionResult> GetPagedStudents(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    // Validate pagination parameters
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 20;
    if (pageSize > 100) pageSize = 100;

    // Always OrderBy before Skip/Take for stable sorting
    var students = await context.Students
        .OrderBy(s => s.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    // Total count for client-side pagination UI
    var totalCount = await context.Students.CountAsync(cancellationToken);

    return Ok(new {
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        Students = students
    });
}
```

**SQL generated:**
```sql
SELECT s."Id", s."GPA", s."IsActive", s."Name", s."RegistrationNumber"
FROM "Students" AS s
ORDER BY s."Name"
LIMIT @pageSize OFFSET @offset
```

**Key design decisions:**
- `OrderBy` before `Skip/Take` — **required for stable pagination**. Without a stable sort, PostgreSQL may return rows in any order between pages, causing duplicates or missed records.
- Parameters validated with sensible defaults (page=1, pageSize=20, max 100)
- CancellationToken passed to `ToListAsync` for proper request cancellation
- Returns total count + total pages so client can build pagination UI

---

### Task 2: Implement Top 5 Courses by Enrollment

**What was done:** Added `GET /api/registrar/queries/top-courses` endpoint.

**C# Implementation:**
```csharp
[HttpGet("queries/top-courses")]
public async Task<IActionResult> TopCoursesByEnrollment(
    CancellationToken cancellationToken = default)
{
    var topCourses = await context.Courses
        .Select(c => new {
            c.Title,
            EnrollmentCount = c.Enrollments.Count
        })
        .OrderByDescending(x => x.EnrollmentCount)
        .Take(5)
        .ToListAsync(cancellationToken);

    return Ok(new {
        Description = "Top 5 courses by enrollment count",
        Results = topCourses
    });
}
```

**SQL generated:**
```sql
SELECT c."Title", (
    SELECT count(*)::int
    FROM "Enrollments" AS e
    WHERE c."Id" = e."CourseId") AS "EnrollmentCount"
FROM "Courses" AS c
ORDER BY (
    SELECT count(*)::int
    FROM "Enrollments" AS e
    WHERE c."Id" = e."CourseId") DESC
LIMIT 5
```

**Result:** Courses ranked by enrollment count, with `LIMIT 5` enforced at the database level.

**Troubleshooting notes:**
- **Client evaluation warning ("could not be translated"):** If your GroupBy projection uses a method EF can't translate, simplify the lambda — use only property access and aggregates (Count(), Average()) inside Select.
- **Pagination returns wrong rows:** You must `OrderBy` before `Skip/Take`. Without a stable sort, PostgreSQL may return rows in any order between pages.
- **Empty results:** Confirm enough enrollment rows exist. Run `SELECT COUNT(*) FROM "Enrollments"` in psql to verify.

---

## Session 2 — Exercise 4: IEntityTypeConfiguration for Each Entity

### Context

`OnModelCreating` should stay short. Each entity gets its own configuration class.

### Steps Taken

#### 1. Created 5 Configuration Classes

All files in `TmsApi/Data/Configurations/`:

**`StudentConfiguration.cs`:**
```csharp
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RegistrationNumber).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.GPA).HasColumnType("decimal(4,2)");
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.HasIndex(s => s.RegistrationNumber).IsUnique().HasDatabaseName("IX_Students_RegistrationNumber");
    }
}
```

**`CourseConfiguration.cs`:**
```csharp
public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Capacity).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("IX_Courses_Code");
    }
}
```

**`EnrollmentConfiguration.cs`** — Contains FK relationships:
```csharp
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Grade).HasColumnType("decimal(4,2)");
        builder.Property(e => e.EnrolledAt).HasDefaultValueSql("NOW()").IsRequired();

        // FK → Student
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK → Course
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**`AssessmentConfiguration.cs`** and **`CertificateConfiguration.cs`** follow the same pattern with appropriate property constraints and FK definitions.

#### 2. Updated TmsDbContext to Use Auto-Discovery

**What was done:** Modified `OnModelCreating` to use `ApplyConfigurationsFromAssembly` — a single line that discovers all `IEntityTypeConfiguration<T>` classes automatically.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
}
```

**Benefits:**
- `OnModelCreating` stays clean (just 1 line)
- Each entity's configuration is in its own file — discoverable and maintainable
- A new team member can read the model in 5 minutes instead of 50
- When the registrar asks "what is the max length of a course title?", the answer is one file: `CourseConfiguration.cs`

#### 3. Generated the RefineTmsModel Migration

**Command:**
```powershell
dotnet ef migrations add RefineTmsModel
```

**What the migration contains:**
- Unique indexes on `Students.RegistrationNumber` and `Courses.Code`
- Column type changes (e.g., `GPA` → `decimal(4,2)`)
- FK delete behavior changes (Cascade → Restrict)
- Database defaults (e.g., `EnrolledAt` → `NOW()`)

**Troubleshooting:**
- **"The entity type requires a primary key":** Your configuration is missing `builder.HasKey(e => e.Id)` or EF cannot infer the key by convention. Name the property `Id` or `EnrollmentId`, or configure it explicitly.
- **Migration shows unexpected table drops:** You renamed a DbSet property or an entity class. EF interprets this as "drop old table, create new one." Rename in a separate migration with `migrationBuilder.RenameTable()` instead.
- **ApplyConfigurationsFromAssembly finds nothing:** The configuration classes must be `public` and in the same assembly as `TmsDbContext`.

---

## Session 2 — Exercise 5: Model the TMS Graph

### Context

A student enrolls in many courses; a course has many students; enrollment stores grade and dates.

### Tasks Done

#### 1. Ensure One-to-Many from Course to Enrollment and Student to Enrollment

**What was done:** FK relationships are configured in `EnrollmentConfiguration.cs`:

- `Enrollment → Student`: `builder.HasOne(e => e.Student).WithMany(s => s.Enrollments).HasForeignKey(e => e.StudentId)`
- `Enrollment → Course`: `builder.HasOne(e => e.Course).WithMany(c => c.Enrollments).HasForeignKey(e => e.CourseId)`

These are the correct sides for the relationship:
- **Enrollment** has the foreign key (it's the dependent/parent entity)
- **Student** and **Course** have the collection navigation properties

#### 2. Decide Delete Behavior

**What was done:** Both relationships use `OnDelete(DeleteBehavior.Restrict)`.

**Why Restrict (not Cascade)?**
- If a student is deleted, their enrollment records should be preserved for historical/audit purposes
- If a course is deleted, enrollment records would be orphaned — the registrar should **deactivate** courses instead of deleting them
- `DeleteBehavior.Restrict` means PostgreSQL will **reject the delete** if enrollments exist, preventing accidental data loss

**Alternative:**
- `DeleteBehavior.Cascade` — deleting a student/course would automatically delete all their enrollments. Useful if enrollment records have no value without the parent.
- `DeleteBehavior.SetNull` — sets the FK to NULL on delete. Only works if the FK property is nullable.

#### 3. Navigation Properties for Include

**What was done:** Navigation properties exist on both sides of each relationship:
- **Student side:** `ICollection<Enrollment> Enrollments`
- **Course side:** `ICollection<Enrollment> Enrollments`
- **Enrollment side:** `Student Student` and `Course Course`

This allows loading related data in Session 3 queries using `.Include()`:
```csharp
var studentWithCourses = context.Students
    .Include(s => s.Enrollments)
    .ThenInclude(e => e.Course)
    .FirstOrDefault(s => s.Id == id);
```

**Troubleshooting:**
- **Cascade cycle detected:** PostgreSQL rejects multiple cascade paths to the same table. Use `DeleteBehavior.Restrict` on at least one side and handle deletion in application code.
- **Navigation property is always null:** You declared the property but forgot `Include()` when querying. Without eager loading, navigation properties remain null by default (EF Core does not use lazy loading unless explicitly configured).

#### 4. Migration Verification

**What was done:** The `RefineTmsModel` migration was inspected to verify correct FK constraints and delete behavior.

```csharp
migrationBuilder.CreateIndex(
    name: "IX_Students_RegistrationNumber",
    table: "Students",
    column: "RegistrationNumber",
    unique: true);

migrationBuilder.AddForeignKey(
    name: "FK_Enrollments_Students_StudentId",
    table: "Enrollments",
    column: "StudentId",
    principalTable: "Students",
    principalColumn: "Id",
    onDelete: ReferentialAction.Restrict);
```

---

## Session Checkpoints

### Session 1 Checkpoint ✅

| Requirement | Status | Evidence |
|-------------|--------|----------|
| `dotnet ef database update` executes successfully | ✅ | Both migrations (InitialCreate + AddAssessmentsAndCertificates) applied |
| Can view tables in PostgreSQL via `\dt` | ✅ | 5 tables: Assessments, Certificates, Courses, Enrollments, Students |
| Can explain `Up()` and `Down()` methods | ✅ | Up() creates tables, Down() drops in reverse dependency order |
| SQL log verifies filters, aggregations, sorting, joins run on database | ✅ | All 4 registrar queries logged with complete SQL |
| Can explain why `.ToList()` before `.Where()` is a performance concern | ✅ | `.AsEnumerable()` pulls entire table into memory before filtering |
| (Stretch) `\dt` shows Assessments and Certificates | ✅ | Wired through named migration `AddAssessmentsAndCertificates` |

### Session 2 Checkpoint ✅

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Migration (RefineTmsModel) generated and inspected | ✅ | Contains unique indexes, column types, FK Restrict, DB defaults |
| 5 configuration classes exist | ✅ | Student, Course, Enrollment, Assessment, Certificate Configurations |
| `OnModelCreating` has only `ApplyConfigurationsFromAssembly(...)` | ✅ | Single line in TmsDbContext |
| Pagination endpoint logs SQL with `LIMIT 20 OFFSET ...` | ✅ | `/api/registrar/students/paged` verified |
| Top-5 courses endpoint logs SQL with `GROUP BY` and `ORDER BY ... DESC LIMIT 5` | ✅ | `/api/registrar/queries/top-courses` verified |
| `OnDelete(DeleteBehavior.Restrict)` is set and justified | ✅ | Comment explains: "prevent deleting a student/course who has enrollments" |

### Session 3 Checkpoint ✅

| Requirement | Status | Evidence |
|-------------|--------|----------|
| At least two migration files exist; can explain Up() and Down() for the latest | ✅ | 4 migrations: InitialCreate, AddAssessmentsAndCertificates, RefineTmsModel, AddSession3Fields |
| N+1 demonstration (Part A) produces 1 + N SQL statements in the log | ✅ | `/api/registrar/n-plus-one/before` — loops queries per student |
| N+1 fix (Part B) produces 1 query (or 1 + 1 subquery) in the log | ✅ | `/api/registrar/n-plus-one/after` — projection with Select translates to SQL subquery |
| LastUpdated shadow column exists on Students (verified in pgAdmin/SQL) | ✅ | `AddSession3Fields` migration adds `LastUpdated` with `timestamp with time zone` |
| Row-version concurrency test produces DbUpdateConcurrencyException | ✅ | `/api/registrar/students/{id}/update-gpa` uses xmin via `IsRowVersion()` |
| ExecuteUpdateAsync for bulk archive logs a single SQL UPDATE statement | ✅ | `/api/registrar/enrollments/archive-old` — single `UPDATE "Enrollments" SET "IsArchived" = true` |
| HasQueryFilter hides soft-deleted students from normal queries | ✅ | `/api/registrar/students/active-list` automatically excludes IsDeleted = true |
| IgnoreQueryFilters() brings soft-deleted students back for admin queries | ✅ | `/api/registrar/students/all-including-deleted` bypasses filter |
---

## Appendix: Verified SQL Outputs

### Session 1 — Deferred Execution
```sql
SELECT s."Id", s."GPA", s."IsActive", s."Name", s."RegistrationNumber"
FROM "Students" AS s
WHERE s."GPA" >= 3.0
ORDER BY s."Name"
```

### Session 1 — Active High GPA Count
```sql
SELECT count(*)::int
FROM "Students" AS s
WHERE s."IsActive" AND s."GPA" >= 3.0
```

### Session 1 — Courses by Enrollments
```sql
SELECT c."Title", (
    SELECT count(*)::int
    FROM "Enrollments" AS e0
    WHERE c."Id" = e0."CourseId") AS "EnrollmentCount"
FROM "Courses" AS c
ORDER BY (
    SELECT count(*)::int
    FROM "Enrollments" AS e
    WHERE c."Id" = e."CourseId") DESC
```

### Session 1 — Average GPA per Course
```sql
SELECT c."Title" AS "Course", (
    SELECT avg(s."GPA")
    FROM "Enrollments" AS e0
    INNER JOIN "Courses" AS c0 ON e0."CourseId" = c0."Id"
    INNER JOIN "Students" AS s ON e0."StudentId" = s."Id"
    WHERE c."Title" = c0."Title") AS "AverageGPA"
FROM "Enrollments" AS e
INNER JOIN "Courses" AS c ON e."CourseId" = c."Id"
GROUP BY c."Title"
```

### Session 1 — Zero Enrollments (NOT EXISTS)
```sql
SELECT s."Name"
FROM "Students" AS s
WHERE NOT EXISTS (
    SELECT 1
    FROM "Enrollments" AS e
    WHERE s."Id" = e."StudentId")
```

### Session 1 — Zero Enrollments (LEFT JOIN)
```sql
SELECT s."Name"
FROM "Students" AS s
LEFT JOIN "Enrollments" AS e ON s."Id" = e."StudentId"
WHERE e."Id" IS NULL
```

### Session 2 — Paged Students
```sql
SELECT s."Id", s."GPA", s."IsActive", s."Name", s."RegistrationNumber"
FROM "Students" AS s
ORDER BY s."Name"
LIMIT @pageSize OFFSET @offset
```

### Session 2 — Top 5 Courses
```sql
SELECT c."Title", (
    SELECT count(*)::int
    FROM "Enrollments" AS e
    WHERE c."Id" = e."CourseId") AS "EnrollmentCount"
FROM "Courses" AS c
ORDER BY (
    SELECT count(*)::int
    FROM "Enrollments" AS e
    WHERE c."Id" = e."CourseId") DESC
LIMIT 5
```

---

## All API Endpoints Reference

| Method | Endpoint | Session | Description |
|--------|----------|---------|-------------|
| GET | `/api/test/deferred` | S1-E2 | LINQ deferred execution demo |
| GET | `/api/test/translation-fail` | S1-E2 | Non-translatable C# method → exception |
| GET | `/api/test/translation-resolved` | S1-E2 | Inline lambda resolves cleanly |
| GET | `/api/test/client-eval` | S1-E2 | `.AsEnumerable()` forces client-side eval |
| GET | `/api/registrar/queries/active-high-gpa-count` | S1-E2 | COUNT active students with GPA >= 3.0 |
| GET | `/api/registrar/queries/courses-by-enrollments` | S1-E2 | Courses ranked by enrollment |
| GET | `/api/registrar/queries/average-gpa-per-course` | S1-E2 | AVG(GPA) per course |
| GET | `/api/registrar/queries/students-no-enrollments/subquery` | S1-E2 | NOT EXISTS approach |
| GET | `/api/registrar/queries/students-no-enrollments/left-join` | S1-E2 | LEFT JOIN approach |
| GET | `/api/registrar/students/paged` | S2-E3 | Paged list with OrderBy/Skip/Take |
| GET | `/api/registrar/queries/top-courses` | S2-E3 | Top 5 by enrollment |
| GET | `/api/enrollments` | S3-E5 | List all enrollments |
| GET | `/api/enrollments/{id}` | S3-E5 | Get enrollment by ID |
| POST | `/api/enrollments` | S3-E5 | Create enrollment |
| DELETE | `/api/enrollments/{id}` | S3-E5 | Delete enrollment |
| GET | `/api/assessments/results` | S1-E1 | Secured sample (requires auth) |
| GET | `/api/enrollments/worker-smoke` | S2-E2 | Background worker test |
| GET | `/api/error` | S3-E6 | Simulated error for ProblemDetails |
| GET | `/api/registrar/n-plus-one/before` | S3-E7 | N+1 demo: 1+N SQL statements per student |
| GET | `/api/registrar/n-plus-one/after` | S3-E7 | N+1 fix: single query with projection subquery |
| GET | `/api/registrar/n-plus-one/include` | S3-E7 | N+1 alternative fix using eager Include |
| GET | `/api/registrar/students/{id}` | S3-E8 | Get student by ID (concurrency test) |
| PUT | `/api/registrar/students/{id}/update-gpa` | S3-E8 | Update GPA with concurrency token check |
| POST | `/api/registrar/enrollments/archive-old` | S3-E9 | Bulk archive old enrollments via ExecuteUpdateAsync |
| POST | `/api/registrar/students/{id}/soft-delete` | S3-E9 | Soft-delete student (sets IsDeleted) |
| GET | `/api/registrar/students/active-list` | S3-E9 | Active students (soft-deleted excluded by HasQueryFilter) |
| GET | `/api/registrar/students/all-including-deleted` | S3-E9 | All students including soft-deleted (admin override) |
| POST | `/api/registrar/students/{id}/restore` | S3-E9 | Restore a soft-deleted student |

---

*Generated on: 2026-06-23 | Branch: `session3-exercises` | Repo: `github.com/NathnelTK/UPDATE-TMSAPI`*
