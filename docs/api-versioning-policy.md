# TMS API Versioning Policy

## 1. Versioning Strategy
The TMS API uses **URL-segment versioning** (`/api/v1/courses`, `/api/v2/courses`). The version is part of the URL path, making it immediately visible in logs, metrics, and during incident response.

- **Default version**: 1.0 (clients that don't specify a version get V1)
- **Current versions**: V1 (stable, deprecated), V2 (active)
- **Header-based opt-in**: Partners may use `X-Api-Version: 2.0` header as an alternative to URL-segment versioning (opt-in only, not the default)

## 2. Breaking Changes (require a new version)
The following changes are **always** breaking and require a new API version:

- Removing a field from a response
- Renaming a field in a request or response
- Changing the type of a field (e.g., `int` GÂ∆ `string`)
- Adding a new required field to a request body
- Changing a response status code (e.g., 200 GÂ∆ 201)
- Tightening validation rules (e.g., making an optional field required)
- Changing the default sort order of a list endpoint
- Removing or renaming an endpoint

## 3. Additive (Non-Breaking) Changes
The following changes are **safe** and do not require a new version:

- Adding a new optional field to a response
- Adding a new endpoint
- Adding a new optional query parameter
- Adding a new HTTP method to an existing resource
- Relaxing validation rules (e.g., increasing a max length)
- Adding HATEOAS links to a response

## 4. Sunset Window
When a new API version ships, the previous version enters a **6-month minimum sunset window**:

- **Day 0**: V2 ships. V1 gets `Deprecation: true`, `Sunset: <date>`, and `Link: <V2>; rel="successor-version"` headers on every response.
- **Month 3**: Email sent to all API key holders with migration guide and V1 shutdown date.
- **Month 5**: V1 starts returning `Warning: 299 - "V1 will be shut down on <date>"` header.
- **Month 6**: V1 is decommissioned. Requests return 410 Gone.

This window ensures rural training centres on quarterly maintenance schedules can migrate without disruption.

## 5. Communication
When a new version is released, the following communication channels are activated:

1. **HTTP Headers**: `Deprecation`, `Sunset`, and `Link` headers from day one of the new version
2. **CHANGELOG**: Entry in the project CHANGELOG describing what changed and why
3. **Email**: Notification to every team that holds an API key
4. **Calendar Invite**: A calendar event for the V1 shutdown date, sent 3 months in advance

## 6. Skipping Versions
Clients may skip intermediate versions. V1 GÂ∆ V3 is allowed. Clients are not forced to migrate through every intermediate version. However, each version has its own sunset window, so skipping versions may result in a shorter effective migration period.

## 7. Policy Review
This policy is reviewed annually. Exceptions require sign-off from the architecture team and must be documented in the CHANGELOG with a rationale.
