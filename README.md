# Best Story API

REST API that exposes [Hacker News](https://news.ycombinator.com/) best stories, ordered by score (descending), with cursor-based pagination.

## Tech stack

- .NET 10
- ASP.NET Core (Web API)
- .NET Aspire (orchestration)
- xUnit, Moq (tests)
- Swagger/OpenAPI
- Serilog, OpenTelemetry (logging, tracing, metrics)

## Backend structure (`App-backend`)

| Project | Role |
|--------|------|
| **App.Api** | HTTP API, controllers, middleware |
| **App.Application** | Use cases and input validators (e.g. GetBestStories, PagedRequestValidator) |
| **App.Domain** | Entities, contracts (services), specifications |
| **App.CrossCutting** | DTOs, validation, notifications, config, OpenTelemetry |
| **App.Adapters** | Integrations (Hacker News HTTP, in-memory cache) |
| **App.AppHost** | Aspire host (orchestrates API + observability) |
| **TestProject** | Unit tests (folder structure mirrors solution) |

### Test layout

- `TestProject/Application/GetBestStories/` — use case and PagedRequest validator tests
- `TestProject/Domain/Specifications/` — DisplayableBestStory specification tests
- `TestProject/CrossCutting/ResultObjects/` — Result and CursorPage tests

## API

- **GET** `/v1/BestStories?pageSize=10&cursor=optional`
  - `pageSize`: 1–500 (default 10)
  - `cursor`: last story id from previous page (pagination)
  - Response: single contract `Result<T>` with `success`, `statusCode`, `messages` (on error), `data` (on success). On success, `data` is `CursorPage<BestStory>` (`items`, `nextCursor`, `hasNext`).

## Run locally

**Prerequisites:** .NET 10 SDK

```bash
cd App-backend
dotnet build App.Api/App.Api.csproj
dotnet run --project App.AppHost
```

- API: URL shown in console (e.g. `http://localhost:5xxx`)
- Swagger: `http://localhost:5xxx/swagger`

## Tests

```bash
cd App-backend
dotnet test TestProject/TestProject.csproj
```

xUnit + Moq; external dependencies are mocked in use case tests.

## Docker

From `App-backend`:

- **Run API:** `docker compose up -d app-api` (API at `http://localhost:8080`)
- **Run tests:** `docker compose run test`
- **Build images:** `docker compose build`

## Patterns and observability

See **App-backend/README.md** for:

- **Patterns:** Hexagonal (ports & adapters), cache factory (`GetOrCreateAsync` + delegate), cursor pagination (`CursorPage<T>`), Specification (displayable rule in domain), in-memory cache.
- **Observability:** OpenTelemetry with distributed tracing (ASP.NET Core + HttpClient), metrics (AspNetCore, Runtime, HttpClient), logs (Serilog → OTLP); export via OTLP gRPC to a collector (Jaeger, Tempo, or Aspire dashboard).
- **Future work:** Rate limiting (per-client or global), distributed cache (Redis + `ICacheService`), health check that probes Hacker News API availability.

## CI

`.github/workflows/ci.yml`: restore, build, and test the backend; on push to `main`/`master`, build and push the API Docker image.
