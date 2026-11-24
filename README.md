# Shared Notes REST API

## Bluestaq Review Summary

A. **Design choices**

- C# with ASP.NET Core 10 and Visual Studio Code, with GitHub Free Copilot used as a coding assistant.
- Controller → service → repository → unit-of-work separation with dependency injection.
- Team-scoped note sharing, separate `UserId` and `TeamId` identities, owner-controlled update permissions, and temporary `ConcurrentDictionary` storage.

B. **Primary design focus**

Most time was spent on the service boundary and authorization model: defining ownership, team membership, direct sharing, cross-team sharing, read/update permissions, and owner-only deletion before endpoint implementation details.

C. **What I would change with more time**

- Replace demo identity headers with JWT/OIDC authentication.
- Replace `ConcurrentDictionary` with EF Core, migrations, transactions, constraints, and optimistic concurrency.
- Add typed service outcomes, standardized HTTP errors, and stronger 403/404 testing.
- Add explicit roles, audit history, pagination, logging, tracing, health checks, and deployment configuration.

For detailed architecture, APIs, storage, testing, and security documentation, see [InformationDescription.md](InformationDescription.md) and [ArchitectureThoughts1Doc.md](ArchitectureThoughts1Doc.md).
