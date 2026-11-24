# Shared Notes REST API

A small ASP.NET Core 10 Web API demonstrating a controller -> service -> repository design. Notes are stored only in memory using `ConcurrentDictionary`, so data is lost when the process stops.

## Bluestaq Review Summary

- **Built with:** C# and ASP.NET Core 10 in Visual Studio Code, using GitHub Free Copilot as a coding assistant.
- **Design choices:** Controller → service → repository separation; teams and users as separate identities; temporary `ConcurrentDictionary` storage for fast, thread-safe in-memory demonstration.
- **If time allowed:** Add persistent EF Core storage and migrations, JWT/OIDC authentication, team/role authorization, integration tests, optimistic concurrency, and deployment/observability configuration.

### Review and Commit Plan

- **Commit 1 — Repository setup:** Established the GitHub repository and the initial ASP.NET Core solution structure.
- **Commit 2 — Design implementation:** Mostly design work, including the controller → service → repository structure, `UserId` and `TeamId` identity, temporary `ConcurrentDictionary` storage, REST endpoint drafts, security configuration, and initial tests.
- **Next phase — Design review:** Perform a design review, reconsider the endpoint routes and contracts, make required endpoint changes, improve test coverage, and complete another code review. Commit 2 will represent the reviewed and improved design state before the final implementation pass.

---

## Run

```powershell
 dotnet restore
 dotnet run --project SharedNotes.Api
```

Swagger/OpenAPI is available at `/openapi/v1.json` in development.

## Temporary Authentication

A real authentication system would be required for production. For this exercise, send `X-User-Id: alice` on requests. The header identifies the actor and is enforced by `NoteService`.

## API

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/notes` | Notes visible to the current user |
| GET | `/api/v1/notes/{id}` | Read a note the user owns or can access |
| POST | `/api/v1/notes` | Create a note |
| PUT | `/api/v1/notes/{id}` | Replace a note's title and content |
| POST | `/api/v1/notes/{id}/share` | Owner grants read/write access to another user |
| DELETE | `/api/v1/notes/{id}` | Owner deletes a note |

Example:

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/v1/notes -Headers @{ 'X-User-Id'='alice' } -ContentType 'application/json' -Body '{"title":"Trip","content":"Bring a map"}'
```

## Tests

```powershell
 dotnet test SharedNotes.Tests/SharedNotes.Tests.csproj
```

The test suite covers:

- Team creation, naming, membership, sorting, and missing-team cases.
- Team-member authorization for creating notes.
- Direct-user and cross-team sharing and access rules.
- Owner-only sharing and deletion.
- Missing notes, blank identifiers, input trimming, and idempotent sharing.
- Concurrent note creation, updates, sharing, and deletion.
- HTTP headers, route status codes, validation, team creation, and note create/read flows.
- Cancellation behavior.

Tests cover owner access, shared read/write access, unauthorized access, owner-only sharing/deletion, and missing notes.

## Security Checks

The repository includes automated and local security checks:

- **GitHub CodeQL:** `.github/workflows/codeql.yml` analyzes C# code on pushes and pull requests to `main`, on a weekly schedule, and through manual workflow runs.
- **Dependabot:** `.github/dependabot.yml` checks NuGet and GitHub Actions dependencies weekly and can open update pull requests.
- **Local dependency scan:** Run `dotnet list package --vulnerable --include-transitive` for the API and test projects.
- **Release build:** Run `dotnet build SharedNotes.Api/SharedNotes.Api.csproj --configuration Release` for a full Release compilation.

CodeQL results appear in the GitHub repository under **Security > Code scanning** after the workflow runs. Dependabot alerts appear under **Security > Dependabot alerts**. Workflow execution status is visible under **Actions**.

## If More Time Were Available

- Replace `ConcurrentDictionary` with EF Core and a transactional database.
- Add JWT/OIDC authentication and derive the user ID from validated claims.
- Add roles/permissions (read-only shares, ACL changes, audit history).
- Support optimistic concurrency using ETags or a version field.
- Add integration tests using `WebApplicationFactory`, validation tests, and concurrent writes.
- Add pagination, filtering, sorting, quotas, and idempotency keys.
- Centralize Problem Details responses and API logging/tracing.
- Configure TLS, CORS, rate limits, secret management, and health checks.
- Add migrations, persistence, backups, and container deployment.
