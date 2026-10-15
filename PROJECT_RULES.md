# Nexora Backend Engineering Rules

This document is the working engineering rule for the current Nexora Backend repository.
It describes conventions that fit this codebase. New code should follow these rules, while
unrelated legacy code should not be refactored without an explicit request.

## 1. Project baseline

- Runtime and target framework: .NET 10 (`net10.0`).
- API: ASP.NET Core Web API using MVC Controllers.
- Database: PostgreSQL.
- ORM: Entity Framework Core with Npgsql.
- Application messaging: MediatR.
- Validation: FluentValidation through MediatR pipeline behavior.
- API documentation: Swashbuckle/Swagger at `/swagger` and `/swagger/v1/swagger.json`.
- Tests: xUnit in `tests/Nexora.Tests`.
- Object storage: MinIO.
- Vector store: Qdrant.
- Document processing: TXT/MD, DOCX, PDF text extraction and OCR.

Do not assume that every domain model has a complete API. Project, Chat, Meeting, Task and
Calendar models exist, but their controllers and use cases may not be implemented.

## 2. Project dependency direction

The accepted dependency graph is:

```text
WebApi ───────┐
              ├──> Application ───> Domain
Infrastructure ┘
```

### Domain

- Contains domain entities, value objects, enums, domain errors and business invariants.
- Must not depend on Application, Infrastructure or WebApi.
- Must not contain controllers, DbContext, repositories or HTTP-specific code.
- Domain entities are classes because they have identity, lifecycle and behavior.

### Application

- Contains commands, queries, handlers, validators, DTOs and application abstractions.
- Depends on Domain and existing packages such as MediatR and FluentValidation.
- Must not reference the Infrastructure or WebApi project.
- Must not access `NexoraDbContext` directly.
- Use repository, unit-of-work and service abstractions defined in Application.
- Existing Identity types are a compatibility concern. Do not expand Application's coupling to
  Infrastructure namespaces; decouple them only as a separate explicit task.

### Infrastructure

- Implements persistence, EF Core configurations, repositories, Identity, JWT, storage,
  vector-store, embedding and background-processing integrations.
- Depends on Application and Domain.
- Keep EF Core configuration and external-service implementation here.

### WebApi

- Contains controllers, middleware, response mapping, HTTP configuration and composition root.
- Maps HTTP input to Application requests and dispatches through MediatR.
- Must not contain business rules or direct DbContext access.

## 3. Feature and CQRS conventions

The current Application convention is feature-first with shared subfolders:

```text
Features/V1/<Area>/<Feature>/
  Command/
  Query/
  Model/
  Validation/
```

Follow this existing layout unless a feature is deliberately reorganized. Do not move all
existing features into one-use-case-per-folder structure as part of an unrelated change.

- Commands modify state.
- Queries read state.
- Commands and queries are MediatR request records where practical.
- Handlers implement `IRequestHandler<TRequest, TResponse>`.
- Do not introduce a new `IHandler<TRequest, TResponse>` abstraction just to satisfy a rule.
- A command/query should carry input only; application decisions belong in its handler.
- Keep a command, its handler and its validator close to the feature. Existing files may contain
  both command and handler when that matches the local convention.
- Do not create shared service classes that collect unrelated use cases.

## 4. Records, classes and mutability

Do not convert every type to a record.

Use records for:

- Commands and queries.
- Immutable application responses and DTOs when practical.
- Small request models that work correctly with ASP.NET Core model binding.

Keep classes for:

- Domain entities and aggregates.
- EF Core/Identity persistence entities.
- Objects with lifecycle, mutable state or framework-managed binding requirements.
- Options/configuration objects.
- Common response infrastructure such as `ApiResponse<T>`, `Result<T>` and `PagedResult<T>`
  unless a focused change proves that conversion is safe.

When changing a model type:

- Preserve JSON property names and nullability behavior.
- Preserve constructors and model-binding behavior unless the contract intentionally changes.
- Do not change public API contracts only to make a type look more immutable.
- Do not mass-convert unrelated models.

## 5. Validation and application results

- Use FluentValidation for commands and queries with meaningful external input.
- Validators are registered by the existing assembly-scanning convention.
- Validation runs through `ValidationBehavior` before the handler.
- Validators validate input shape and format.
- Handlers enforce use-case rules and authorization decisions.
- Domain entities enforce domain invariants.
- Do not duplicate the same validation rules in controllers and handlers.

Use the existing result types:

- Commands normally return `Result` or `Result<T>`.
- Queries may return `Result<T>`, `PagedResult<T>`, a list, or a DTO according to the existing
  feature contract. Do not add wrappers mechanically.
- Expected business/application failures must use `Result` error values.
- Unexpected exceptions are handled centrally; do not expose stack traces or internal details.

## 6. Domain entities and audit fields

Do not require every persistent entity to inherit the same audit base class.

Use the appropriate existing base type for each entity:

- `EntityBase` for identity-only entities.
- `CreatedEntityBase` for create-only records.
- `AuditableEntityBase` for entities with update auditing.
- `SoftDeletableEntityBase` for soft deletion.
- Concurrency/composite/chunk-specific bases where the model requires them.

Preserve domain behavior and invariants. Do not put HTTP, JSON or controller concerns in entities.

Audit values are currently maintained by `NexoraDbContext` during `SaveChanges` and
`SaveChangesAsync`. Do not introduce an EF interceptor unless audit processing is explicitly
being redesigned.

## 7. Controllers and HTTP behavior

Controllers must:

- Use ASP.NET Core Controllers and attribute routing.
- Use `[ApiController]` directly or through the existing `BaseController` convention.
- Stay thin: bind input, create a command/query, dispatch it and map the result.
- Pass `CancellationToken` to asynchronous operations.
- Never access DbContext or EF entities directly.
- Never contain business rules.

Use the existing response envelope and mapping helpers. Preserve current status-code and error
mapping conventions when adding or changing endpoints.

Do not introduce Minimal APIs for application features.

## 8. Authentication and authorization

The current project uses JWT Bearer authentication plus custom middleware. Before changing
authorization, inspect these components together:

- `ControllerSecurityMetadata`.
- `JwtAuthenticationMiddleware`.
- `ApiPermissionMiddleware`.
- `ApiPermissionService`.
- `IUserContext` and `ITenantContext`.

Important current behavior:

- Custom middleware identifies protected controllers through `BaseController`.
- The permission service currently verifies that an active API catalog entry matches the HTTP
  method and route template; it does not by itself calculate effective Role grants.
- Do not assume that catalog existence is equivalent to effective user authorization.
- Do not add a new authorization mechanism without updating middleware tests and checking its
  interaction with existing JWT behavior.

When authorization is deliberately redesigned, prefer explicit ASP.NET Core policies/handlers
and make endpoints protected by default, with explicit anonymous authentication routes.

## 9. Persistence and asynchronous code

- Keep entity configurations in Infrastructure.
- Use asynchronous EF Core APIs such as `ToListAsync`, `FirstOrDefaultAsync`,
  `SingleOrDefaultAsync` and `SaveChangesAsync`.
- Propagate `CancellationToken` through controllers, handlers, repositories and integrations.
- Do not use `.Result`, `.Wait()` or unnecessary `Task.Run`.
- Use `AsNoTracking()` for read-only queries unless tracking is required.
- Avoid loading unnecessary columns or relationships.
- Do not create or modify migrations unless explicitly requested.
- Do not use `IgnoreQueryFilters()` casually; it must have a clear tenant/admin reason.

## 10. Multi-tenancy

The current business flow uses a server-side default Organization for compatibility with the
existing schema and authorization model.

- Never trust an OrganizationId supplied by the client for current tenant selection.
- Preserve server-side tenant resolution and EF query filters.
- Check organization scope explicitly when a repository intentionally bypasses query filters.
- Keep the distinction clear between organization membership, role assignment, role grants and
  request authorization.

## 11. RAG and document processing

The current RAG pipeline is an MVP:

```text
Upload → PostgreSQL metadata → MinIO file → in-memory queue → extraction/OCR
       → chunks → embeddings → Qdrant → READY/FAILED
```

- Preserve document status transitions and failure codes.
- Ensure embedding dimension matches Qdrant dimension before indexing.
- Keep provider-specific request/response formats inside the provider adapter.
- Pass cancellation to storage, process execution, embedding and vector-store calls.
- Treat the in-memory queue as non-durable MVP behavior; durable jobs/retry/resume require a
  separate design task.
- Do not add retrieval/chat use cases unless the application contract and access rules are defined.

## 12. Configuration and secrets

- Never commit database passwords, JWT signing keys, API keys or storage secrets.
- Prefer environment variables, user secrets or a secret manager.
- Preserve existing user configuration changes.
- Validate required configuration at application startup where the current options pattern supports it.
- Keep local development configuration separate from production configuration.

## 13. API catalog and endpoint changes

When adding or changing an endpoint, check all related sources:

1. Controller route and HTTP method.
2. API catalog synchronization handler.
3. Seed files under `scripts/`.
4. Permission and middleware tests.
5. API documentation or contract tests when the public contract changes.

Do not assume that the catalog, SQL seed and JSON seed are automatically synchronized.

## 14. Testing and verification

Before completing a source change:

1. Inspect `git status` and preserve unrelated user changes.
2. Build the affected project or solution.
3. Run the relevant xUnit tests.
4. Run `git diff --check`.
5. Confirm that migrations, secrets and unrelated configuration were not changed.

Standard commands:

```bash
dotnet restore Nexora.sln -m:1
dotnet build Nexora.sln --configuration Release --no-restore -m:1
dotnet test tests/Nexora.Tests/Nexora.Tests.csproj --configuration Release --no-build --no-restore -m:1
```

If package restore is blocked by the environment, report that limitation separately from code
failures. Do not reset or overwrite user changes to make the build pass.

## 15. Scope and change discipline

- Analyze the requested change before editing.
- Inspect related implementations and tests first.
- Make the smallest complete change.
- Preserve unrelated modifications.
- Do not modify migrations, generated output, secrets or production configuration unless the
  user explicitly requests it and the scope is clear.
- Do not refactor adjacent code merely because it could be cleaner.
- Report changed files, verification performed and remaining risks.
