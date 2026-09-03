using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

/// <summary>
/// Deterministic seeder that populates 25 courses for pagination verification.
/// Idempotent — running it twice does not double-insert.
/// Calls MigrateAsync first so schema and data come up in one call on fresh databases.
/// </summary>
public static class DataSeeder
{
    private static readonly (string Code, string Title, string Category, string Duration, string Description, string Requirements, string Prerequisites, int MaxCapacity)[] Courses =
    [
        ("CSE-101", "Web Development Fundamentals", "Software Development", "8 weeks", "Build accessible websites with HTML, CSS, JavaScript, and practical responsive layouts.", "Basic computer literacy and logical problem solving", "None", 30),
        ("CSE-102", "TypeScript Application Development", "Software Development", "8 weeks", "Develop maintainable browser applications with strict TypeScript, testing, and reusable components.", "Basic JavaScript", "Web Development Fundamentals", 30),
        ("CSE-103", "Git and Collaborative Workflows", "Professional Practice", "4 weeks", "Use Git branching, pull requests, code review, and team delivery practices in a professional workflow.", "Basic command-line usage", "None", 25),
        ("CSE-201", "ASP.NET Core Web Applications", "Software Development", "10 weeks", "Create secure production-oriented web applications and REST APIs with C# and ASP.NET Core.", "Object-oriented programming", "None", 28),
        ("CSE-202", "Entity Framework Core and PostgreSQL", "Data Engineering", "6 weeks", "Model relational data, create migrations, tune queries, and build reliable PostgreSQL-backed services.", "C# and SQL fundamentals", "ASP.NET Core Web Applications", 28),
        ("CSE-203", "Building RESTful Web APIs", "Software Development", "8 weeks", "Design versioned REST APIs with validation, authorization, ProblemDetails, observability, and integration tests.", "C# programming", "ASP.NET Core Web Applications", 28),
        ("CSE-301", "Advanced Web API Architecture", "Software Architecture", "8 weeks", "Apply CQRS, domain rules, resilience, caching, and scalable service boundaries to enterprise APIs.", "Professional API experience", "Building RESTful Web APIs", 24),
        ("CSE-302", "Angular Frontend Engineering", "Software Development", "8 weeks", "Build responsive Angular applications with strict TypeScript, routing, forms, state, and accessible UX.", "HTML, CSS, and JavaScript", "Web Development Fundamentals", 26),
        ("CSE-303", "Advanced Angular Applications", "Software Development", "8 weeks", "Deliver modular Angular systems with signals, lazy loading, performance profiling, and testable components.", "Angular fundamentals", "Angular Frontend Engineering", 24),
        ("CSE-304", "Full-Stack Product Delivery", "Software Development", "10 weeks", "Integrate Angular and ASP.NET Core into a complete product with authentication, workflows, and deployment readiness.", "Frontend and backend fundamentals", "Building RESTful Web APIs", 22),
        ("CSE-305", "Software Testing and Quality Assurance", "Quality Engineering", "6 weeks", "Prove business rules with unit, integration, API, and end-to-end tests while improving delivery confidence.", "Programming fundamentals", "None", 22),
        ("CSE-306", "Application Security and Authentication", "Cybersecurity", "6 weeks", "Implement JWT authentication, authorization policies, password protection, secure configuration, and threat-aware API design.", "Web development experience", "Building RESTful Web APIs", 20),
        ("DAT-101", "Database Design Foundations", "Data Engineering", "6 weeks", "Design normalized relational schemas, constraints, relationships, indexes, and practical reporting queries.", "Basic computer literacy", "None", 30),
        ("DAT-201", "Advanced SQL and Query Performance", "Data Engineering", "6 weeks", "Improve database performance using query plans, indexes, aggregation, transactions, and PostgreSQL tools.", "SQL fundamentals", "Database Design Foundations", 26),
        ("DAT-202", "Data Modelling for Digital Services", "Data Engineering", "6 weeks", "Translate institutional workflows into robust data models that support reporting, audit, and future change.", "SQL and analysis fundamentals", "Database Design Foundations", 26),
        ("ARC-101", "Software Architecture Patterns", "Software Architecture", "6 weeks", "Choose practical architecture patterns for maintainability, modularity, integration, and long-term product evolution.", "Professional programming experience", "None", 22),
        ("ARC-201", "Cloud-Native Application Design", "Cloud and Infrastructure", "8 weeks", "Design observable, resilient services with containers, health checks, configuration, and cloud deployment concepts.", "Backend development experience", "Software Architecture Patterns", 22),
        ("DEV-101", "DevOps Foundations", "Cloud and Infrastructure", "6 weeks", "Learn repeatable build, test, deployment, monitoring, and incident-response practices for software teams.", "Basic Git and Linux concepts", "Git and Collaborative Workflows", 24),
        ("DEV-201", "Continuous Delivery Pipelines", "Cloud and Infrastructure", "6 weeks", "Create reliable CI/CD pipelines with quality gates, artifact management, environment promotion, and rollback strategy.", "DevOps fundamentals", "DevOps Foundations", 22),
        ("MOB-101", "Mobile Application Foundations", "Mobile Development", "8 weeks", "Design and build mobile interfaces with navigation, local state, API integration, and accessibility in mind.", "Programming fundamentals", "None", 24),
        ("MOB-201", "Cross-Platform Mobile Development", "Mobile Development", "8 weeks", "Deliver maintainable cross-platform mobile experiences that consume secure backend services.", "Mobile fundamentals", "Mobile Application Foundations", 22),
        ("AI-101", "Applied Machine Learning", "Data and AI", "10 weeks", "Prepare data and train practical models for classification, regression, evaluation, and responsible deployment.", "Python and basic statistics", "None", 20),
        ("AI-201", "Generative AI for Developers", "Data and AI", "6 weeks", "Build responsible AI-assisted features with prompt design, retrieval concepts, evaluation, and security controls.", "Python or JavaScript fundamentals", "Applied Machine Learning", 18),
        ("UX-101", "UX Research and Service Design", "Design and Product", "6 weeks", "Research learner needs, map service journeys, prototype solutions, and validate designs with real users.", "Basic digital literacy", "None", 24),
        ("UX-201", "Design Systems and Accessible Interfaces", "Design and Product", "6 weeks", "Create consistent, accessible interface systems with reusable tokens, components, content, and responsive behavior.", "UX fundamentals", "UX Research and Service Design", 22),
    ];

    /// <summary>
    /// Seed the database with the deterministic catalog.
    /// Idempotent: existing course codes are preserved and only missing rows are added.
    /// </summary>
    public static async Task SeedAsync(TmsDbContext context, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        var existingCodes = await context.Courses
            .AsNoTracking()
            .Select(course => course.Code)
            .ToHashSetAsync(ct);

        foreach (var (code, title, category, duration, description, requirements, prerequisites, maxCapacity) in Courses)
        {
            var existing = await context.Courses.FirstOrDefaultAsync(course => course.Code == code, ct);
            if (existing is not null)
            {
                existing.Title = title;
                existing.Category = category;
                existing.Duration = duration;
                existing.Description = description;
                existing.MinimumRequirements = requirements;
                existing.Prerequisites = prerequisites;
                existing.MaxCapacity = maxCapacity;
                existing.Status = "Published";
                continue;
            }

            context.Courses.Add(new Course
            {
                Code = code,
                Title = title,
                MaxCapacity = maxCapacity,
                Category = category,
                Duration = duration,
                Description = description,
                MinimumRequirements = requirements,
                Prerequisites = prerequisites,
                Status = "Published"
            });
        }

        await context.SaveChangesAsync(ct);
    }
}
