# Task Manager Aspire AppHost

Orchestrates local development for Task Manager:

- PostgreSQL with **pgvector** (`pgvector/pgvector:pg16`)
- **WebApi** on ports 5000 (HTTP) / 5001 (HTTPS)
- **WebApp** (Blazor WASM) on port 5002 (HTTPS)
- Optional **pgAdmin** on host port 5050

## Run

```bash
dotnet run --project src/AppHost/AppHost.csproj
```

The Aspire dashboard opens automatically. WebApi receives `ConnectionStrings__MainDb` from the `MainDb` database resource.

## Notes

- Set AI keys via WebApi user-secrets or process environment (same as non-Aspire runs).
- MCP Server is not started by AppHost (stdio / Claude Desktop process).
- Prefer this over `docker compose` for day-to-day API + UI work; avoid running both Postgres stacks unless you know you need two databases.
