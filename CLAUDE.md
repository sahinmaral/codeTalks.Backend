# codeTalks Backend

.NET 8 chat backend, Clean Architecture with a custom CQRS layer (`Core.Application.CQRS`: `ICommand`/`IQuery` + `IRequestHandler`, dispatched via `Dispatcher`). FluentValidation runs as a cross-cutting pipeline behavior; Mapster for mapping; ASP.NET Identity + JWT for auth; PostgreSQL via EF Core.

## Testing

Application-layer unit tests live in `tests/codeTalks.Application.UnitTests` (xUnit + FluentAssertions + NSubstitute). The test folder mirrors the source tree, e.g. `Features/Users/Commands/ChangeUserPassword/…`.

Run them with:

```bash
dotnet test tests/codeTalks.Application.UnitTests
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

## Integration testing

HTTP-pipeline integration tests live in `tests/codeTalks.WebAPI.IntegrationTests` (xUnit + FluentAssertions + NSubstitute + `Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql`). They boot the **real** WebAPI host through the full pipeline (routing → auth → FluentValidation behavior → handler → EF Core) against a throwaway Postgres container.

```bash
dotnet test tests/codeTalks.WebAPI.IntegrationTests
```

Requires a running Docker daemon; Testcontainers auto-detects the active Docker context (no `DOCKER_HOST` needed with Docker Desktop). `Program` is made testable by `public partial class Program;` at the end of `Program.cs`.

### Fidelity: Postgres real, the rest faked

`CustomWebApplicationFactory` (in `Infrastructure/`) starts a `postgres:16` container and, via `ConfigureTestServices`, replaces the external infra the app touches at boot:

- **Postgres** is real — EF Core, ASP.NET Identity, and the `Database.Migrate()` that `Program` runs on startup all execute for real against the container. The container connection string is injected through `ConfigureAppConfiguration` (`ConnectionStrings:PostgreSQLConnectionString`). This only works because `PersistanceServiceRegistration` reads the connection string **lazily inside the `AddDbContext` options callback** (`options.UseNpgsql(configuration.GetConnectionString(...))`), not into a variable captured beforehand — an eager read there would resolve before the override is merged in and silently fall back to `appsettings.Development.json`'s hardcoded value (as it did until this was fixed; the tests were unknowingly running against a local dev Postgres instead of the throwaway container).
- **Redis** — `AddInfrastructureService` calls `ConnectionMultiplexer.Connect` **eagerly at registration time**, reading `configuration["Redis:ConnectionString"]` directly rather than through a lazily-evaluated factory. That read happens before `WebApplicationFactory`'s `ConfigureAppConfiguration` override is merged in, so the override is inert — the eager connect always uses `appsettings.Development.json`'s literal `localhost:6379`. A real Redis must therefore be reachable at that address wherever these tests run (a local Redis on `6379` when running locally, a `redis` service container in CI — see below). `ConfigureTestServices` then swaps `IConnectionMultiplexer` for an NSubstitute stub for the rest of the test run.
- **RabbitMQ** — the `ChannelMessageFanoutWorker` hosted service is removed (matched by `ImplementationType`) and `IMessagePublisher` is replaced with `NoOpMessagePublisher`. Note `RabbitMqPublisher`'s constructor opens a real connection, so it must never be resolved in tests.
- **Cloudinary** — `ICloudinaryService` replaced with a substitute.
- **User-settings cache** — `IUserSettingsCache` (Redis-backed) replaced with `NoOpUserSettingsCache`, so the settings handlers (notification / channel-mute) run without depending on the stubbed multiplexer. Postgres stays the source of truth.
- **Unread tracker** — `IUnreadTracker` (Redis-backed) replaced with an in-memory `FakeUnreadTracker` **singleton**, so notification-count tests seed counts (`tracker.Seed(userId, channelId, n)`) and the handler reads them back. Register replacement fakes as the same lifetime the tests need: singleton here so seeded state survives into the request scope.

### Harness shape

- Environment is set to **`Development`** so `appsettings.Development.json` loads — the base `appsettings.json` carries no `JwtOptions`, which `AddSecurityServices` requires.
- One container + one host per assembly: `CustomWebApplicationFactory : IAsyncLifetime` starts/stops the container, shared via the `"Integration"` collection fixture (`IntegrationTestCollection`). `IAsyncLifetime` is implemented **explicitly** because `WebApplicationFactory` already exposes a `ValueTask DisposeAsync()`.
- Test classes derive from `IntegrationTestBase`, which gives:
  - `Client` (unauthenticated) and `CreateAuthenticatedClient(accessToken)` (bearer-token client).
  - `CreateScope()` — a DI scope for arranging/asserting straight against container-backed services (`AppDbContext`, `UserManager<User>`). `AppDbContext` exposes no `DbSet` properties, so query domain tables via `db.Set<T>()`.
  - `RegisterAsync()` / `RegisterAndLoginAsync()` → returns `AuthenticatedUser` (credentials + tokens). Every non-Auth slice should acquire a token through these rather than re-posting to `/api/auth`.
  - `JsonWebOptions` (`JsonSerializerDefaults.Web`) for reading responses back into the real Application DTOs.
- **Shared DB across the assembly** → tests must self-isolate. `TestUsers.New()` mints a unique username/email per call (Guid `N` format — all chars are within Identity's `AllowedUserNameCharacters`); never hard-code a username. `CreateChannelAsync` names channels uniquely and reads the created row back by name (create returns 204 with no body). Higher-level helpers: `CreateUserAsync()` → `(AuthenticatedUser, HttpClient)`; `CreateChannelAsync(client, joinPolicy)` → `ChannelInfo(Id, InviteCode, Name)`.
- **Roles `Owner`/`User`/`Moderator` are seeded by migration** (`AddOwnerRoleToRoles` + `RoleConfiguration.HasData`), so they exist in the fresh container — `CreateChannel`/`JoinChannel`/`ChangeUserRole` resolve them via `RoleManager`. Per-channel role lives on `ChannelUser.RoleId`; assert against `RoleManager.FindByNameAsync(name).Id`, not a claim.
- **Global query filters** hide soft-deleted data: `Channel` has `HasQueryFilter(c => c.IsActive)` and `ChannelUser`/`Message` filter on `Channel.IsActive`. After a sole-owner `leave` (which sets `IsActive=false`), assert the row with `db.Set<Channel>().IgnoreQueryFilters()`.

### Covered / not yet covered

Covered: the harness (401 smoke + DB reachability); the **Auth slice** (`Features/Auths/AuthTests.cs`) — register (201 + seeded `UserStatus`/`UserNotificationSetting`, validation 400, duplicate-username 400), login (200 + JWT shape + persisted refresh token, wrong-password 400, unknown-user 404), refresh (rotation + old-token invalidation, invalid 401, expired 401); the **Channels slice** (`Features/Channels/ChannelTests.cs`) — create (owner seeded Accepted, validation 400), GetById (member 200 with role+count, non-member 400, unknown 404), join (Open→Accepted, Request→RequestSent, bad invite 404, already-joined 400), ChangeUserRole (owner promotes, non-owner 403), RemoveMember (owner removes, self 403), leave (sole-owner soft-delete, owner-with-members 403, non-member 404); the **Channel admin suite** (`Features/Channels/ChannelAdminTests.cs`) — PatchUserStatus join-request workflow (accept/deny/ban, non-admin 403, accept-on-Open 400, self 403, RequestSent-status 400), update/patch/delete (owner mutates name/description/join-policy/soft-delete, non-owner 403), GetChannels discovery list (shows non-joined, hides joined, title filter), GetUsersByChannelId (members in Items + owner in Admins, non-member 400); the **Messages slice** (`Features/Messages/MessageTests.cs`) — create (persisted + 200, no-token 403, unknown-channel 404, empty-content 400), list (oldest-first with sender, page-size paging, empty page); the **Users slice** (`Features/Users/UserTests.cs`) — GET /me (401 no-token, 200 with status + joined-channel-count), status (persisted, out-of-enum 400), password (new credential works + old rejected, wrong-current 400, same-as-current 400), profile info (401 without token, persisted, short-name 400), notification settings (seeded defaults, sound-flag persisted), channel mute (mute→list, past-date 400, unmute removes, unmute-when-not-muted 400); the **Devices slice** (`Features/Devices/DeviceTests.cs`) — register (401 no-token, persisted, idempotent on duplicate token), remove (deletes, unknown-token no-op 200); the **Notifications slice** (`Features/Notifications/NotificationTests.cs`) — channel unread count reflects tracker, total sums Accepted channels, zero without activity, reset clears a channel; the **Photos slice** (`Features/Photos/PhotoTests.cs`) — profile photo (`[Authorize]` → 401 without token, upload persists `ProfilePhotoURL` + returns path, invalid content-type 400, re-upload replaces and deletes the previous Cloudinary asset, delete clears URL, delete-when-none 400) and channel thumbnail (owner upload persists `ThumbnailPhotoURL`, non-owner-member 403, unknown-channel 404, invalid content-type 400, `[Authorize]` → 401 without token, owner delete clears URL, delete-when-none 400, non-owner delete 403).

**`/api/messages` carries no `[Authorize]`** (unlike Channels/Users): create still needs identity because the handler resolves the current user (missing token → `AuthorizationException` → **403, not 401**), and it only checks the channel *exists* — not membership — so any authenticated user can post to any channel. Listing is fully open (no auth, no channel check). CreateMessage publishes a `ChannelMessageCreatedEvent` through the faked `IMessagePublisher`, so nothing hits RabbitMQ. The list handler orders by `CreatedAt` desc then reverses, so a page reads oldest→newest.

**`GET /api/channels` is a discovery list, not "my channels"** — it returns channels the caller has *not* joined (`ChannelUsers.All(u => u.UserId != me)`), ordered by name and paged. Because the assembly shares one DB, that list spans every other test's channels, so a specific channel may land on any page: **isolate with a unique channel name + the `title=` filter** rather than asserting membership on an unfiltered page.

The photo endpoints do their multipart binding + FluentValidation + owner-authorization + persistence for real against Postgres; only the Cloudinary calls themselves are faked (upload returns a canned secure URL, delete is asserted via `Received`), so the assertions cover everything except the actual asset transfer.

Not yet covered: fan-out / notification *delivery* is faked, so RabbitMQ + Redis behavior is not exercised end-to-end (the unread *counting* path is tested via the fake tracker). Also still open: query handlers that are thin passthroughs.

## Continuous integration

`.github/workflows/ci.yml` runs on every push and PR targeting `main`, on a GitHub-hosted `ubuntu-latest` runner: restore → `dotnet build --configuration Release` → `codeTalks.Application.UnitTests` → `codeTalks.WebAPI.IntegrationTests`. The job name GitHub reports is `build-and-test`.

- **Docker** is preinstalled on the runner, so `codeTalks.WebAPI.IntegrationTests`' Testcontainers-managed Postgres container works with no extra setup.
- **Redis** does not come for free — because `AddInfrastructureService` connects to Redis eagerly at registration time (see above), the workflow declares a `redis:7-alpine` `services` container mapped to `localhost:6379` so that connection succeeds on the runner the same way it does on a machine with a local Redis.

**`main` is protected**: pushes go through a PR, and the `build-and-test` check must pass (with the branch up to date) before the merge button unlocks — enforced for admins too, so there's no direct-push or bypass path. Day-to-day workflow is: branch → PR → wait for the check → merge.