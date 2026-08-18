## Purpose
- Chat microservice for Team Hub (org-scoped conversations and messages).

## Source of truth
- `team-hub-chat/` (`Program.cs`, `Configuration/`, `Controllers/`, `Services/`, `Dtos/`, `appsettings*.json`)
- `team-hub-chat/Data/ChatDbContext.cs` (EF Core models + mappings)
- `team-hub-chat/Migrations/` (schema)
- `team-hub-chat/.env.example` (`ConnectionStrings:DefaultConnection`, `Jwt:Key`, `BlobStorage:*` dev)
- `building-blocks/TeamHub.Observability/`
- `building-blocks/TeamHub.BlobStorage/`
- `Grpc/` (internal gRPC proxy for organization membership lists)
- `docs/chat.mb`

## Do
- Keep Controllers under `Controllers/` with XML `<summary>` on actions.
- Use Serilog via `AddTeamHubSerilog()`.
- Observability: `AddTeamHubOpenTelemetry` (traces OTLP + `/metrics`); shared Exception/CorrelationId/SessionId/UserIdLogging + `UseSerilogRequestLoggingExcludingHealth`.
- Register `AddTeamHubProblemDetails()` + `AddTeamHubExceptionMapper<ChatExceptionMapper>()`.
- Production Serilog: compact JSON + OTLP sink to `mon-otel:4317`. Dev also ships OTLP to `localhost:4317`.
- Swagger: Development only; versioned docs via `IApiVersionDescriptionProvider`.
- API versioning: URL path `/api/chat/v1/*` (contract `1.0`; major only in URL). Source of truth: `Configuration/ChatApiVersions.cs` + `Controllers/ChatApiController` base.
- Config layering:
  - `appsettings.json` — shared defaults (Jwt Issuer/Audience, Serilog, Observability). No localhost Kestrel/Grpc.
  - `appsettings.Development.json` / `appsettings.Staging.json` — localhost Kestrel (`5005`), `Grpc:Organization` localhost.
  - `appsettings.Production.json` — Kestrel `+:8080`, `Grpc:Organization` `srv-organization:8081`, compact Serilog + OTLP logs.
  - `GrpcOptions.Organization` is `[Required]` + `ValidateOnStart` (no class-level localhost default).
- Dev Env: HTTP only on port `5005` (`launchSettings.json` / `appsettings.Development.json`).
- Prod Env (Docker): container listens on `8080`; image build context is monorepo root (`Dockerfile`).
- Endpoints (JWT; org member via gRPC `ListMembers`):
  - `GET /health` — PostgreSQL health check (`200` healthy, `503` unhealthy)
  - `GET /api/chat/v1/health` — REST health (versioned, `ChatHealthController`, anonymous)
  - `GET /metrics` — Prometheus metrics
  - Conversations/members/messages/attachments/reactions/reads/pin/settings under `/api/chat/v1/organizations/{orgId}/conversations` — see `docs/chat.mb`
- Internal gRPC:
  - Exposes `OrganizationMemberService.ListMembers` (same contract as `team-hub-organization`) proxied to organization service; also used for chat authZ.
- Gateway route: `/api/chat/{**catch-all}` → `chat-cluster`.
- Docker compose: `srv-chat` on host `5005`, depends on `db-postgres` + `blob-storage` + `mon-otel`.
- Database: PostgreSQL schema managed via EF Core migrations in `Migrations/` (auto-applied on startup via `ApplyStartupSchemaAsync`, all envs except Testing).
- Tables: `conversations`, `conversation_members`, `messages`, `message_attachments`, `message_reactions`, `message_mentions`, `message_reads`, `conversation_pins`, `conversation_settings`.
- Connection string:
  - Aspire: `Database=chat_db` (injected by AppHost)
  - staging Compose: `Database=chatdb` on `db-postgres` (from root `.env.staging` via Compose `environment`)
- Blob: Production requires `BlobStorage:ConnectionString`; attachments return `503` when unset in non-Production.
- Config: `ConnectionStrings:DefaultConnection`, `Jwt:*`, `Grpc:Organization`, `BlobStorage:*`; DotNetEnv `Env.NoClobber().TraversePath().Load()` when not Production.
- DI: Database → Health → Jwt → Grpc → Blob → ApplicationServices → ApiInfrastructure.
- Tests: `team-hub-chat.Tests` (in-memory DB, fake org gRPC, fake blob).
- Keep this file updated after API, port, schema, or observability changes.

## Don't
- Do not add domain routes without updating gateway, nginx (if needed), and docs.
- Do not bypass gateway for frontend API calls.
- Do not invent ports already used by auth (`5001`/`5101`), organization (`5002`/`5102`), bff (`5003`), notification (`5004`), or gateway (`5000`).
- Do not add additional domain routes/feature endpoints without updating gateway/nginx/docs.
