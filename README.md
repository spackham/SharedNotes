# Shared Notes REST API

A small ASP.NET Core 10 Web API demonstrating a controller -> service -> repository design. Notes are stored only in memory using `ConcurrentDictionary`, so data is lost when the process stops.

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
 dotnet test
```

Tests cover owner access, shared read/write access, unauthorized access, owner-only sharing/deletion, and missing notes.

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
