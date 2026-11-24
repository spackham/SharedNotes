# Architecture Thoughts 1 — Shared Notes REST Service

## Purpose

This document records the architecture review and limited-time design decisions for the Shared Notes ASP.NET Core evaluation project.

## High-Level Structure

The implementation follows a controller → service → repository structure:

- **Controller:** HTTP concerns, route parameters, headers, request validation, and status-code mapping.
- **Service:** Team membership, ownership, sharing, update, and delete rules.
- **Repository:** In-memory note and team storage behind interfaces.

This separation keeps authorization and business rules out of endpoint methods and makes service logic independently testable.

## Identity and Teams

The temporary system models two identifiers:

- `UserId` identifies the acting user.
- `TeamId` identifies the team workspace that owns or receives access to a note.

Teams have names and member lists. The current `X-User-Id` header is demonstration-only. A production system should derive identity from validated JWT or OIDC claims.

## REST Resource Design

The primary note routes are team-scoped:

- `GET /api/v1/teams/{teamId}/notes`
- `GET /api/v1/teams/{teamId}/notes/{id}`
- `POST /api/v1/teams/{teamId}/notes`
- `PUT /api/v1/teams/{teamId}/notes/{id}`
- `POST /api/v1/teams/{teamId}/notes/{id}/share`
- `DELETE /api/v1/teams/{teamId}/notes/{id}`

Team operations are provided under:

- `GET /api/v1/teams`
- `GET /api/v1/teams/{teamId}`
- `POST /api/v1/teams`
- `POST /api/v1/teams/{teamId}/members`

Plural nouns, standard HTTP verbs, route identifiers, and async controller signatures are used consistently.

## Permission Model

For the time-boxed evaluation scope:

- A note owner can create, read, update, share, and delete notes.
- A direct user may read a shared note and add updates only when the owner permits updating.
- Shared team members receive the same owner-granted update capability.
- Users outside the owner role cannot delete notes.
- Sharing belongs to the owner; a shared user may not extend sharing or change note ownership.

Full role-based permissions and update-history/audit models are intentionally deferred.

## Temporary Data Storage

`ConcurrentDictionary` is used for both notes and teams during this evaluation. It demonstrates thread-safe in-memory state and a clean repository boundary.

The abstraction allows the implementation to be replaced later without changing controller or service contracts. The data is process-local and lost on restart.

## Inversion of Control

ASP.NET Core dependency injection is configured in `Program.cs`:

- `INoteRepository` maps to `ConcurrentDictionaryNoteRepository`.
- `ITeamRepository` maps to `ConcurrentDictionaryTeamRepository`.
- `INoteService` maps to `NoteService`.

Repositories are singletons because their dictionaries must persist across requests. The service is scoped.

## Async and Concurrency

Controller, service, and repository methods use `Task`, `CancellationToken`, and `await`. Repository snapshots and `TryUpdate` operations reduce mutable shared-state problems in the `ConcurrentDictionary` implementation.

Concurrent dictionary operations should still be treated as process-local. A database implementation will require transactions, indexes, optimistic concurrency, and locking strategies.

## Testing Approach

The current test suite covers:

- Team creation and membership.
- Owner, direct-user, and cross-team access.
- Owner-only share and delete behavior.
- Missing notes and missing teams.
- Blank identifiers and duplicate shares.
- Input trimming and cancellation.
- Concurrent create, update, share, and delete operations.
- HTTP create/read and payload-validation flows.

Further work should add forbidden-versus-not-found semantics, test role permissions, add real authorization claims, and run integration tests in a controlled environment.

## Security Review

GitHub CodeQL is configured for C# pushes, pull requests, weekly scans, and manual runs. Dependabot is configured for NuGet and GitHub Actions dependency updates.

Local checks include package vulnerability scans and Release builds. These checks do not replace security review, threat modeling, authentication, authorization testing, or secure secret management.

## Repository and Unit-of-Work Pattern

The repository implementation is formalized behind `INoteRepository`, `ITeamRepository`, and `IUnitOfWork`.

- `INoteRepository` owns note storage operations.
- `ITeamRepository` owns team and membership operations.
- `IUnitOfWork` exposes both repositories and a future commit boundary.
- `ConcurrentDictionaryUnitOfWork` is the temporary implementation and commits without persistence.
- `NoteService` consumes `IUnitOfWork` through dependency injection rather than concrete repositories.

This structure preserves a migration path to EF Core or another database implementation without moving storage details into controllers.

## Owner-Controlled Update Permissions

The `ShareNoteRequest` includes `AllowUpdates` for direct-user and team shares.

- Owner: always read/update/delete and control sharing.
- Direct shared user: read by default; update only when `AllowUpdates=true`.
- Shared team member: read by default; update only when the owner grants team update permission.
- No shared user or team member may delete a note.

The repository stores update authorization lists separately for user shares and team shares, keeping read access and edit access explicit.

## Deferred for Time Constraints

The initial implementation is intentionally limited. With more time, the next priorities are:

1. Replace header identity with JWT/OIDC claims.
2. Replace `ConcurrentDictionary` with EF Core and a relational database.
3. Add migrations, constraints, indexes, transactions, and optimistic concurrency.
4. Add explicit permission models and owner-granted update scopes.
5. Add update history with user and timestamp records.
6. Centralize typed service outcomes and Problem Details responses.
7. Add pagination, filtering, quotas, logging, tracing, health checks, and rate limiting.

## Reviewer Assessment

The project demonstrates clear separation of concerns, explicit temporary state, consistent REST naming, async programming, dependency injection, and testable business logic. The next review should focus on authorization semantics, endpoint contract consistency, typed result handling, and the persistence migration boundary.
