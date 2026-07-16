# Task Manager MCP Server

Standalone [Model Context Protocol](https://modelcontextprotocol.io/) server for Claude Desktop. It calls WebApi over HTTP as the **signed-in Auth0 user** (Device Code + refresh token). It does **not** talk to the database directly.

## Auth0 setup (once)

1. Create an Auth0 **Native** application (public client — no secret in Claude config).
2. Enable **Device Code** and **Refresh Token** grants; allow `offline_access`.
3. Authorize the app for API audience `https://api.taskmanager.com/` (same as WebApi).
4. Note the **Domain** and **Client ID**.

## Environment variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `API_URL` | No | `http://localhost:5000` | WebApi base URL |
| `AUTH0_DOMAIN` | Yes* | — | e.g. `dev-xxx.us.auth0.com` (`AUTH0_AUTHORITY` also accepted) |
| `AUTH0_CLIENT_ID` | Yes* | — | Native app client id |
| `AUTH0_AUDIENCE` | No | `https://api.taskmanager.com/` | API audience |
| `AUTH0_SCOPE` | No | `openid profile email offline_access` | OAuth scopes |
| `API_ACCESS_TOKEN` | No | — | Optional static JWT override (CI/tests only) |

\*Required unless `API_ACCESS_TOKEN` is set.

Tokens are cached at `~/.config/taskmanager-mcp/tokens.json` (Windows: `%LOCALAPPDATA%\taskmanager-mcp\tokens.json`), mode `600` on Unix. Delete that file to force re-login.

## First-time login

On first tool call (or after cache clear / refresh failure), MCP logs to **stderr**:

```text
Task Manager MCP: sign in required.
Open: https://...
```

Complete the browser login. Claude Desktop MCP logs show this output. Later runs refresh silently.

## Resources / tools

| URI / tool | Description |
|---|---|
| `taskmanager://todos` | All todos for the authenticated user |
| `taskmanager://statistics` | Workload summary |
| `create_todo` | Create (+ priority via PUT when needed) |
| `update_todo` | Partial update |
| `search_todos` | Semantic search with keyword fallback |
| `analyze_workload` | Same metrics as statistics |

## Run locally

```bash
export API_URL="http://localhost:5000"
export AUTH0_DOMAIN="dev-xxx.us.auth0.com"
export AUTH0_CLIENT_ID="your-native-client-id"
export AUTH0_AUDIENCE="https://api.taskmanager.com/"

dotnet run --project src/McpServer/McpServer.csproj
```

stdio transport: stdout = MCP JSON-RPC; logs = stderr.

## Claude Desktop configuration

`~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "TaskManager": {
      "command": "dotnet",
      "args": [
        "exec",
        "/absolute/path/to/src/McpServer/bin/Debug/net10.0/McpServer.dll"
      ],
      "env": {
        "API_URL": "http://localhost:5000",
        "AUTH0_DOMAIN": "dev-xxx.us.auth0.com",
        "AUTH0_CLIENT_ID": "your-native-client-id",
        "AUTH0_AUDIENCE": "https://api.taskmanager.com/"
      }
    }
  }
}
```

Prefer `http://localhost:5000` so Claude does not need the ASP.NET HTTPS dev cert. Restart Claude after config changes. Rebuild the DLL after MCP code changes (`dotnet build src/McpServer`).

## Build & test

```bash
dotnet build src/McpServer/McpServer.csproj
dotnet test src/Tests/McpServer.Tests/McpServer.Tests.csproj
```

## Example prompts

- "What todos do I have this week?"
- "Create a high-priority task to fix the login bug due Friday."
- "Update that task's due date to next Monday."
- "Search my tasks for payment integration."
- "Analyze my workload and call out anything overdue."
