# Shared Notes — Implementation and Architecture Details

## Overview

This project is a C# ASP.NET Core 10 REST service demonstration for sharing notes across users and teams. It uses a controller → service → repository → unit-of-work structure.

## API Resources

Primary note routes:

- `GET /api/v1/teams/{teamId}/notes` — list accessible notes for a team member.
- `GET /api/v1/teams/{teamId}/notes/{id}` — retrieve an accessible note.
- `POST /api/v1/teams/{teamId}/notes` — create a note as a team member.
- `PUT /api/v1/teams/{teamId}/notes/{id}` — update a note when the owner or granted permission allows it.
- `POST /api/v1/teams/{teamId}/notes/{id}/share` — owner grants direct-user or team access.
- `DELETE /api/v1/teams/{teamId}/notes/{id}` — owner deletes a note.

Team routes:

- `GET /api/v1/teams`
- `GET /api/v1/teams/{teamId}`
- `POST /api/v1/teams`
- `POST /api/v1/teams/{teamId}/members`

## Temporary Identity and Authentication

The current implementation uses the `X-User-Id` header to identify the actor. It is temporary demonstration behavior, not authentication. A production implementation should use validated JWT/OIDC claims and derive ownership and team membership from authoritative identity and authorization providers.

## Permission Model

- Owners can create, read, update, share, and delete notes.
- Direct shared users can read and update only when `AllowUpdates=true` in the owner-controlled share.
- Shared team members receive read access and may receive owner-granted update access.
- Shared users and team members cannot delete notes.
- A shared user may not extend sharing or change ownership.

## Storage and Repository Pattern

`ConcurrentDictionary` repositories provide process-local temporary note and team storage.

- `INoteRepository` contains note operations.
- `ITeamRepository` contains team and membership operations.
- `IUnitOfWork` exposes repositories and a commit boundary.
- `ConcurrentDictionaryUnitOfWork` is a temporary implementation where `CommitAsync` has no persistence operation.
- `NoteService` consumes `IUnitOfWork` rather than concrete repositories.

This abstraction provides a replacement boundary for EF Core or another persistent store.

## Async and Concurrency

Controllers, services, and repositories accept `CancellationToken` and use async methods. Note state is stored as immutable record snapshots and updated through concurrency-safe dictionary operations.

The repository is process-local. A future database implementation needs transactions, indexes, uniqueness constraints, optimistic concurrency, and access-control enforcement.

## Dependency Injection

`Program.cs` registers:

- `INoteRepository` → `ConcurrentDictionaryNoteRepository`
- `ITeamRepository` → `ConcurrentDictionaryTeamRepository`
- `IUnitOfWork` → `ConcurrentDictionaryUnitOfWork`
- `INoteService` → `NoteService`

Repositories are singletons because in-memory state must persist across requests. The service and unit-of-work are scoped.

## Test Coverage

Run:

```powershell
dotnet test SharedNotes.Tests/SharedNotes.Tests.csproj
```

Current tests cover:

- Team creation, naming, membership, sorting, and missing-team cases.
- Owner and team-member note creation.
- Direct-user and cross-team sharing.
- Read-only versus owner-granted update shares.
- Owner-only sharing and deletion.
- Missing notes, blank identifiers, duplicate shares, and input trimming.
- Concurrent note creation, updates, sharing, and deletion.
- HTTP headers, route status codes, payload validation, and create/read flows.
- Cancellation behavior.

## Security Checks

### GitHub CodeQL

`.github/workflows/codeql.yml` analyzes C# code on pushes and pull requests to `main`, weekly, and through manual workflow runs.

CodeQL results appear under **Security > Code scanning**. Workflow status appears under **Actions**.

### Dependabot

`.github/dependabot.yml` monitors NuGet packages and GitHub Actions dependencies weekly and may open update pull requests.

Dependabot alerts appear under **Security > Dependabot alerts**.

### Local Checks

```powershell
dotnet list SharedNotes.Api/SharedNotes.Api.csproj package --vulnerable --include-transitive
dotnet list SharedNotes.Tests/SharedNotes.Tests.csproj package --vulnerable --include-transitive
dotnet build SharedNotes.Api/SharedNotes.Api.csproj --configuration Release
dotnet test SharedNotes.Tests/SharedNotes.Tests.csproj
```

Local dependency checks found no vulnerable packages. These checks supplement rather than replace authentication, authorization testing, threat modeling, and secure configuration review.

## Deferred Work

With additional time, the next implementation priorities are:

1. Add JWT/OIDC authentication and authorization policies.
2. Replace `ConcurrentDictionary` with EF Core and a relational database.
3. Add migrations, constraints, indexes, transactions, and optimistic concurrency.
4. Add explicit read/update role permissions and audit history.
5. Add typed service outcomes and standardized Problem Details responses.
6. Improve forbidden-versus-not-found semantics.
7. Add pagination, filtering, sorting, quotas, logging, tracing, health checks, and rate limiting.

## Run

```powershell
dotnet restore
dotnet run --project SharedNotes.Api
```

Use `X-User-Id` during the temporary demonstration to identify the actor.
