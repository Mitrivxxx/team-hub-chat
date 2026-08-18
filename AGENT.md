## Purpose
- Chat microservice for Team Hub (PostgreSQL schema MVP; no business endpoints yet).

## Source of truth
- `team-hub-chat/` (`Program.cs`, `Configuration/`, `Controllers/ChatApiController.cs`, `appsettings*.json`)
- `team-hub-chat/Data/ChatDbContext.cs` (EF Core models + mappings)
- `team-hub-chat/Migrations/` (schema)
- `team-hub-chat/.env.example`
- `building-blocks/TeamHub.Observability/`
- `Grpc/` (internal gRPC proxy for organization membership lists)
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
  - `GET /health` — PostgreSQL health check (`200` healthy, `503` unhealthy)
  - `GET /api/chat/v1/health` — REST health (versioned, `ChatHealthController`, anonymous)
  - `GET /metrics` — Prometheus metrics
- Internal gRPC:
  - Exposes `OrganizationMemberService.ListMembers` (same contract as `team-hub-organization`) proxied to organization service, for listing users in an organization.
- Gateway route: `/api/chat/{**catch-all}` → `chat-cluster`.
- Docker compose: `srv-chat` on host `5005`, depends on `db-postgres` + `mon-otel`.
- Database: PostgreSQL schema managed via EF Core migrations in `Migrations/` (auto-applied on startup via `ApplyStartupSchemaAsync`, all envs except Testing).
- Tables: `conversations`, `conversation_members`, `messages`, `message_attachments`, `message_reactions`, `message_mentions`, `message_reads`, `conversation_pins`, `conversation_settings`.
- Connection string:
  - Aspire: `Database=chat_db` (injected by AppHost)
  - staging Compose: `Database=chatdb` on `db-postgres` (from root `.env.staging` via Compose `environment`)
- Config: `ConnectionStrings:DefaultConnection`, `Jwt:*`; DotNetEnv `Env.NoClobber().TraversePath().Load()` when not Production.
- Keep this file updated after API, port, schema, or observability changes.

## Don't
- Do not add domain routes without updating gateway, nginx (if needed), and docs.
- Do not bypass gateway for frontend API calls.
- Do not invent ports already used by auth (`5001`/`5101`), organization (`5002`/`5102`), bff (`5003`), notification (`5004`), or gateway (`5000`).
- Do not add additional domain routes/feature endpoints without updating gateway/nginx/docs.
