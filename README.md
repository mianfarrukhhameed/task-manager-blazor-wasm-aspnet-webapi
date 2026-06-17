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
    participant DB as SQL Server (EF Core)

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
| `ConnectionStrings__MainDb` | Yes | SQL Server connection string for the main database |
| `Ai__GoogleAI__ApiKey` | When `Ai:Provider` is `google` | Google AI (Gemini) API key |
| `Ai__Claude__ApiKey` | When `Ai:Provider` is `claude` | Anthropic API key |
| `Ai__OpenAI__ApiKey` | When `Ai:Provider` is `openai` | OpenAI API key |
| `App__ApiAccessKey` | Optional | API access key if the `HaveApiAccessKey` filter is enabled |
| `AppConfigConnectionString` | Optional | Azure App Configuration connection string |
| `AppConfigEnvironmentName` | Optional | Azure App Configuration label filter (e.g. `dev`, `prod`) |
| `KeyVault__Uri` | Production | Azure Key Vault URI; loaded when `ASPNETCORE_ENVIRONMENT` is `Production` |


### Local development setup

1. Copy required values into `src/WebApi/Properties/launchSettings.json` under `environmentVariables`, **or** use user secrets:

```bash
cd src/WebApi
dotnet user-secrets set "ConnectionStrings:MainDb" "Server=localhost,1433;Database=Task-db;User Id=sa;Password=<your-password>;TrustServerCertificate=True;"
```

2. Export AI keys in your shell or IDE run configuration (`launchSettings.json` environment variables):

```bash
export Ai__GoogleAI__ApiKey="your-google-ai-key"
export Ai__Claude__ApiKey="your-anthropic-key"
```

3. Never commit API keys, database passwords, or other secrets to git. `launchSettings.json` is gitignored; use placeholders only in tracked files.

### Logging

LLM prompts and task content are logged at **Debug** level only. At **Information** level, the AI pipeline logs metadata (provider, model, content lengths) without user data. Enable Debug logging locally when troubleshooting summarization:

```json
"Logging": {
  "LogLevel": {
    "Fistix.TaskManager.AiLayer": "Debug"
  }
}
```
