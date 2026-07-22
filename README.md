# codeTalks.Backend

[![CI](https://github.com/sahinmaral/codeTalks.Backend/actions/workflows/ci.yml/badge.svg)](https://github.com/sahinmaral/codeTalks.Backend/actions/workflows/ci.yml)

🇹🇷 [Bu belgeyi Türkçe okuyun](README.tr.md)

**codeTalks** is the backend for a real-time chat application built for developers. Instead of relying on a ready-made service like Firebase, it's a custom-written .NET API with real-time messaging built in via SignalR, backed by PostgreSQL, Redis, and RabbitMQ.

This repository doubles as a personal exercise in shipping a side project to a genuinely production-ready standard — not just "it runs," but tested, containerized, monitored, and gated by CI before anything reaches `main`.

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 / ASP.NET Core Web API |
| Architecture | Clean Architecture with a custom CQRS layer (`ICommand`/`IQuery` + `IRequestHandler`, dispatched via a custom `Dispatcher`) — MediatR was dropped after its license change |
| Validation | FluentValidation, as a cross-cutting dispatcher pipeline behavior |
| Mapping | Mapster |
| Auth | ASP.NET Identity + JWT (access + refresh token rotation) |
| Database | PostgreSQL, via EF Core (Npgsql) |
| Cache / presence | Redis — connection tracking, unread-count tracking, settings cache |
| Messaging | RabbitMQ — async fan-out of channel messages to recipients |
| Real-time | SignalR — chat hub and notification hub |
| Media | Cloudinary — profile photos, channel thumbnails |
| Push notifications | Expo push API |
| Logging | Serilog — structured JSON in non-Development environments, human-readable console locally |
| Error tracking | Sentry — captures genuine unhandled exceptions |
| Localization | English + Turkish, via `Microsoft.Extensions.Localization` |

## Architecture

The solution is split into `corePackages` (framework-agnostic building blocks — CQRS, repository pattern, security, pagination, cross-cutting concerns) and `mainPackages` (the actual domain and application layers: `Domain`, `Application`, `Infrastructure`, `Persistence`, `Presentation`, `WebAPI`), following Clean Architecture dependency rules throughout.

## Features

- **Auth** — register, login, JWT refresh with rotation
- **Channels** — create/join (open or request-to-join policies)/leave, per-channel roles (Owner/Moderator/User), admin actions (accept/deny/ban join requests), discovery list, thumbnail photos
- **Messages** — post and page through channel messages; delivery fans out in real time via RabbitMQ → SignalR
- **Users** — profile info & photo, online status, password changes, notification & channel-mute settings
- **Devices** — push-notification device token registration
- **Notifications** — per-channel and total unread counts, reset on read

## Running locally

The fastest path — everything in containers, including the API itself:

```bash
docker compose up --build
```

This starts Postgres, Redis, RabbitMQ, and the API (mapped to `http://localhost:5050`), each with its own healthcheck.

Alternatively, run the API from source against containerized infra only:

```bash
docker compose up postgres codetalks-redis codetalks-rabbitmq
dotnet run --project src/mainPackages/codeTalks.WebAPI
```

Swagger UI is available at `/swagger` in the Development environment.

## Testing

```bash
dotnet test tests/codeTalks.Application.UnitTests   # 229 tests — handler/validator unit tests, NSubstitute mocks
dotnet test tests/Core.Application.UnitTests        # 9 tests
dotnet test tests/Core.Security.UnitTests           # 5 tests
dotnet test tests/codeTalks.WebAPI.IntegrationTests # 96 tests — full HTTP pipeline against real
                                                     # Postgres/RabbitMQ/Redis (Testcontainers);
                                                     # requires a running Docker daemon
```

The integration suite boots the real ASP.NET Core host — routing, auth, validation, EF Core — against throwaway containers rather than mocks, so it's exercising the actual production wiring, not a simulation of it.

## CI/CD

Every push and pull request against `main` runs the full suite (build → unit tests → integration tests) via GitHub Actions. `main` is branch-protected: changes go through a PR, and the check must pass before merging — enforced for admins too.

On every merge to `main`, a second job builds the Docker image and publishes it to GitHub Container Registry (`ghcr.io/sahinmaral/codetalks-backend`, tagged `latest` and the commit SHA).

## Observability

- **Health checks** — `GET /health/live` (liveness, always healthy if the process responds) and `GET /health/ready` (readiness — verifies Postgres, Redis, and RabbitMQ are actually reachable). Both are used by `docker-compose.yml`'s healthchecks.
- **Structured logging** — Serilog, with one line per HTTP request logged automatically.
- **Error tracking** — genuine unhandled exceptions are reported to Sentry.

## Related repository

Mobile app source: [codeTalks](https://github.com/sahinmaral/codeTalks)
