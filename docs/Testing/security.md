# Security Checks Document 1

## Scope

This document records the initial local and GitHub security checks for the Shared Notes REST service.

## Local Checks Performed

### 1. API Dependency Vulnerability Scan

Command:

```powershell
dotnet list SharedNotes.Api/SharedNotes.Api.csproj package --vulnerable --include-transitive
```

Result: **No vulnerable NuGet packages were reported for the API project or its transitive dependencies.**

The scan used NuGet.org and the configured local NuGet package source.

### 2. Test Dependency Vulnerability Scan

Command:

```powershell
dotnet list SharedNotes.Tests/SharedNotes.Tests.csproj package --vulnerable --include-transitive
```

Result: **No vulnerable NuGet packages were reported for the test project or its transitive dependencies.**

### 3. Release Build

Command:

```powershell
dotnet build SharedNotes.Api/SharedNotes.Api.csproj --configuration Release
```

Result: **Passed.** The API compiled successfully for Release configuration.

### 4. Automated Test Suite

Command:

```powershell
dotnet test SharedNotes.Tests/SharedNotes.Tests.csproj --no-restore
```

Result: **30 tests passed, 0 failed** during the most recent execution.

## GitHub Automated Security Checks

### CodeQL

The repository contains `.github/workflows/codeql.yml`.

It is configured to:

- Analyze C# code.
- Run on pushes to `main`.
- Run on pull requests targeting `main`.
- Run weekly on a Monday schedule.
- Support manual workflow execution.
- Build the API in Release configuration before analysis.

When pushed and enabled by GitHub, CodeQL results should appear under:

- **GitHub repository > Actions** for workflow execution status.
- **GitHub repository > Security > Code scanning** for findings.

CodeQL is a static application security analysis tool. It may identify patterns and potential vulnerabilities in source code, but it does not prove that an application is secure.

### Dependabot

The repository contains `.github/dependabot.yml`.

It is configured to check weekly for updates to:

- NuGet packages.
- GitHub Actions dependencies.

Dependabot may create pull requests or security alerts for vulnerable dependencies. Results appear under:

- **GitHub repository > Security > Dependabot alerts**.
- **Pull requests** for dependency update proposals.

## Current Security Observations

### Positive Findings

- The API and test dependency scans reported no vulnerable packages.
- The API builds successfully in Release configuration.
- The project has CodeQL and Dependabot automation files.
- The repository uses interface-based repositories and a unit-of-work boundary.
- Owner-only deletion and owner-granted update sharing are covered by tests.

### Known Temporary Risks

- `X-User-Id` is demonstration-only. It is not authentication and should not be trusted in a deployed service.
- The in-memory `ConcurrentDictionary` stores no durable data and provides no database authorization policy.
- Null and boolean service outcomes can hide the difference between missing resources and forbidden operations.
- HTTP integration tests use a development server and temporary header identity.
- The team, user, and note model has not been reviewed under real JWT/OIDC claims and role policies.
- The repository is public and source-level findings or configuration mistakes are visible.

## Recommended Next Security Checks

1. Replace demo headers with JWT/OIDC authentication and authorization policies.
2. Add tests for expired, malformed, missing, spoofed, and unauthorized identities.
3. Add explicit `401 Unauthorized`, `403 Forbidden`, and `404 Not Found` behavior tests.
4. Add cross-team isolation and permission-escalation tests.
5. Add EF Core transactions, constraints, and database authorization review.
6. Add secret scanning and repository secret/credential checks.
7. Add container, OS, and third-party dependency scanning if deployment packaging is introduced.
8. Add rate limiting, HTTPS enforcement, CORS policy review, structured logging, and security headers.
9. Review CodeQL findings after the workflow is enabled and run in GitHub Actions.

## Limitations

These checks provide a useful baseline but do not establish a complete security certification. Production readiness would require threat modeling, authentication review, authorization testing, dependency lifecycle management, secure configuration, deployment review, and monitoring.
