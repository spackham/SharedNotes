# Required Fixes Document

## Fixed in the Easy-Fix Pass

### Team Route Scoping

- Read operations now require an owner-team membership/direct share or target-team share relationship.
- Update operations require either ownership or the explicit user/team update permission for the route team.
- Owners and direct-share recipients cannot access or update a note through an unrelated team route.
- Delete operations remain owner-only and require the note's source team route.

### Atomic Share Validation

- Direct-user and team-share targets are resolved before note mutation.
- Invalid team targets prevent direct-user shares from being partially applied.
- The combined repository share operation updates direct-user and team share lists in one in-memory compare-and-update operation.

### Retry-Safe In-Memory Writes

- Note updates retry `TryUpdate` after snapshot contention.
- Combined share operations retry after snapshot contention.
- Team-member additions retry `TryUpdate` after snapshot contention.
- Team-name insertion uses a name index for atomic case-insensitive duplicate protection in the process-local implementation.

### Integration Tests

The suite now includes tests for:

- Wrong-team owner and direct-share access.
- Wrong-team update attempts.
- Invalid combined shares not applying partial direct-user access.
- Existing read-only, unauthorized, missing-note, concurrent, and idempotent share cases.

## Moderate Fixes

### Typed Service Outcomes

Replace nullable and boolean service returns with typed outcomes that distinguish:

- Success.
- Invalid input.
- Missing resource.
- Forbidden operation.
- Conflict or concurrent update failure.
- Unauthorized identity.

This will improve controller status mapping and eliminate ambiguous `null`/`false` responses.

### HTTP Status and Problem Details

Add centralized `ProblemDetails` responses and consistent status mappings:

- `400 Bad Request`: malformed or invalid input.
- `401 Unauthorized`: missing or invalid authentication.
- `403 Forbidden`: authenticated identity lacks permission.
- `404 Not Found`: missing or intentionally hidden resource.
- `409 Conflict`: concurrent or version conflict.
- `204 No Content`: successful deletion.

### Share Revocation

Add operations to remove:

- Direct-user read access.
- Direct-user update permission.
- Team read access.
- Team update permission.

Owner-only endpoints should make revocation explicit rather than requiring a replacement share request.

### Meaningful Unit-of-Work Behavior

The temporary unit-of-work should either:

- Be removed as misleading infrastructure, or
- Be retained as a repository boundary while clearly stating that `CommitAsync` is process-local and does not provide transactions.

A future EF Core implementation should provide real transaction and rollback behavior.

### Immutable Repository Responses

Return immutable collections or copies instead of arrays that may be cast and mutated by callers.

### Team and Team-Member Service Boundary

Move team creation and membership rules into a team service rather than relying on direct team-repository controller dependencies.

### Status and Route Documentation

Document exact route semantics:

- Whether team-shared notes are accessible through both source and target team routes.
- Whether direct-share access is team-independent.
- Which route is authoritative for owner, update, share, and delete operations.

## Harder Fixes

### Real Authentication and Authorization

Replace `X-User-Id` with:

- JWT or OIDC authentication.
- Server-derived user and tenant claims.
- Team membership claims or server membership lookup.
- ASP.NET Core `[Authorize]` attributes and authorization policies.
- Authentication middleware and claims-principal handling.

This also requires test coverage for spoofed headers, expired tokens, malformed tokens, missing claims, and cross-team escalation.

### Persistent EF Core Repository

Replace `ConcurrentDictionary` implementations with EF Core:

- Notes table.
- Teams table.
- Team membership join table.
- Direct-user share join table.
- Team share join table.
- Update permission fields or permission join tables.
- Migrations and constraints.
- Database indexes.
- Foreign keys.
- Audit timestamps and optional audit history.

### Transactional Unit of Work

Use real database transactions and rollback semantics for combined share operations.

### Optimistic Concurrency

Add note versions or row versions and return `409 Conflict` when stale updates are attempted.

### Role-Based Authorization

Add owner, editor, viewer, team-admin, and platform-admin roles only after authentication is production-grade.

### Multi-Replica and Distributed Behavior

The current dictionaries cannot remain consistent across process replicas or restarts.

Use a database or distributed store for:

- Durable note state.
- Durable team membership.
- Durable share permissions.
- Atomic cross-resource operations.
- Cross-instance concurrency.

### Operational Hardening

Add:

- Pagination and filtering.
- Rate limiting.
- Quotas and resource limits.
- CORS policy.
- TLS configuration.
- Structured logging and correlation IDs.
- Audit logs.
- Health checks.
- Metrics and tracing.
- Secret management.
- Container and deployment configuration.

### CI and Repository Hardening

Add:

- Solution-wide restore/build/test.
- `global.json` for SDK pinning.
- Vulnerability scan gate.
- Test coverage threshold.
- Test report artifact upload.
- Secret scanning.
- CodeQL and Dependabot status review.
- Pull-request branch protection and required checks.

## Validation Status

The easy-fix pass should be considered complete only after:

- The full test suite passes.
- Wrong-team read/update tests pass.
- Invalid combined-share tests prove no partial mutation.
- A Release build succeeds.
- CI executes the complete test suite.

## Priority Recommendation

1. Authentication and authorization.
2. Typed outcomes and HTTP semantics.
3. EF Core persistence and transactions.
4. Optimistic concurrency and share revocation.
5. Operational hardening and role-based permissions.
