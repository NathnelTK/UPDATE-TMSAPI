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
# Deferred execution experiment — watch the console logs!
curl http://localhost:5003/api/test/deferred

# Translation failure experiment — shows EF can't translate C# methods to SQL
curl http://localhost:5003/api/test/translation-fail

# Resolved translation — inline lambda works fine
curl http://localhost:5003/api/test/translation-resolved

# Client-side evaluation — pulls all rows into memory first
curl http://localhost:5003/api/test/client-eval

# --- Registrar Business Queries ---

# 1. Active students with GPA >= 3.0
curl http://localhost:5003/api/registrar/queries/active-high-gpa-count

# 2. Courses sorted by enrollment count
curl http://localhost:5003/api/registrar/queries/courses-by-enrollments

# 3. Average GPA per course
curl http://localhost:5003/api/registrar/queries/average-gpa-per-course

# 4a. Students with zero enrollments (NOT EXISTS approach)
curl http://localhost:5003/api/registrar/queries/students-no-enrollments/subquery

# 4b. Students with zero enrollments (LEFT JOIN approach)
curl http://localhost:5003/api/registrar/queries/students-no-enrollments/left-join
```

---

## 🧪 Endpoint Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/test/deferred` | LINQ deferred execution demo — builds query in steps, materializes at `.ToList()` |
| GET | `/api/test/translation-fail` | Throws on non-translatable C# method |
| GET | `/api/test/translation-resolved` | Inline lambda resolves cleanly |
| GET | `/api/test/client-eval` | `.AsEnumerable()` forces client-side evaluation |
| GET | `/api/registrar/queries/active-high-gpa-count` | `SELECT COUNT(*) ... WHERE IsActive AND GPA >= 3.0` |
| GET | `/api/registrar/queries/courses-by-enrollments` | Courses ranked by enrollment count (desc) |
| GET | `/api/registrar/queries/average-gpa-per-course` | `GROUP BY ... AVG(GPA)` |
| GET | `/api/registrar/queries/students-no-enrollments/subquery` | `NOT EXISTS` approach |
| GET | `/api/registrar/queries/students-no-enrollments/left-join` | `LEFT JOIN ... IS NULL` approach |
| GET | `/api/enrollments` | List all enrollments |
| GET | `/api/enrollments/{id}` | Get enrollment by ID |
| POST | `/api/enrollments` | Create enrollment |
| DELETE | `/api/enrollments/{id}` | Delete enrollment |
| GET | `/api/assessments/results` | Secured sample endpoint (requires auth header) |
| GET | `/api/enrollments/worker-smoke` | Background worker smoke test |

---

## 🏗️ Project Structure

```
TmsApi/
├── Controllers/
│   ├── EnrollmentsController.cs   # CRUD for enrollments
│   ├── TestController.cs          # LINQ experiments (deferred, translation)
│   ├── RegistrarController.cs     # Business queries (Module 5)
│   └── WeatherForecastController.cs
├── Data/
│   └── TmsDbContext.cs             # EF Core database context
├── Entities/
│   ├── Student.cs                  # Student entity
│   ├── Course.cs                   # Course entity
│   ├── Enrollment.cs               # Enrollment entity (many-to-many)
│   ├── Assessment.cs               # Assessment entity (belongs to Course)
│   └── Certificate.cs              # Certificate entity (belongs to Student + Course)
├── Migrations/
│   ├── 20260618..._InitialCreate.cs                     # Creates 3 tables
│   └── 20260618..._AddAssessmentsAndCertificates.cs      # Creates 2 tables
├── Properties/
│   └── launchSettings.json         # Launch configuration (gitignored)
├── Program.cs                      # App entry point, DI, middleware, seeder
├── EnrollmentService.cs            # Business logic (in-memory → DB)
├── EnrollmentWorker.cs             # Background worker
├── TrainingAuthHandler.cs          # Custom authentication
├── RequestLoggingMiddleware.cs     # Request logging
├── TmsDatabaseException.cs         # Custom exception
├── PaymentOptions.cs               # Options model
├── appsettings.json                # Production settings (gitignored)
├── appsettings.Development.json    # Dev settings with connection string (gitignored)
└── TmsApi.csproj                   # Project file
```

---

## 🧠 Architecture Notes

### Entity Design
- **Surrogate keys** (`int Id`) — stable, auto-incremented primary keys used by foreign keys
- **Natural keys** (`RegistrationNumber`, `Code`, `SerialNumber`) — human-readable identifiers
- **Navigation properties** — enable EF Core to translate LINQ joins to SQL

### LINQ Execution Model
- **IQueryable**: LINQ queries compose an expression tree — NO database contact
- **Materialization**: `.ToList()`, `.CountAsync()`, `.FirstAsync()` trigger SQL execution
- **Translation**: EF Core translates expression trees to SQL — custom C# methods **cannot** be translated

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

---

## 📚 Key Technologies

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Application framework |
| ASP.NET Core | 10.0 | Web API & routing |
| Entity Framework Core | 10.0 | Object-relational mapping |
| Npgsql | 10.0 | PostgreSQL database provider |
| PostgreSQL | 17 | Relational database |

---

## 🎯 Lab Checkpoint

- [x] `dotnet ef database update` executes successfully
- [x] All 5 tables visible in PostgreSQL (`\dt`)
- [x] SQL logs show filters, aggregates, sorting, and joins running on the database server
- [x] Calling `.ToList()` before `.Where()` causes performance concern (client-side eval)
- [x] Assessments and Certificates wired through a named migration (not `EnsureCreated()`)