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

HTTP-pipeline integration tests live in `tests/codeTalks.WebAPI.IntegrationTests` (xUnit + FluentAssertions + NSubstitute + `Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql` + `Testcontainers.RabbitMq` + `Testcontainers.Redis`). They boot the **real** WebAPI host through the full pipeline (routing → auth → FluentValidation behavior → handler → EF Core) against throwaway Postgres, RabbitMQ, and Redis containers.

```bash
dotnet test tests/codeTalks.WebAPI.IntegrationTests
```

Requires a running Docker daemon; Testcontainers auto-detects the active Docker context (no `DOCKER_HOST` needed with Docker Desktop). `Program` is made testable by `public partial class Program;` at the end of `Program.cs`.

### Fidelity: Postgres, RabbitMQ, and Redis are real; only third-party calls are faked

`CustomWebApplicationFactory` (in `Infrastructure/`) starts `postgres:16`, `rabbitmq:3.13-alpine`, and `redis:7-alpine` containers and, via `ConfigureTestServices`, replaces only the infra that would otherwise make a genuine external network call:

- **Postgres** is real — EF Core, ASP.NET Identity, and the `Database.Migrate()` that `Program` runs on startup all execute for real against the container. The container connection string is injected through `ConfigureAppConfiguration` (`ConnectionStrings:PostgreSQLConnectionString`). This only works because `PersistanceServiceRegistration` reads the connection string **lazily inside the `AddDbContext` options callback** (`options.UseNpgsql(configuration.GetConnectionString(...))`), not into a variable captured beforehand — an eager read there would resolve before the override is merged in and silently fall back to `appsettings.Development.json`'s hardcoded value (as it did until this was fixed; the tests were unknowingly running against a local dev Postgres instead of the throwaway container).
- **Redis** is real too, for the same reason: `AddInfrastructureService` originally called `ConnectionMultiplexer.Connect(...)` **eagerly at registration time** instead of through a lazy factory, so the `ConfigureAppConfiguration` override was inert (it merges in only when `builder.Build()` runs, which is later). Fixed by registering it as `services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(...))` — a factory resolved on first use, well after the override is in place. That's what unblocked using a dynamically-ported Testcontainers Redis at all (previously the workaround was pinning a real Redis to the hardcoded `localhost:6379`, including a dedicated `redis` service container in CI — no longer needed).
- **RabbitMQ** is real — `ChannelMessageFanoutWorker` runs as a genuine hosted service, consuming from the container the same way `RabbitMqPublisher` publishes to it. Testcontainers assigns a random host port, so `RabbitMqOptions` gained a `Port` property (default `5672`, so prod/dev config is unaffected) that both places construct their `ConnectionFactory` from.
- **Cloudinary** — `ICloudinaryService` replaced with a substitute (no real uploads in tests).
- **Push notifications** — `IPushNotificationProvider` (Expo) replaced with a substitute. This is the one fake that became *necessary* once the fan-out worker started running for real: `ExpoPushNotificationProvider` calls the real `https://exp.host` API, which must never happen from a test.
- **User-settings cache** — `IUserSettingsCache` (Redis-backed) replaced with `NoOpUserSettingsCache`, so the settings handlers (notification / channel-mute) don't need Redis round trips in every settings test. Postgres stays the source of truth. Orthogonal to the fan-out pipeline — kept faked even though Redis itself is now real.

### Harness shape

- Environment is set to **`Development`** so `appsettings.Development.json` loads — the base `appsettings.json` carries no `JwtOptions`, which `AddSecurityServices` requires.
- Three containers + one host per assembly: `CustomWebApplicationFactory : IAsyncLifetime` starts/stops Postgres, RabbitMQ, and Redis together, shared via the `"Integration"` collection fixture (`IntegrationTestCollection`). `IAsyncLifetime` is implemented **explicitly** because `WebApplicationFactory` already exposes a `ValueTask DisposeAsync()`.
- Test classes derive from `IntegrationTestBase`, which gives:
  - `Client` (unauthenticated) and `CreateAuthenticatedClient(accessToken)` (bearer-token client).
  - `CreateScope()` — a DI scope for arranging/asserting straight against container-backed services (`AppDbContext`, `UserManager<User>`). `AppDbContext` exposes no `DbSet` properties, so query domain tables via `db.Set<T>()`.
  - `RegisterAsync()` / `RegisterAndLoginAsync()` → returns `AuthenticatedUser` (credentials + tokens). Every non-Auth slice should acquire a token through these rather than re-posting to `/api/auth`.
  - `JsonWebOptions` (`JsonSerializerDefaults.Web`) for reading responses back into the real Application DTOs.
  - `WaitUntilAsync(condition, timeout?, interval?)` — polls until true or a bounded timeout, throwing on timeout. Needed for the message-delivery tests: RabbitMQ publish → background worker → Redis write happens off the request thread, so those assertions can't check synchronously the way every other test in this suite does.
- **Shared DB across the assembly** → tests must self-isolate. `TestUsers.New()` mints a unique username/email per call (Guid `N` format — all chars are within Identity's `AllowedUserNameCharacters`); never hard-code a username. `CreateChannelAsync` names channels uniquely and reads the created row back by name (create returns 204 with no body). Higher-level helpers: `CreateUserAsync()` → `(AuthenticatedUser, HttpClient)`; `CreateChannelAsync(client, joinPolicy)` → `ChannelInfo(Id, InviteCode, Name)`.
- **Roles `Owner`/`User`/`Moderator` are seeded by migration** (`AddOwnerRoleToRoles` + `RoleConfiguration.HasData`), so they exist in the fresh container — `CreateChannel`/`JoinChannel`/`ChangeUserRole` resolve them via `RoleManager`. Per-channel role lives on `ChannelUser.RoleId`; assert against `RoleManager.FindByNameAsync(name).Id`, not a claim.
- **Global query filters** hide soft-deleted data: `Channel` has `HasQueryFilter(c => c.IsActive)` and `ChannelUser`/`Message` filter on `Channel.IsActive`. After a sole-owner `leave` (which sets `IsActive=false`), assert the row with `db.Set<Channel>().IgnoreQueryFilters()`.

### Covered / not yet covered

Covered: the harness (401 smoke + DB reachability); the **Auth slice** (`Features/Auths/AuthTests.cs`) — register (201 + seeded `UserStatus`/`UserNotificationSetting`, validation 400, duplicate-username 400), login (200 + JWT shape + persisted refresh token, wrong-password 400, unknown-user 404), refresh (rotation + old-token invalidation, invalid 401, expired 401); the **Channels slice** (`Features/Channels/ChannelTests.cs`) — create (owner seeded Accepted, validation 400), GetById (member 200 with role+count, non-member 400, unknown 404), join (Open→Accepted, Request→RequestSent, bad invite 404, already-joined 400), ChangeUserRole (owner promotes, non-owner 403), RemoveMember (owner removes, self 403), leave (sole-owner soft-delete, owner-with-members 403, non-member 404); the **Channel admin suite** (`Features/Channels/ChannelAdminTests.cs`) — PatchUserStatus join-request workflow (accept/deny/ban, non-admin 403, accept-on-Open 400, self 403, RequestSent-status 400), update/patch/delete (owner mutates name/description/join-policy/soft-delete, non-owner 403), GetChannels discovery list (shows non-joined, hides joined, title filter), GetUsersByChannelId (members in Items + owner in Admins, non-member 400); the **Messages slice** (`Features/Messages/MessageTests.cs`) — create (persisted + 200, no-token 403, unknown-channel 404, empty-content 400), list (oldest-first with sender, page-size paging, empty page); the **Message delivery pipeline** (`Features/Messages/MessageDeliveryTests.cs`) — posting a message really flows through RabbitMQ → `ChannelMessageFanoutWorker` → Redis, verified by polling (`WaitUntilAsync`): a recipient's real unread count increments, a disconnected recipient triggers exactly one (faked) push call, and a non-member's count is untouched; the **Users slice** (`Features/Users/UserTests.cs`) — GET /me (401 no-token, 200 with status + joined-channel-count), status (persisted, out-of-enum 400), password (new credential works + old rejected, wrong-current 400, same-as-current 400), profile info (401 without token, persisted, short-name 400), notification settings (seeded defaults, sound-flag persisted), channel mute (mute→list, past-date 400, unmute removes, unmute-when-not-muted 400); the **Devices slice** (`Features/Devices/DeviceTests.cs`) — register (401 no-token, persisted, idempotent on duplicate token), remove (deletes, unknown-token no-op 200); the **Notifications slice** (`Features/Notifications/NotificationTests.cs`) — channel unread count reflects the real tracker (seeded via `IUnreadTracker.IncrementAsync`, not a fake), total sums Accepted channels, zero without activity, reset clears a channel; the **Photos slice** (`Features/Photos/PhotoTests.cs`) — profile photo (`[Authorize]` → 401 without token, upload persists `ProfilePhotoURL` + returns path, invalid content-type 400, re-upload replaces and deletes the previous Cloudinary asset, delete clears URL, delete-when-none 400) and channel thumbnail (owner upload persists `ThumbnailPhotoURL`, non-owner-member 403, unknown-channel 404, invalid content-type 400, `[Authorize]` → 401 without token, owner delete clears URL, delete-when-none 400, non-owner delete 403).

**`/api/messages` carries no `[Authorize]`** (unlike Channels/Users): create still needs identity because the handler resolves the current user (missing token → `AuthorizationException` → **403, not 401**), and it only checks the channel *exists* — not membership — so any authenticated user can post to any channel. Listing is fully open (no auth, no channel check). CreateMessage publishes a `ChannelMessageCreatedEvent` through the real `IMessagePublisher`/RabbitMQ, consumed by the real fan-out worker (`MessageDeliveryTests.cs`). The list handler orders by `CreatedAt` desc then reverses, so a page reads oldest→newest.

**`GET /api/channels` is a discovery list, not "my channels"** — it returns channels the caller has *not* joined (`ChannelUsers.All(u => u.UserId != me)`), ordered by name and paged. Because the assembly shares one DB, that list spans every other test's channels, so a specific channel may land on any page: **isolate with a unique channel name + the `title=` filter** rather than asserting membership on an unfiltered page.

The photo endpoints do their multipart binding + FluentValidation + owner-authorization + persistence for real against Postgres; only the Cloudinary calls themselves are faked (upload returns a canned secure URL, delete is asserted via `Received`), so the assertions cover everything except the actual asset transfer.

Not yet covered: a live SignalR-connected-client test — `MessageDeliveryTests.cs` proves the disconnected/queue/Redis path (`SignalRAndPush`), but nothing connects a real `HubConnection` to exercise the `IsConnected=true` paths (`SignalRSound`/`SignalRSilent`); that needs a `HubConnection` wired against the `WebApplicationFactory` `TestServer`, meaningfully more harness work for a narrower payoff.

## Continuous integration

`.github/workflows/ci.yml` runs on every push and PR targeting `main`, on a GitHub-hosted `ubuntu-latest` runner: restore → `dotnet build --configuration Release` → `codeTalks.Application.UnitTests` → `codeTalks.WebAPI.IntegrationTests`. The job name GitHub reports is `build-and-test`.

- **Docker** is preinstalled on the runner, so `codeTalks.WebAPI.IntegrationTests`' Testcontainers-managed Postgres, RabbitMQ, and Redis containers all work with no extra setup — no `services:` block needed in the workflow, since each gets a dynamically-ported throwaway container per test run rather than relying on a fixed well-known port.

**`main` is protected**: pushes go through a PR, and the `build-and-test` check must pass (with the branch up to date) before the merge button unlocks — enforced for admins too, so there's no direct-push or bypass path. Day-to-day workflow is: branch → PR → wait for the check → merge.

A second job, `push-image`, runs after `build-and-test` succeeds — but only on an actual push to `main` (`if: github.ref == 'refs/heads/main' && github.event_name == 'push'`), never on a PR build. It builds `src/mainPackages/codeTalks.WebAPI/Dockerfile` and pushes to GHCR (`ghcr.io/sahinmaral/codetalks-backend`, tagged `latest` and the commit SHA) using the workflow's own `GITHUB_TOKEN` — no extra secrets. GHCR image names must be lowercase, hence `codetalks-backend` rather than matching the repo's casing.

## Docker

`src/mainPackages/codeTalks.WebAPI/Dockerfile` is a standard multi-stage build (SDK image restores + publishes, `aspnet` runtime image runs the output as the non-root `app` user). The runtime stage also installs `curl` (`apt-get install -y --no-install-recommends curl`, before `USER app` since apt-get needs root) — the base image ships neither `curl` nor `wget`, and `curl` is what `docker-compose.yml`'s `codetalks-webapi` healthcheck uses to call `/health/live` from inside the container, the same way `postgres`/`codetalks-redis`/`codetalks-rabbitmq` each have their own healthcheck. Liveness (not readiness) is deliberate: a transient Redis/RabbitMQ blip shouldn't make Docker consider the whole container unhealthy and cycle it. `docker-compose.yml` provisions Postgres/Redis/RabbitMQ for local dev and wires the `codetalks-webapi` service's env vars (`ConnectionStrings__PostgreSQLConnectionString`, `Redis__ConnectionString`, `RabbitMq__Host`/`Username`/`Password`) to reach them by container name — `docker compose up --build` runs the whole stack. Note `dotnet publish` copies whatever `appsettings.*.json` files exist in the project into the image, including `appsettings.Development.json`'s placeholder JWT key; a real deployment overrides config via environment variables regardless, but excluding non-`Production` appsettings from the publish output would be a cleaner fix.

## Health checks

Two endpoints, both anonymous (no `[Authorize]` — orchestrators/load balancers probing them have no credentials):

- **`GET /health/live`** — liveness. Runs zero checks (`HealthCheckOptions.Predicate = _ => false`); healthy as long as the middleware pipeline itself responds. Never fails just because a dependency blipped — that's what avoids restart-loop thrashing under an orchestrator.
- **`GET /health/ready`** — readiness. Runs the three checks tagged `"ready"` (`PostgresHealthCheck`, `RedisHealthCheck`, `RabbitMqHealthCheck`, in `src/mainPackages/codeTalks.WebAPI/HealthChecks/`), each a thin wrapper around a real connectivity check (`AppDbContext.Database.CanConnectAsync`, `IConnectionMultiplexer.GetDatabase().PingAsync()`, and a throwaway RabbitMQ `ConnectionFactory.CreateConnectionAsync` respectively) — 200 `"Healthy"` when all three succeed, 503 `"Unhealthy"` if any fail.

No unit tests for the three `IHealthCheck` classes — same rationale as thin passthrough queries: real coverage is `SmokeTests.cs`'s two integration tests hitting both endpoints against the harness's real Postgres/Redis/RabbitMQ containers, which is a stronger proof than mocking the dependency away would be.

## CORS

There is no CORS policy — `Program.cs` used to register one with `SetIsOriginAllowed(_ => true).AllowCredentials()` (any origin, with credentials), which is the exact anti-pattern CORS exists to prevent, and it was never actually needed: the only client is the Expo mobile app (`../Mobile`), which talks to the API directly and isn't subject to browser CORS enforcement at all, and Swagger UI is served same-origin. If a browser-based client is ever added (including running the Expo app via `expo start --web`, which _would_ go through a browser), add back a scoped policy naming that origin explicitly — never a wildcard-origin + credentials combination.

## Logging

Serilog is the actual logging provider (`Program.cs`, `builder.Host.UseSerilog(...)`) — application code still just takes `ILogger<T>` via constructor injection (`NotificationDecisionEngine`, `ChannelFanoutService`, `ExpoPushNotificationProvider`, `CachingBehavior`, `CacheRemovingBehavior`, `ExceptionMiddleware`), Serilog is purely what's underneath that abstraction. Console output is human-readable text in Development and compact JSON (`CompactJsonFormatter`) otherwise — JSON-to-stdout is the right target for a containerized app; there's no file sink, since files inside an ephemeral container are lost the same way the DataProtection-keys warning already flags. `app.UseSerilogRequestLogging()` gives one structured log line per HTTP request (method/path/status/elapsed) for free. Minimum log levels live under a `Serilog:MinimumLevel` config section (`appsettings.json`/`.Development.json`), not the standard `Logging:LogLevel` — Serilog reading its own config section is what `ReadFrom.Configuration` expects, and the standard section stops being consulted once Serilog owns the provider.

`Core.CrossCuttingConcerns/Logging/*` (a custom `LoggerServiceBase`/`FileLogger` Serilog wrapper) and `Core.Application/Pipelines/Logging/*` (`LoggingBehavior`, `ILoggableRequest`) were deleted — confirmed zero references anywhere before removal: `LoggingBehavior` was never registered as an `IPipelineBehavior` (only `RequestValidationBehavior` is, in `ApplicationServiceRegistration`), so this whole apparatus, including the two old `Serilog`/`Serilog.Sinks.File` package references it pulled in, was dead code left over from whatever template the project started from.

**Real fix along the way:** `ExceptionMiddleware.CreateInternalException` (the 500/unexpected-exception path) previously caught and fully absorbed every exception without logging it anywhere — since the middleware never rethrows, nothing else in the pipeline ever saw it either, so a genuine production bug left zero trace in any log. It now logs via `ILogger<ExceptionMiddleware>.LogError(exception, ...)` before returning the response. The other exception branches (validation/business/not-found/authorization) are expected 4xx outcomes already visible via the per-request status code from `UseSerilogRequestLogging()`, so they're deliberately not logged separately.

Not yet done (flagged as a follow-up): log aggregation/shipping (Seq, Loki, CloudWatch, etc.) — depends on the eventual hosting target.

## Error tracking (Sentry)

`Sentry.AspNetCore` (`Program.cs`, `builder.WebHost.UseSentry(...)`) reports genuine unexpected exceptions to Sentry. The DSN comes from configuration (`Sentry:Dsn`) — set via .NET User Secrets locally (this project already has a `<UserSecretsId>`), and via a `Sentry__Dsn` environment variable in real deployments; it's never hardcoded or committed. `options.Dsn` is explicitly set to `builder.Configuration["Sentry:Dsn"] ?? string.Empty` — Sentry's SDK requires an *explicit* empty string to disable itself and throws at startup if the config key is merely absent, so the fallback is what keeps CI and a fresh clone (no Sentry account configured) from crashing on boot.

Only **Error Monitoring** is enabled — `TracesSampleRate = 0.0` deliberately disables Sentry's Performance/Tracing product, and its separate Logging product isn't used at all (would duplicate what Serilog already does and burn a separate quota for no benefit).

`ExceptionMiddleware` catches and fully absorbs every exception without rethrowing (see above), so Sentry's own automatic exception-capturing middleware would never see anything on its own. `CreateInternalException` — the same 500/unexpected-exception path that logs via `ILogger` — also explicitly calls `SentrySdk.CaptureException(exception)`. The other exception branches (validation/business/not-found/authorization) are expected 4xx outcomes and are deliberately not reported — sending those to Sentry would burn through the free tier's 5,000-events/month quota on routine client errors (wrong passwords, validation failures, etc.), not genuine bugs.