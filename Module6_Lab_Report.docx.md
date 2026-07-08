# Module 6 Lab Report

## 1. Introduction
This report documents the comparison between the Module 6 Session 1 and Session 2 lab PDFs and the implementation that exists in the current repository and related pull requests.

## 2. Objective
The objective is to explain what the lab exercises required, what implementation exists in the repository, and what runtime outputs were observed while exercising the API.

## 3. Source Materials Reviewed
- Module 6 Lab Session 1 PDF
- Module 6 Lab Session 2 PDF
- GitHub PR 1: Session 1 Request Flow & Visibility
- GitHub PR 2: Session 2 Services Done Right
- Local ASP.NET Core project files

## 4. Summary of the Lab Requirements
### Session 1
- Build a first REST contract for a Course resource.
- Expose resource-based routes.
- Use DTOs instead of EF entities.
- Return appropriate validation and conflict responses.

### Session 2
- Add pagination and filtering.
- Return metadata such as totalCount and totalPages.
- Add a cross-cutting audit filter.
- Seed deterministic course data.

## 5. What Was Implemented
### Implemented in the repository
- Custom authentication with a training header.
- Request logging middleware with correlation IDs.
- A secured assessments endpoint.
- An enrollment service with structured logging.
- An enrollment worker that resolves a scoped service safely.
- Payment options with validation annotations.

### Not implemented in the current repository
- Full Course and Enrollment controllers.
- DTO-based course contracts.
- Validation-driven 400 responses.
- 409 conflict handling for business rules.
- Pagination and metadata for courses.
- Global audit filter.

## 6. Verified Run Output
### Build
```text
dotnet build
Build succeeded in 1.3s
```

### Unauthorized access
```text
curl -i http://127.0.0.1:5077/api/assessments/results
HTTP/1.1 401 Unauthorized
```

### Authorized access
```text
curl -i -H "X-Training-User: demo" http://127.0.0.1:5077/api/assessments/results
HTTP/1.1 200 OK
```

### Worker endpoint
```text
curl -i http://127.0.0.1:5077/api/enrollments/worker-smoke
HTTP/1.1 200 OK
```

## 7. Conclusion
The current repository implements related API patterns and training exercises, but it does not yet fully match the detailed Course/Enrollment REST contract described in the Module 6 PDFs.
