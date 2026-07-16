# Task Manager MCP Server

Standalone [Model Context Protocol](https://modelcontextprotocol.io/) server that exposes Task Manager todos to MCP clients such as Claude Desktop. It talks to the existing WebApi over HTTP with a bearer token; it does **not** embed business logic or talk to the database directly.

> **Note on `EnableMcp`:** `Ai:Features:EnableMcp` in WebApi `appsettings` is a conceptual flag documenting that MCP exists as a separate process. Starting this console app is what actually enables MCP for a client — WebApi does not host the MCP transport.

## Prerequisites

1. WebApi running locally (default HTTP: `http://localhost:5000`, HTTPS: `https://localhost:5001`).
2. A valid JWT access token for an authenticated Task Manager user (`API_ACCESS_TOKEN`).
3. .NET 10 SDK.

## Environment variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `API_URL` | No | `http://localhost:5000` | Base URL of WebApi (no trailing slash required) |
| `API_ACCESS_TOKEN` | **Yes** | — | Bearer JWT sent on every API call |

## Obtaining / refreshing `API_ACCESS_TOKEN`

Auth0 access tokens expire (typically within hours). When tools start returning `401 Unauthorized`, mint a fresh token and update your MCP config (or shell env), then restart Claude Desktop.

**Option A — Blazor WASM (DevTools)**

1. Run WebApp, sign in via Auth0.
2. Open browser DevTools → **Application** / **Storage** (or **Network**).
3. Copy the access token used on API calls (`Authorization: Bearer …` on requests to WebApi), or from the Auth0 SPA cache if visible.

**Option B — Network tab**

1. With WebApp logged in, trigger any authenticated API call (e.g. open Todos).
2. In DevTools → **Network**, select the request to `/api/todos`.
3. Copy the `Authorization` header value after `Bearer `.

There is no long-lived machine-client / refresh-token helper in this MVP — paste a current user JWT when it expires.

## Resources

| URI | Description |
|---|---|
| `taskmanager://todos` | All todos for the authenticated user (`GET /api/todos`) |
| `taskmanager://statistics` | Computed summary (counts by priority/status, overdue, due this week) |

## Tools

| Tool | Arguments | Description |
|---|---|---|
| `create_todo` | `title`, `description`, `priority`, `dueDate` | Creates a todo (`POST /api/todos`), then sets priority via `PUT` when needed (priority failure returns a `warning`, not a hard error) |
| `update_todo` | `externalId`, optional `title` / `description` / `priority` / `dueDate` | Updates a todo (`PUT /api/todos/{id}`); omitted fields keep current values |
| `search_todos` | `query` | Semantic search when available; on `503`/`429` falls back to keyword filter (`rateLimited: true` when applicable). Auth/validation errors surface as tool errors |
| `analyze_workload` | _(none)_ | Same metrics as the statistics resource |

All API calls include `Authorization: Bearer <API_ACCESS_TOKEN>`.

## Run locally

```bash
export API_URL="http://localhost:5000"
export API_ACCESS_TOKEN="eyJ..."   # paste a real JWT

dotnet run --project src/McpServer/McpServer.csproj
```

The server uses **stdio** transport (stdout is reserved for MCP JSON-RPC; logs go to stderr).

## Claude Desktop configuration

Add an entry to Claude Desktop's MCP config (typically `~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

```json
{
  "mcpServers": {
    "TaskManager": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/task-manager-blazor-wasm-aspnet-api/src/McpServer"],
      "env": {
        "API_URL": "http://localhost:5000",
        "API_ACCESS_TOKEN": "your-jwt-here"
      }
    }
  }
}
```

Prefer `http://localhost:5000` so Claude Desktop does not need to trust the ASP.NET Core HTTPS development certificate. Use `https://localhost:5001` only if that process trusts the cert.

Restart Claude Desktop after editing the config.

## Build & test

```bash
dotnet build src/McpServer/McpServer.csproj
dotnet test src/Tests/McpServer.Tests/McpServer.Tests.csproj
```

## Example prompts (Claude)

- "What todos do I have this week?"
- "Create a high-priority task to fix the login bug due Friday."
- "Update that task's due date to next Monday."
- "Search my tasks for payment integration."
- "Analyze my workload and call out anything overdue."
