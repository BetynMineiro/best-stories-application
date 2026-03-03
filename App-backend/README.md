# Hacker News Best Stories API

RESTful API that returns the first **n** best stories from the [Hacker News API](https://github.com/HackerNews/API), sorted by score descending.

## How to run

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Run the API

```bash
cd App-backend
dotnet run --project App.Api
```

The API listens on `https://localhost:5xxx` (or `http://localhost:5xxx`). The exact URL is shown in the console.

### Run with Aspire (orchestration + observability)

```bash
dotnet run --project App.AppHost
```

Starts the API and the Aspire dashboard when configured.

### Run tests

```bash
dotnet test TestProject/TestProject.csproj
```

### Docker

From `App-backend`:

- **Run API:** `docker compose up -d app-api` (API at `http://localhost:8080`)
- **Run tests:** `docker compose run test`
- **Build images:** `docker compose build`

### Example request

```bash
# First 10 best stories (PagedRequest: pageSize + cursor)
curl "http://localhost:5000/v1/beststories?pageSize=10"

# With cursor (cursor-based pagination)
curl "http://localhost:5000/v1/beststories?pageSize=10&cursor=21233041"
```

### Response format

All responses use the same contract `Result<T>`: `success`, `statusCode`, `messages` (on error), `data` (on success). The list is in `CursorPage<T>` inside `data`.

Success (200):

```json
{
  "success": true,
  "statusCode": 200,
  "messages": null,
  "data": {
    "items": [
      {
        "title": "A uBlock Origin update was rejected from the Chrome Web Store",
        "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
        "postedBy": "ismaildonmez",
        "time": "2019-10-12T13:43:01Z",
        "score": 1716,
        "commentCount": 572
      }
    ],
    "nextCursor": "21233041",
    "hasNext": true
  }
}
```

Error (e.g. 400 or 503):

```json
{
  "success": false,
  "statusCode": 503,
  "messages": ["External API is temporarily unavailable."],
  "data": null
}
```

---

## Assumptions

- **pageSize** (query): item limit 1–500; **cursor** optional for cursor-based pagination (last id from previous response).
- List endpoint uses `PagedRequest` (PageSize + Cursor); response is `Result<CursorPage<BestStory>>`.
- Stories without a `title` (e.g. deleted or invalid) are filtered out via `DisplayableBestStorySpecification`.
- Time is ISO 8601 UTC (`yyyy-MM-ddTHH:mm:ssZ`).
- No database; data comes from Hacker News API with in-memory caching.

---

## Avoiding overload on Hacker News

- **Caching:** Best story IDs and story details cached in memory (TTL in `appsettings.json`).
- **HttpClientFactory:** Typed client for Hacker News API.
- **Polly:** Retry with exponential backoff and timeout.
- **Parallel fetch:** Story details for the requested window via `Task.WhenAll`.
- **Cap:** Max page size 500.

---

## Configuration

`App.Api/appsettings.json`:

```json
{
  "App": {
    "HackerNews": {
      "BaseAddress": "https://hacker-news.firebaseio.com/v0/",
      "HttpClientTimeoutSeconds": 10
    },
    "Cache": {
      "BestStoryIdsTtlSeconds": 300,
      "StoryDetailTtlSeconds": 180
    }
  }
}
```

---

## Architecture

- **Hexagonal (Ports & Adapters):**
  - **Ports** (Domain): `IBestStoryService`, `ICacheService`, `IValidator<PagedRequest>`, `ISpecification<BestStory>`; REST in Api.
  - **Application:** `GetBestStoriesUseCase` (orchestrates IDs, cache, details, sort).
  - **Adapters:** `HackerNewsBestStoryAdapter` (HTTP), `MemoryCacheAdapter` (IMemoryCache).
- No database; data from Hacker News API with in-memory cache.
- .NET 10, ASP.NET Core, optional .NET Aspire for observability.

---

## Design patterns

- **Factory (cache-aside):** `ICacheService.GetOrCreateAsync(key, factory, ttl)` takes a `Func<Task<T?>>` factory. On cache miss the factory is invoked; the adapter stays agnostic of the value source. Keeps caching separate from data-fetching and simplifies use case tests with mocks.
- **CursorPage:** Value object for cursor-based pagination (`Items`, `NextCursor`, `HasNext`). Client sends the last item id as `cursor` for the next page. Avoids offset-based pagination and keeps the API stable when the source list changes.
- **Specification:** `ISpecification<T>` and `DisplayableBestStorySpecification` encode the rule “story is displayable iff non-empty title”. Used in `HackerNewsBestStoryAdapter` to filter deleted/invalid items. Rule lives in the domain, is reusable and testable; adapters depend on the abstraction.
- **In-memory cache:** `IMemoryCache` via `MemoryCacheAdapter`. Single-instance only; for scale-out, a distributed cache (e.g. Redis) is the next step (see Enhancements).

---

## Observability (OpenTelemetry & distributed tracing)

The API is instrumented with **OpenTelemetry** for traces, metrics and log correlation:

- **Distributed tracing:** ASP.NET Core and HttpClient are instrumented. Each request gets a trace context; outbound calls to Hacker News become child spans, so the full path (API → use case → cache → HTTP client) appears in one trace. Export via **OTLP gRPC** to a collector (Jaeger, Grafana Tempo, or Aspire dashboard when using `App.AppHost`).
- **Metrics:** AspNetCore (duration, status codes), Runtime (GC, thread pool), HttpClient (calls, duration), exported via OTLP gRPC.
- **Logs:** Serilog with **OpenTelemetry sink** (OTLP gRPC); logs go to the same pipeline and can be correlated with trace IDs in the observability backend.

Configuration: `App.CrossCutting` (`ConfigureMetrics`, `ConfigureLogging`). With **.NET Aspire** (`dotnet run --project App.AppHost`), the dashboard can ingest this telemetry if the collector is configured. For a custom setup, set the OTLP exporter endpoint (e.g. `OTEL_EXPORTER_OTLP_ENDPOINT` or app settings).

---

## Enhancements / future work

- **Rate limiting:** Per-client or global limit to protect the API and Hacker News. Options: middleware (e.g. `AspNetCoreRateLimit`) or API Gateway; sliding or fixed window; return `429 Too Many Requests` and `Retry-After` where appropriate.
- **Distributed cache:** Use **Redis** (or similar) when running multiple instances so replicas share cache. Add an `ICacheService` adapter using `IDistributedCache` (e.g. StackExchange.Redis) and register in DI; use case and TTL config can stay unchanged.
- **Health check:** Endpoint that probes Hacker News API availability (e.g. `GET /health/ready`); today `/health` only indicates the process is up.

**Already in place:**

- **Structured response:** Single contract `Result<T>` with `success`, `statusCode`, `messages`, `data`; used by the controller and `ErrorHandlerMiddleware`.
- **OpenAPI:** Cursor pagination and response contract documented in Swagger (doc + operation filter for GET BestStories).
- **Docker:** `Dockerfile` (App.Api) and `docker-compose.yml`; services `app-api` (run API) and `test` (run tests). From `App-backend`: `docker compose up -d app-api`, `docker compose run test`.
