# Architecture Description

## Summary

The Shared Notes API is an ASP.NET Core 10 REST service separated into controllers, services, repositories, and a unit-of-work boundary.

## Layers

- **Controllers:** Handle HTTP routes, headers, request validation, and status codes.
- **Services:** Enforce team membership, ownership, sharing, update permission, and delete rules.
- **Repositories:** Expose note and team storage operations through interfaces.
- **Unit of Work:** Provides one repository boundary for services and a future database commit abstraction.

## Identity

`X-User-Id` is temporary demonstration identity. A production implementation should derive `UserId` and team authorization from validated JWT or OIDC claims.

## Storage

`ConcurrentDictionary` implementations provide process-local, thread-safe temporary storage. EF Core or another database should replace these repositories when persistence is required.

## Dependency Injection

`Program.cs` registers note, team, unit-of-work, and service implementations. Repositories are singleton implementations for process lifetime; the service and unit-of-work are scoped.

## Permission Model

- Owners can create, read, update, share, and delete.
- Direct shared users can read and may update only with `AllowUpdates=true`.
- Shared teams may read and may update when the owner grants team update permission.
- Shared users and teams cannot delete.

## REST Resources

- `/api/v1/teams`
- `/api/v1/teams/{teamId}/members`
- `/api/v1/teams/{teamId}/notes`
- `/api/v1/teams/{teamId}/notes/{id}`
- `/api/v1/teams/{teamId}/notes/{id}/share`

## Test Boundary

Service tests verify business rules; repository tests verify concurrent storage behavior; `WebApplicationFactory` integration tests verify HTTP routing, DI, headers, payload validation, and status codes.
