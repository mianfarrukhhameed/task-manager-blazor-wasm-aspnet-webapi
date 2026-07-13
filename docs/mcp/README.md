# Task Manager MCP Server

Standalone [Model Context Protocol](https://modelcontextprotocol.io/) server that exposes Task Manager todos to MCP clients such as Claude Desktop. It talks to the existing WebApi over HTTP with a bearer token; it does **not** embed business logic or talk to the database directly.

> **Note on `EnableMcp`:** `Ai:Features:EnableMcp` in WebApi `appsettings` is a conceptual flag documenting that MCP exists as a separate process. Starting this console app is what actually enables MCP for a client — WebApi does not host the MCP transport.

## Prerequisites

1. WebApi running locally (default HTTPS: `https://localhost:5001`, HTTP: `http://localhost:5000`).
2. A valid JWT access token for an authenticated Task Manager user (`API_ACCESS_TOKEN`).
3. .NET 10 SDK.

## Environment variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `API_URL` | No | `https://localhost:5001` | Base URL of WebApi (no trailing slash required) |
| `API_ACCESS_TOKEN` | **Yes** | — | Bearer JWT sent on every API call |

## Resources

| URI | Description |
|---|---|
| `taskmanager://todos` | All todos for the authenticated user (`GET /api/todos`) |
| `taskmanager://statistics` | Computed summary (counts by priority/status, overdue, due this week) |

## Tools

| Tool | Arguments | Description |
|---|---|---|
| `create_todo` | `title`, `description`, `priority`, `dueDate` | Creates a todo (`POST /api/todos`), then sets priority via `PUT` when needed |
| `search_todos` | `query` | Semantic search (`POST /api/ai/todos/search/semantic`) when available; otherwise keyword filter on the todo list |
| `analyze_workload` | _(none)_ | Same metrics as the statistics resource |

All API calls include `Authorization: Bearer <API_ACCESS_TOKEN>`.

## Run locally

```bash
export API_URL="https://localhost:5001"
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

Use `https://localhost:5001` if your client trusts the ASP.NET Core dev certificate; otherwise prefer `http://localhost:5000`.

Restart Claude Desktop after editing the config.

## Build & test

```bash
dotnet build src/McpServer/McpServer.csproj
dotnet test src/Tests/McpServer.Tests/McpServer.Tests.csproj
```

## Example prompts (Claude)

- "What todos do I have this week?"
- "Create a high-priority task to fix the login bug due Friday."
- "Search my tasks for payment integration."
- "Analyze my workload and call out anything overdue."
