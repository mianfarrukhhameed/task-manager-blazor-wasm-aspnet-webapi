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
