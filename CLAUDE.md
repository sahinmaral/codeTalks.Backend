# codeTalks Backend

.NET 8 chat backend, Clean Architecture with a custom CQRS layer (`Core.Application.CQRS`: `ICommand`/`IQuery` + `IRequestHandler`, dispatched via `Dispatcher`). FluentValidation runs as a cross-cutting pipeline behavior; Mapster for mapping; ASP.NET Identity + JWT for auth; PostgreSQL via EF Core.

## Testing

Application-layer unit tests live in `tests/codeTalks.Application.Tests` (xUnit + FluentAssertions + NSubstitute). The test folder mirrors the source tree, e.g. `Features/Users/Commands/ChangeUserPassword/…`.

Run them with:

```bash
dotnet test tests/codeTalks.Application.Tests
```

### Pattern: two files per command

For each command write a `…ValidatorTests` and a `…CommandHandlerTests`:

- **Validator tests** are pure — no mocks. Use `new TheValidator().TestValidate(command)` with `ShouldHaveValidationErrorFor` / `ShouldNotHaveAnyValidationErrors` (FluentValidation.TestHelper).
- **Handler tests** call `handler.Handle(...)` directly with mocked dependencies and assert *behavior*: fields mutated, repository calls (`Received(1)` / `DidNotReceive()`), and exceptions thrown.

**Validation is cross-cutting.** `RequestValidationBehavior` runs in the dispatcher pipeline *before* the handler, so it does **not** run in handler tests (they call the handler directly). This is intentional: cover input rules in the validator tests; never write a handler test that feeds invalid input expecting a `ValidationException`.

### Test helpers (`tests/…/TestUtilities/`)

- `UserManagerMock.Create()` — substitutes the 9-arg `UserManager<User>` (its methods are virtual).
- `RoleManagerMock.Create()` — same for the 5-arg `RoleManager<Role>`.
- `TestAsyncQueryable.From(items)` — an EF Core async query provider. Required whenever a handler runs `FirstOrDefaultAsync`/`ToListAsync` over `UserManager.Users` or any in-memory queryable; a plain `AsQueryable()` throws *"source IQueryable doesn't implement IAsyncEnumerable"*.

Concrete business-rule classes (`AuthBusinessRules`, `ChannelBusinessRules`) have non-virtual methods, so build a **real** instance over the mocked repository/`UserManager` rather than mocking the rule. `IChannelRepository.GetDetailedAsync` is mocked with all four positional args (`Arg.Any<Expression<…>>()`, `Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>()`, `Arg.Any<bool>()`, `Arg.Any<CancellationToken>()`). Validators auto-register via `AddValidatorsFromAssembly` — no manual wiring.

### Conventions & gotchas

- **Skip empty validators.** If a command has nothing to constrain (only a `bool`, or no fields), don't add an `AbstractValidator` with no rules.
- **Which queries to test.** Unit-test a query handler only if it has authorization, branching, or a computed field (e.g. `GetById`, `GetUsersByChannelId`, `GetCurrentUser`). Skip thin repo-projection passthroughs — their real risk (EF filtering/projection/ordering/paging) is mocked away here and belongs to integration tests. For queries, assert the **arguments passed to the repository** (the decision), not the mocked results.
- **"Must be in the future" rules:** use `.Must(x => x > DateTime.UtcNow)`, not `.GreaterThan(DateTime.UtcNow)` — the latter captures "now" once when the validator is constructed.
- **Time is UTC.** Compare and default timestamps with `DateTime.UtcNow`, never `DateTime.Now` — stored expiries (JWT refresh, mute-until) are UTC.
- **Not-found / rule failures** should throw `EntityNotFoundException` (→ 404) or `BusinessException` (→ 400); a bare framework exception (e.g. `InvalidOperationException`) has no mapping in `ExceptionMiddleware` and surfaces as a 500.
- Paged repository returns are constructed with `new Paginate<T> { Items = new List<T>() }` (`Core.Persistence.Paging`).

### Not yet covered

Query handlers that are thin passthroughs; a test project for `Core.Security` (e.g. `JwtProvider`); and full-pipeline / HTTP integration tests (WebApplicationFactory + Testcontainers Postgres).