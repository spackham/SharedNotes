# QA — How to Test

## 1. Start the API

From the solution folder:

```powershell
dotnet restore
dotnet run --project SharedNotes.Api
```

In development, OpenAPI is available at `/openapi/v1.json`.

## 2. Run Automated Tests

```powershell
dotnet test SharedNotes.Tests/SharedNotes.Tests.csproj
```

The suite includes service, repository, concurrency, team, and ASP.NET Core HTTP integration tests.

## 3. Run a Basic API Flow

The temporary implementation requires `X-User-Id` for note operations.

Create a team:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5008/api/v1/teams" -ContentType "application/json" -Body '{"name":"Red Team"}'
```

Use the returned team ID in the following examples.

Add a team member:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5008/api/v1/teams/{teamId}/members" -ContentType "application/json" -Body '{"userId":"alice"}'
```

Create a note:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5008/api/v1/teams/{teamId}/notes" -Headers @{ "X-User-Id"="alice" } -ContentType "application/json" -Body '{"title":"Trip","content":"Bring a map"}'
```

## 4. Test Sharing

Grant read and update access:

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5008/api/v1/teams/{teamId}/notes/{noteId}/share" -Headers @{ "X-User-Id"="alice" } -ContentType "application/json" -Body '{"userId":"bob","allowUpdates":true}'
```

## 5. Test Edge Cases

Verify the following:

- Missing `X-User-Id` returns `400`.
- A non-member cannot create a note.
- A shared read-only user can read but cannot update.
- A granted update user can update but cannot delete.
- Only the owner can share or delete.
- Wrong-team note routes return `404`.
- Missing note IDs return `404`.
- Blank titles and content return `400`.
- Re-sharing the same user is idempotent.
- Repeated concurrent creates produce unique note IDs.

## 6. Local Security and Build Checks

```powershell
dotnet build SharedNotes.Api/SharedNotes.Api.csproj --configuration Release
dotnet list SharedNotes.Api/SharedNotes.Api.csproj package --vulnerable --include-transitive
dotnet list SharedNotes.Tests/SharedNotes.Tests.csproj package --vulnerable --include-transitive
```

GitHub CodeQL and Dependabot checks are configured under `.github/`.

## 7. Run Tests Automatically on GitHub

The public repository includes `.github/workflows/tests.yml`. GitHub Actions runs the complete .NET test suite on:

- Pushes to `main`
- Pull requests targeting `main`
- Manual workflow dispatch

The workflow restores dependencies, builds the test project in Release configuration, and runs all automated tests.

GitHub Free is sufficient for this public repository. Results, logs, and pass/fail status appear under:

- **Repository > Actions > .NET Tests**

A successful run should report all 36 tests passing. GitHub Actions does not require a local development environment after the workflow file is committed and pushed.

## Known Temporary Limitation

The `X-User-Id` header is not authentication. Test results demonstrate architecture and authorization logic, not production identity security.
