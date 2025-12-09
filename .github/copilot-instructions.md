## 10xCards – AI Coding Guide (Blazor Server + .NET 9)

Purpose: Concise rules for AI-assisted contributions. Stack: Blazor Server (.NET 9), ASP.NET Core, EF Core + SQLite. Styling: Bootstrap (no Tailwind). No MediatR – direct service injection.

### Solution Structure
- 10xCards.Domain – Entities, value objects, enums, pure logic. Enforce invariants in constructors/factories.
- 10xCards.Application – Application services (use-cases), DTOs, interfaces (ICardService, IProposalService). No MediatR.
- 10xCards.Persistance – EF Core DbContext (CardsDbContext), configurations, migrations (SQLite). No business logic.
- 10xCards (Web) – Blazor Server host, Program.cs, DI, Razor Components, optional MVC controllers.
- Tests: 10xCards.Unit.Tests (domain/application), 10xCards.Integration.Tests (host + real DbContext temp SQLite).

### Frontend (Blazor + Bootstrap)
- Razor Components only; keep logic minimal.
- Use code-behind partial classes if component logic > ~40 lines.
- Navigation via <NavLink>. Lowercase short routes.
- Avoid JS interop unless necessary (wrap via service).
- Use Bootstrap utilities/components; minimize custom CSS.

### Controllers
- Standard controllers in Controllers/.
- Direct DI: public SessionIdResponse GetSessionId([FromServices] ISessionIdProvider provider).
- Return DTOs, never EF entities.
- Validate inputs via DataAnnotations (FluentValidation optional later).

### Domain Model (initial)
Entities: Card, CardProposal, SourceText, CardProgress, AcceptanceEvent, GenerationError.
SrState stored as JSON string; wrap later in value object.
Lengths (Front/Back) 50–500 enforced in domain.

### EF Core Guidelines
- Separate IEntityTypeConfiguration<T> classes.
- Guid keys, UTC DateTime (suffix Utc). SrState TEXT.
- Indices per db-plan (UserId + NextReviewUtc, GenerationMethod + CreatedUtc).
- No lazy loading. Use AsNoTracking for reads.
- Keep transactions minimal.

### Services Pattern
- Interfaces reflect use-cases (AcceptProposalAsync, ReviewCardAsync).
- Async for I/O; pure computation sync.
- Return result/DTO types (not bool).
- Flow: Validate → Load → Domain logic → Persist → Map DTO.

### Spaced Repetition (MVP)
- ReviewResult: Again/Good/Easy.
- Update: increment ReviewCount; compute NextReviewUtc; adjust ease/interval in SrState JSON.
- SrState extensible without migrations.

### Testing
Unit:
- Domain invariants.
- SR interval calculations.
- Proposal acceptance logic via mocked repository interfaces.

Integration:
- Host factory.
- Temp SQLite file (unique per test run).
- Controller endpoints + optionally component interaction via bUnit (later).

Conventions:
- Arrange / Act / Assert separated by blank lines.
- Deterministic time (introduce ITimeProvider when needed).
- Test data helpers (TestData.SeedCards).

### DI Registration (later expansion)
builder.Services.AddDbContext<CardsDbContext>(o => o.UseSqlite(conn));
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IProposalService, ProposalService>();
(Add Identity when auth is introduced.)

### Performance & Quality
- Avoid N+1 queries (Include only when needed).
- Use projection to lightweight DTOs.
- CancellationToken for long-running operations (AI generation later).
- Keep services < ~200 LOC.

### Coding Standards
- C# 12, file-scoped namespaces.
- Nullable enabled; no #nullable disable.
- Mark non-inheritable classes sealed.
- Use readonly record struct for small immutable values.

### Feature Checklist
1. Domain change (entity/value object).
2. Service interface + implementation.
3. Persistence mapping/migration if needed.
4. DI registration.
5. UI component/controller.
6. Unit + integration tests.
7. Update .ai docs (db-plan / summary) if schema changed.

### Avoid
- MediatR / CQRS split at MVP stage.
- Returning EF entities directly.
- Business logic inside Razor components.
- Static mutable singletons.

### Bootstrap Usage
- Use built-in components (alerts, cards, navbar).
- Form validation: EditForm + DataAnnotations + ValidationSummary.
- Provide focus styles (default Bootstrap acceptable).

### Future Extensions
- ITimeProvider abstraction.
- Strongly typed SrState value object + EF converter.
- Identity + roles (Admin).
- Migration SQLite → PostgreSQL (partial indexes, RLS, JSONB).
- AI generation service abstraction (OpenRouter integration).

### Quality Checklist Before Commit
1. dotnet build succeeds.
2. dotnet test all green.
3. No warnings in Domain/Application.
4. Public APIs documented (XML comments if complex).
5. Validation rules covered by tests.

Ask for clarification before changing architectural patterns.