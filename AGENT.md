## Purpose
- Chat microservice scaffold for Team Hub (no business logic yet).

## Source of truth
- `team-hub-chat/` (`Program.cs`, `Configuration/`, `Controllers/ChatApiController.cs`, `appsettings*.json`)
- `team-hub-chat/.env.example`
- `building-blocks/TeamHub.Observability/`
- `docs/chat.mb`

## Do
- Keep Controllers under `Controllers/` with XML `<summary>` on actions.
- Use Serilog via `AddTeamHubSerilog()`.
- Observability: `AddTeamHubOpenTelemetry` (traces OTLP + `/metrics`); shared Exception/CorrelationId/SessionId/UserIdLogging + `UseSerilogRequestLoggingExcludingHealth`.
- Register `AddTeamHubProblemDetails()` for ModelState `ValidationProblemDetails`.
- Production Serilog: compact JSON + OTLP sink to `mon-otel:4317`. Dev also ships OTLP to `localhost:4317`.
- Swagger: Development only; versioned docs via `IApiVersionDescriptionProvider`.
- API versioning: URL path `/api/chat/v1/*` (contract `1.0`; major only in URL). Source of truth: `Configuration/ChatApiVersions.cs` + `Controllers/ChatApiController` base.
- Dev Env: HTTP only on port `5005` (`launchSettings.json` / `appsettings.Development.json`).
- Prod Env (Docker): container listens on `8080`; image build context is monorepo root (`Dockerfile`).
- Endpoints (scaffold):
  - `GET /health` — process health
  - `GET /metrics` — Prometheus metrics
- Gateway route: `/api/chat/{**catch-all}` → `chat-cluster`.
- Docker compose: `srv-chat` on host `5005`, depends on `mon-otel`.
- Config: `Jwt:*`; DotNetEnv `Env.NoClobber().TraversePath().Load()` when not Production.
- Keep this file updated after API, port, or observability changes.

## Don't
- Do not add domain routes without updating gateway, nginx (if needed), and docs.
- Do not bypass gateway for frontend API calls.
- Do not invent ports already used by auth (`5001`/`5101`), organization (`5002`/`5102`), bff (`5003`), notification (`5004`), or gateway (`5000`).
- Do not add EF/Kafka/gRPC/seed until a feature requires them.
