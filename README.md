# Tree Editor

## Run with Docker (one command)

Requires only Docker:

```bash
docker compose up -d --build
```

Open http://localhost:8080. Compose starts PostgreSQL. It then builds the `api` image from `src/TreeEditor.Api/Dockerfile`, which runs three stages. First, Node builds the React UI into `wwwroot`. Next, the .NET SDK publishes the API with that UI included. Finally, the image runs on the ASP.NET runtime. The API waits until the PostgreSQL healthcheck passes, then applies migrations and sample data. Stop with `docker compose down`; add `-v` to also delete the database volume. The container runs in the Production environment, so Swagger UI is not enabled.

## Run locally (development)

Requires Docker, .NET 10 SDK and Node.js.

1. Start PostgreSQL only: `docker compose up -d postgres`
2. From `src/TreeEditor.Api/ClientApp`, run `npm ci && npm run dev`.
3. From the project root, run `dotnet run --project src/TreeEditor.Api --launch-profile http`.

Open the UI at http://localhost:5173. The Vite development server proxies API requests to http://localhost:5227. Swagger UI is available at http://localhost:5227/swagger in Development.

For a single-server setup, run `npm ci && npm run build` in `src/TreeEditor.Api/ClientApp`, then start the API as above and open http://localhost:5227. The API serves the built UI from `wwwroot`; it does not build the UI on startup.

## Tests

Run `dotnet test TreeEditor.sln`. Integration tests require a running Docker daemon, but **do not require `docker compose up`**: Testcontainers starts a separate disposable PostgreSQL container, applies migrations and sample data through the API startup, and removes the container after the tests. The tests never use the development database. Frontend unit tests run with `npm test` in `src/TreeEditor.Api/ClientApp`.

## Data model

PostgreSQL stores nodes in `tree_nodes` with a UUID primary key, nullable `parent_id` self-reference (indexed, cascading deletes), a value and an `xmin` concurrency token. Sample data has five levels. The database view fetches only requested levels; loading a node into the local cache fetches its ancestor IDs without loading its ancestors as editable nodes. Edits, additions and deletions remain in the browser until Apply, which persists them in a transaction and returns the new row versions of added and modified nodes. The cache is kept after Apply: saved nodes become unchanged with their new versions, and applied deletions stay visible as `removed` (read-only, never sent again). Apply validates values (non-empty, at most 500 characters) and returns 400 for invalid input and 409 for stale versions or conflicting concurrent changes. Reset restores the initial sample tree.

API responses include an `X-Trace-Id` header; error responses also include `traceId` in their Problem Details body. Serilog writes readable console logs with timestamps, levels and the `RequestTraceId` property for correlating requests, errors and tree operations. Logging levels are configured in `src/TreeEditor.Api/appsettings.json`; routine EF Core SQL commands are suppressed, while warnings and errors remain visible. In Development, executed SQL commands are logged via the `Microsoft.EntityFrameworkCore.Database.Command` override in `appsettings.Development.json` (parameter values are hidden). Logs contain method, path, status, duration and counts of applied changes (without logging node values).
