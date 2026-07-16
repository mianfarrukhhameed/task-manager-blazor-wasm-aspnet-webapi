# 📝 Task Manager (Full-Stack Reactive Architecture)

A robust Task Management system showcasing a **Clean Architecture** approach. This project integrates a **Reactive Blazor WebAssembly** frontend with a **CQRS-driven ASP.NET Core Web API**.

## 🏗️ Architecture Overview

This project is built on the principle of **Separation of Concerns**, ensuring each layer is independent and testable.

### 1. Frontend: Reactive Blazor WASM
* **State Management:** Powered by **Rx.NET (Reactive Extensions)**. It uses a `BehaviorSubject` as a Single Source of Truth, allowing the UI to react instantly to data changes.
* **Decoupled Components:** Components are "dumb" and reusable. They handle local validation via **FluentValidation** and communicate via `EventCallback`.
* **Observer Pattern:** Pages subscribe to data streams and automatically re-render via `IObservable` updates, minimizing manual UI refreshes.

### 2. Backend: CQRS & MediatR
* **Command/Query Segregation:** Uses the **CQRS pattern** to separate read and write operations, handled seamlessly by **MediatR**.
* **Thin Controllers:** API controllers contain zero business logic; they simply delegate requests to MediatR handlers.
* **Mapping:** **AutoMapper** is used to transform incoming Commands into Domain Entities and outgoing Entities into DTOs.

### 3. Data Layer: Repository Pattern
* **Abstraction:** The `TodoTaskRepository` abstracts **Entity Framework Core**, providing a clean interface for data persistence.
* **Concurrency:** Supports `CancellationToken` throughout the pipeline for optimized resource management.

---

## 🔄 The Full-Stack Data Flow

```mermaid
sequenceDiagram
    participant User
    participant Blazor as Blazor Component
    participant State as TodoStateService
    participant API as TodosController
    participant MediatR as CommandHandler
    participant DB as PostgreSQL (EF Core)

    User->>Blazor: Click "Submit"
    Blazor->>Blazor: Local Validation (Fluent)
    Blazor->>State: CreateTodo(command)
    State->>API: HTTP POST /api/todos
    API->>MediatR: _mediator.Send(command)
    MediatR->>MediatR: AutoMapper (Cmd -> Entity)
    MediatR->>DB: Repository.Create(entity)
    DB-->>MediatR: Save Changes
    MediatR-->>API: Return TodoTaskDto
    API-->>State: 201 Created
    State->>State: Update BehaviorSubject
    State-->>Blazor: Observable Push
    Note over Blazor: UI Updates Automatically

---

## Configuration

Secrets and environment-specific values are **not** committed to source control. Set them via environment variables, `dotnet user-secrets` (local development), Azure App Configuration, or Azure Key Vault (production).

### Web API environment variables

| Variable | Required | Description |
|---|---|---|
| `ConnectionStrings__MainDb` | Yes | PostgreSQL connection string for the main database |
| `Ai__GoogleAI__ApiKey` | When `Ai:Provider` is `google` | Google AI (Gemini) API key |
| `Ai__Claude__ApiKey` | When `Ai:Provider` is `claude` | Anthropic API key |
| `Ai__OpenAI__ApiKey` | When `Ai:Provider` is `openai` | OpenAI API key |
| `AppConfigConnectionString` | Optional | Azure App Configuration connection string |
| `AppConfigEnvironmentName` | Optional | Azure App Configuration label filter (e.g. `dev`, `prod`) |
| `KeyVault__Uri` | Production | Azure Key Vault URI; loaded when `ASPNETCORE_ENVIRONMENT` is `Production` |
| `AllowedHosts` | Production | Semicolon-separated hostnames allowed by the API (see `appsettings.Production.json`) |

### Server-side access token

The API stores the incoming bearer token via JWT `SaveToken` (not as a user claim). Inject `IAccessTokenProvider` when a handler or service needs the current user's access token (e.g. future Auth0 Management API calls):

```csharp
var token = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken);
```

`RequireHttpsMetadata` is enabled outside Development so JWT metadata is fetched over HTTPS only.
### Local development setup

1. Start PostgreSQL (Postgres 16 via Docker Compose; host port **5433** to avoid clashing with a local Postgres on 5432):

```bash
docker compose up -d
```

2. Copy required values into `src/WebApi/Properties/launchSettings.json` under `environmentVariables`, **or** use user secrets:

```bash
cd src/WebApi
dotnet user-secrets set "ConnectionStrings:MainDb" "Host=localhost;Port=5433;Database=taskdb;Username=taskuser;Password=taskpass"
```

3. Export AI keys in your shell or IDE run configuration (`launchSettings.json` environment variables):

```bash
export Ai__GoogleAI__ApiKey="your-google-ai-key"
export Ai__Claude__ApiKey="your-anthropic-key"
```

4. Never commit API keys, database passwords, or other secrets to git. `launchSettings.json` is gitignored; use placeholders only in tracked files.

### Local ONNX embeddings (BGE-small)

Todo embeddings default to an in-process **ONNX Runtime** model (`bge-small-en-v1.5`, 384-d) when `Ai:Embedding:Provider` is `Onnx`. Model weights are not committed; download them once:

```bash
chmod +x scripts/download-bge-onnx.sh
./scripts/download-bge-onnx.sh
```

Then set `Ai:Features:EnableEmbeddings` to `true`. Alternate providers remain available via `Ai:Embedding:Provider` (`OpenAI` or `Ollama`). If you switch embedding providers or models, clear or recreate stored rows in `TodoEmbeddings` so vectors stay comparable.

### Sprint planning agent (Microsoft Agent Framework)

With `Ai:Features:EnableAgents` set to `true`, **AI Chat → Optimize Sprint** runs a Microsoft Agent Framework **Analyst → Planner** sequential workflow (`Ai:Agents:WorkflowMode` = `Multi`, default): Analyst uses search/stats/due-soon tools; Planner proposes and creates the sprint. Set `WorkflowMode` to `Single` for the previous one-agent loop. Steps in the UI show `AgentName/tool`. Prefer `Ai:Agents:ChatModel` = `gemini-2.5-flash` (or OpenAI) — Gemini 3.x often returns HTTP 400 on OpenAI-compat tool follow-ups. Heuristic selection still runs if the agent fails.

### Logging

LLM prompts and task content are logged at **Debug** level only. At **Information** level, the AI pipeline logs metadata (provider, model, content lengths) without user data. Enable Debug logging locally when troubleshooting summarization:

```json
"Logging": {
  "LogLevel": {
    "Fistix.TaskManager.AiLayer": "Debug"
  }
}
```

### Database migrations

Schema changes are managed with EF Core migrations in `src/DataLayer/Migrations/`.

Apply pending migrations locally:

```bash
cd src
dotnet ef database update --project DataLayer/DataLayer.csproj --startup-project WebApi/WebApi.csproj
```

In Development, migrations are applied automatically on API startup.

Add a new migration after model changes:

```bash
cd src
dotnet ef migrations add <MigrationName> --project DataLayer/DataLayer.csproj --startup-project WebApi/WebApi.csproj
```

### AI feature flags

Disable AI summarization without removing the endpoint:

```json
"Ai": {
  "Features": {
    "EnableSummarization": false
  }
}
```

When disabled, `POST /api/ai/summarize` returns **503 Service Unavailable**.

### MCP server (Claude Desktop)

A standalone MCP console app in `src/McpServer` exposes todos as MCP resources/tools over stdio and calls WebApi with a bearer token. See **[docs/mcp/README.md](docs/mcp/README.md)** for setup, Claude Desktop config, and environment variables (`API_URL`, `API_ACCESS_TOKEN`). `Ai:Features:EnableMcp` documents the feature conceptually; the MCP process is separate from WebApi.

### AI rate limiting

`POST /api/ai/summarize` is rate-limited per authenticated user (`sub` claim), falling back to client IP when unauthenticated.

```json
"Ai": {
  "Features": {
    "SummarizeRateLimit": {
      "Enabled": true,
      "PermitLimit": 10,
      "WindowMinutes": 1
    }
  }
}
```

When the limit is exceeded, the API returns **429 Too Many Requests** with a `Retry-After` header.

### Input validation limits

| Field | Max length |
|---|---|
| Title | 200 |
| Description | 4,000 |
| AI summary (output) | 500 (truncated with warning if exceeded) |

### Production hardening (Phase 6)

- **Swagger UI** is enabled only in Development.
- **Security headers** are applied on every API response (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, baseline `Content-Security-Policy`).
- **HSTS** is enabled outside Development.
- **`AllowedHosts`**: Development allows `*`; Production defaults to `localhost;api.taskmanager.com` — override via `appsettings.Production.json` or environment for your deployment hostname.
