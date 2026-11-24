# Shared Notes REST API

## Bluestaq Review Summary

A. **Design choices**

- C# with ASP.NET Core 10 and Visual Studio Code, with GitHub Free Copilot used as a coding assistant.
- Controller → service → repository → unit-of-work separation with dependency injection.
- Team-scoped note sharing, separate `UserId` and `TeamId` identities, owner-controlled update permissions, and temporary `ConcurrentDictionary` storage.
- A staged code review used `docs/CodeReview/CodeReviewDoc1.md` to identify flaws. Required-fix records separate implemented fixes from moderate and harder production work.

B. **Primary design focus**

Most time was spent on the service boundary and authorization model: defining ownership, team membership, direct sharing, cross-team sharing, read/update permissions, and owner-only deletion before endpoint implementation details.

C. **What I would change with more time**

- Replace demo identity headers with JWT/OIDC authentication.
- Replace `ConcurrentDictionary` with EF Core, migrations, transactions, constraints, and optimistic concurrency.
- Add typed service outcomes, standardized HTTP errors, and stronger 403/404 testing.
- Add explicit roles, audit history, pagination, logging, tracing, health checks, and deployment configuration.

A paid AI coding or review plan could have supported longer autonomous implementation, broader external-library research, deeper test generation, and repeated automated code-review passes. With the remaining time and available tools, this project focused on the highest-value architecture and test boundaries rather than attempting every production enhancement.

## Testing Status

The suite currently covers team membership, ownership, sharing, route scoping, read/update permissions, concurrency, HTTP status behavior, and integration flows.

See `docs/Testing/` for QA, testing, and security information. See `docs/architecture/` for implementation details and `docs/CodeReview/` for review findings and required fixes.
