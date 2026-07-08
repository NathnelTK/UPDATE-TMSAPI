# Module 6 Lab Session 1 and Session 2 Documentation

## Overview

This document compares the requested Module 6 lab sessions with the implementation available in the repository and the public pull requests for the corresponding work. The repository currently contains a training-style ASP.NET Core API project rather than the full EF Core + PostgreSQL course contract described in the PDF, so the documentation below explains both:

- what the lab PDFs require, and
- what was actually implemented in the available codebase and PRs.

The goal is to provide a practical report that explains the work done, the current state, and the observable outputs from running the application.

---

## Source Materials Reviewed

The following materials were used as the basis for this report:

- Module 6 Lab Session 1 PDF: M6-Lab-Session-1.pdf
- Module 6 Lab Session 2 PDF: M6-Lab-Session-2.pdf
- GitHub PR 1: Session 1 Request Flow & Visibility
- GitHub PR 2: Session 2 Services Done Right
- Local project files in the workspace and the nested TMSApi project

---

## High-Level Comparison

### What the PDFs describe

The PDFs describe a Module 6 REST API lab focused on:

- a Course resource contract,
- DTO-based API responses,
- validation and status codes,
- services for persistence logic,
- a paged course listing,
- filtering and sorting,
- and a cross-cutting audit filter.

### What the repository currently contains

The available implementation is a smaller training API that focuses on:

- authentication with a custom training header,
- request logging middleware,
- a minimal assessments endpoint,
- an enrollment service with structured logging,
- an enrollment worker that resolves a scoped service from a scope,
- and payment configuration options with validation.

This means the repository does not yet implement the full Course/Enrollment REST contract from the PDF, but it does contain several of the same architectural habits and patterns that the labs are trying to teach.

---

## Session 1 Summary

### Lab intent from the PDF

Session 1 aims to build a first workable REST contract for a Course resource. The key goals are:

- expose resource-based routes such as /api/courses and /api/courses/{id},
- use DTOs instead of returning EF entities directly,
- validate request data,
- return 400 Bad Request for validation failures,
- return 409 Conflict for business-rule failures such as duplicate course codes or full courses.

### What is implemented in the available PR and project

The available implementation is not the full course contract, but it does implement the underlying request pipeline and service-layer patterns that are central to those labs:

1. Authentication flow
   - A custom authentication handler was added.
   - The handler accepts a custom X-Training-User header and authenticates the request when it is present.

2. Request logging middleware
   - A middleware component logs request start and completion events.
   - It attaches a correlation ID header to the response.

3. Secured endpoint
   - A minimal endpoint under /api/assessments/results was added.
   - The endpoint is protected and requires authentication.

### Mapping to the Session 1 lab ideas

| PDF lab concept | Current implementation status |
| --- | --- |
| REST controller for resource actions | Partially represented through minimal endpoint setup, but not a full CoursesController |
| DTOs and validation | Not implemented in the current course-contract form |
| 400/409 error handling | Not implemented as the PDF describes |
| Service abstraction | Not implemented for Course persistence, but the repository shows service-oriented patterns in the enrollment service |

### Verified runtime behavior

The application was built and run locally. The following outputs were observed:

#### Build

```text
$ dotnet build
Restore complete (0.3s)
  TMSApi net10.0 succeeded (0.6s) → bin\Debug\net10.0\TMSApi.dll

Build succeeded in 1.3s
```

#### Request without header

```text
$ curl -i http://127.0.0.1:5077/api/assessments/results
HTTP/1.1 401 Unauthorized
Content-Length: 0
Date: Wed, 08 Jul 2026 07:28:09 GMT
Server: Kestrel
```

#### Request with header

```text
$ curl -i -H "X-Training-User: demo" http://127.0.0.1:5077/api/assessments/results
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Wed, 08 Jul 2026 07:28:21 GMT
Server: Kestrel
Transfer-Encoding: chunked
```

---

## Session 2 Summary

### Lab intent from the PDF

Session 2 extends the REST contract so it can scale and support cross-cutting concerns. The main goals are:

- add pagination for list endpoints,
- support filtering and sorting,
- enforce a hard page-size limit,
- return paged metadata such as totalCount and totalPages,
- add a global audit filter,
- seed the database deterministically for reproducible testing.

### What is implemented in the available PR and project

The available implementation does not implement pagination or a full course list endpoint, but it does include several related patterns from the same training module:

1. Service abstraction for enrollments
   - An enrollment service was introduced.
   - It handles duplicate-detection logic and structured logging.

2. Dependency lifetime awareness
   - An EnrollmentWorker demonstrates how to resolve a scoped service from a singleton using IServiceScopeFactory.

3. Options validation pattern
   - PaymentOptions uses data annotations to define validation rules for payment configuration.

### Mapping to the Session 2 lab ideas

| PDF lab concept | Current implementation status |
| --- | --- |
| Pagination contract | Not implemented |
| Filtering and sorting | Not implemented |
| Audit filter | Not implemented as a global action filter |
| Deterministic data seeding | Not implemented in the course-contract form |
| Service-layer paging and projection | Not implemented |

### Verified runtime behavior

The worker endpoint was exercised successfully. The observed output was:

```text
$ curl -i http://127.0.0.1:5077/api/enrollments/worker-smoke
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Wed, 08 Jul 2026 07:28:09 GMT
Server: Kestrel
Transfer-Encoding: chunked
```

The server logs also showed:

```text
Worker processed 0 enrollments.
```

---

## What Was Done in the Repository

### 1. Custom authentication

A training authenticator was introduced to represent a simple, header-based authentication mechanism.

Key file:
- TMSApi/traningauthhandler.cs

Behavior:
- If X-Training-User is absent, the request is unauthenticated.
- If it is present, the request is authenticated.

### 2. Request logging and correlation IDs

A custom middleware component was added to log entry and exit information for each request.

Key file:
- TMSApi/RequestLoggingMiddleware.cs

Behavior:
- Generates a correlation ID.
- Adds it to the response header.
- Logs start and completion events for observability.

### 3. Enrollment service and logging

The service encapsulates enrollment operations and logs meaningful events.

Key file:
- TMSApi/EnrollmentService.cs

Capabilities:
- Create enrollment records.
- Retrieve a single record or all records.
- Delete a record.
- Detect duplicate enrollment attempts.

### 4. Scoped service resolution in a singleton worker

The worker demonstrates a common DI lifetime pitfall and how to avoid it.

Key file:
- TMSApi/EnrollmentWorker.cs

### 5. Options validation

Payment options were introduced with validation attributes.

Key file:
- TMSApi/PaymentOptions.cs

---

## Important Note About the Module 6 PDF Requirements

The PDFs describe a more advanced REST API contract than the code currently in the repository. In particular, the full implementation expected by the PDFs would include:

- a CoursesController,
- an EnrollmentsController,
- EF Core entities and migrations,
- DTOs and validation,
- conflict responses for business-rule violations,
- paged list endpoints with metadata,
- and a global audit filter.

Those pieces are not present in the current workspace implementation. The available code is best understood as a training scaffold that demonstrates related API concepts rather than the exact full exercise solution from the PDF.

---

## Recommended Next Steps

If the goal is to align the repository with the PDF lab exercises more closely, the next steps would be:

1. Create the Course entity and EF Core configuration.
2. Add a CoursesController with GET/POST endpoints.
3. Introduce DTOs and validation attributes.
4. Implement conflict handling for duplicate course codes and full-course enrollment.
5. Build a paged course listing endpoint with filtering, sorting, and metadata.
6. Add a global audit filter for request logging.
7. Add a deterministic seeder for course data.

---

## Conclusion

The Module 6 lab PDFs describe a richer and more production-oriented API implementation than the current repository contains. However, the available PRs and source files do show several of the architectural concepts that the labs are teaching: authentication, middleware, service abstractions, dependency lifetimes, structured logging, and options validation.

The implementation was verified locally by building the project and exercising the API. The observed outputs are included above and can be copied into a final report or presentation.
