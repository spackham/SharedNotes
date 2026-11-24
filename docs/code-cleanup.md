# Cleanup Document 1 — Clarity Refactors

## Purpose

This document records the first cleanup pass made to improve readability, reduce repetition, and preserve testability without changing the evaluation project's core behavior.

## 1. Simplify NoteService Authorization Logic

### Change

Repeated authorization conditionals in `NoteService` were extracted into private helper methods.

The helpers centralize:

- Retrieving a note.
- Checking team membership.
- Checking team existence.
- Determining whether a user can read a note.
- Determining whether a user can update a note.
- Determining whether a user can delete a note.
- Determining whether a user can share a note.

### Why

The public service methods now describe the operation rather than repeating nested permission expressions. This makes the main create, read, update, delete, and share flows easier to review and reduces the risk of inconsistent authorization checks.

### Result

Authorization behavior remains in the service layer, where it belongs. The code is less repetitive while avoiding a larger permission framework that would exceed the time-boxed scope.

## 2. Centralize User Header Parsing

### Change

Repeated `X-User-Id` parsing in `NotesController` was replaced with an identity abstraction:

- `ICurrentUser` exposes `TryGetUserId(out string userId)`.
- `HeaderCurrentUser` reads the temporary `X-User-Id` header from the HTTP context.
- `NotesController` receives `ICurrentUser` through dependency injection.

ASP.NET Core registers `IHttpContextAccessor` and the scoped `ICurrentUser` implementation in `Program.cs`.

### Why

The controller no longer needs to know how the temporary user identity is decoded. Repeated header checks are removed, and the identity source can later be replaced with JWT or OIDC claims without changing every endpoint method.

### Temporary Scope

`X-User-Id` is demonstration-only and must not be considered authentication or authorization in a production system.

## 3. Share Service Test Setup

### Change

The repeated construction of `ConcurrentDictionaryNoteRepository`, `ConcurrentDictionaryTeamRepository`, `ConcurrentDictionaryUnitOfWork`, and `NoteService` was moved to `SharedNotesTestFixture`.

`NoteServiceTests` now uses `IClassFixture<SharedNotesTestFixture>` and obtains a fresh service/repository setup for each test.

### Why

Tests describe scenarios more directly because repository construction is no longer duplicated in the test methods. The fixture keeps setup in one place while preserving isolated state between tests.

## Validation

The cleanup pass was validated with:

- **29 tests passed**.
- **0 tests failed**.
- API and test projects built successfully.
- VS Code reported no source errors.

## Deferred Improvements

Remaining clarity work could include:

- Typed service outcomes instead of nullable results.
- Centralized HTTP problem responses.
- A real identity abstraction backed by JWT/OIDC claims.
- A reusable HTTP integration fixture for endpoint tests.
- A formal authorization policy framework if the permission model becomes more complex.
