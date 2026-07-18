# 7Pace Timetracker MCP Server (.NET)

A Model Context Protocol (MCP) server for 7Pace Timetracker, built with .NET 10 and the MCP C# SDK. Enables AI assistants to log time, manage worklogs, list activity types, and generate time reports through the 7Pace REST API v3.2.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A 7Pace Timetracker API token (generate from 7pace Settings → Reporting and API)

## Configuration

All configuration is via environment variables:

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `SEVENPACE_ORGANIZATION` | Yes | — | Your 7pace organization name (e.g., `labournet`) |
| `SEVENPACE_TOKEN` | Yes* | — | 7pace API token. When missing or placeholder, server runs in limited mode |
| `SEVENPACE_BASE_URL` | No | `https://{org}.timehub.7pace.com` | Custom base URL override |
| `SEVENPACE_WRITE_TIMEOUT_MS` | No | `30000` | Timeout for write operations (create/update/delete) in milliseconds |
| `TZ` | No | UTC-3 | IANA timezone ID (e.g., `America/Sao_Paulo`) for date-to-timestamp conversion |
| `PORT` | No | — | When set, enables HTTP transport (Streamable HTTP stateless) on `/mcp` |

*`SEVENPACE_TOKEN` is not strictly required for startup — when absent or set to the placeholder `test-token-replace-with-real-token`, the server runs in **limited mode** (returns informational messages without calling the 7Pace API).

## Build and Run

### Build

```bash
dotnet build SevenPaceMcp.sln
```

### Run (stdio mode — default)

```bash
# Set environment variables
export SEVENPACE_ORGANIZATION=your_org
export SEVENPACE_TOKEN=your_token

# Run
dotnet run --project src/SevenPaceMcp
```

### Run (HTTP mode)

```bash
export SEVENPACE_ORGANIZATION=your_org
export SEVENPACE_TOKEN=your_token
export PORT=3000

dotnet run --project src/SevenPaceMcp
```

The server will be available at `http://localhost:3000/mcp`.

### Run Tests

```bash
dotnet test
```

## Transport Modes

### stdio (default)

When `PORT` is not set, the server uses stdio transport — MCP JSON-RPC frames over stdin/stdout. This is the standard mode for local MCP clients (Claude Desktop, VS Code, etc.).

**Logging** goes to stderr only; stdout is reserved for MCP protocol.

### HTTP (Streamable HTTP stateless)

When `PORT` is set to a valid integer, the server starts as an ASP.NET Core web app with the MCP Streamable HTTP transport in stateless mode. The MCP endpoint is available at `/mcp`.

This mode is suitable for containerized deployments and remote access.

## MCP Tools

The server exposes 8 tools:

### health

Simple health check for deployment scanners.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| — | — | — | No parameters |

Returns server status and limited mode indicator.

### configure_sevenpace

Configure 7pace credentials at runtime (validates inputs and provides guidance).

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `organization` | string | Yes | 7pace organization name |
| `token` | string | Yes | 7pace API token |
| `baseUrl` | string | No | Optional base URL override |

**Note:** In this .NET implementation, runtime configuration changes require restarting the server with new environment variables. This tool validates inputs and returns guidance.

### log_time

Log time entry to 7pace Timetracker for a specific work item.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `workItemId` | int | Yes | Azure DevOps Work Item ID |
| `date` | string | Yes | Date in YYYY-MM-DD format |
| `hours` | double | Yes | Number of hours worked |
| `description` | string | Yes | Description of work performed |
| `activityType` | string | No | Activity type (name or ID) |

### list_activity_types

List available 7pace activity types (name and id).

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| — | — | — | No parameters |

### get_worklogs

Retrieve time logs from 7pace Timetracker.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `workItemId` | int | No | Filter by specific work item ID |
| `startDate` | string | No | Start date in YYYY-MM-DD format |
| `endDate` | string | No | End date in YYYY-MM-DD format |

### update_worklog

Update an existing time log entry.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `worklogId` | string | Yes | ID of the worklog to update |
| `workItemId` | int | No | New work item ID |
| `hours` | double | No | New number of hours |
| `description` | string | No | New description |

### delete_worklog

Delete a time log entry.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `worklogId` | string | Yes | ID of the worklog to delete |

### generate_time_report

Generate time tracking report for a date range.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `startDate` | string | Yes | Start date in YYYY-MM-DD format |
| `endDate` | string | Yes | End date in YYYY-MM-DD format |
| `userId` | string | No | Filter by specific user ID |

## Limited Mode

When `SEVENPACE_TOKEN` is not set or is the placeholder value (`test-token-replace-with-real-token`), the server runs in **limited mode**:

- All tools return informational messages instead of calling the 7Pace API
- The `health` tool reports `limitedMode: true`
- This allows deployment scanners to verify the server is reachable without valid credentials

## Docker

### Build

```bash
docker build -t sevenpace-mcp .
```

### Run (stdio mode)

```bash
docker run -e SEVENPACE_ORGANIZATION=your_org \
           -e SEVENPACE_TOKEN=your_token \
           sevenpace-mcp
```

### Run (HTTP mode)

```bash
docker run -e SEVENPACE_ORGANIZATION=your_org \
           -e SEVENPACE_TOKEN=your_token \
           -e PORT=3000 \
           -p 3000:3000 \
           sevenpace-mcp
```

## Architecture

This server follows the architecture of [mcp-devops](https://github.com/mcp-devops) with:

- **Generic Host** with DI and console logging to stderr
- **Typed HttpClient** (`SevenPaceClient`) for 7Pace REST API v3.2
- **Service layer** (WorklogService, ActivityTypeService, ReportService) with validation and limited mode
- **MCP Tools** as a partial class with `[McpServerToolType]` and `[McpServerTool]` attributes
- **OperationResult** envelope for consistent responses
- **Source-generated JSON serialization** via `JsonSerializerContext`

### Key Design Decisions

**billableLength**: The `billableLength` property is intentionally **not included** in worklog create/update payloads, matching the behavior of the reference Node.js implementation. The property does not exist in the DTOs (`CreateWorklogRequest` and `UpdateWorklogRequest`), so it is structurally absent from the serialized JSON — not sent as `null` or `0`.

**timeStamp (not date)**: The 7Pace REST API v3.2 officially expects a `timeStamp` field (ISO 8601 datetime with timezone offset) in worklog creation payloads, not a `date` field. The reference Node.js implementation used `date` (a date-only string like `"2026-07-16"`), which the API does not recognize — causing it to fall back to the server's current timestamp and potentially record the wrong date. This .NET implementation converts the date-only input to a proper ISO 8601 timestamp at midnight in the configured timezone (e.g., `"2026-07-16T00:00:00-03:00"`), ensuring the correct date is recorded regardless of when the request is made. The timezone is configurable via the `TZ` environment variable (IANA ID), with a fallback of UTC-3.

## API Reference

- 7Pace REST API v3.2: `https://{organization}.timehub.7pace.com/api/rest`
- Authentication: Bearer token
- Endpoints used:
  - `GET /api/rest/activitytypes?api-version=3.2`
  - `POST /api/rest/workLogs?api-version=3.2`
  - `GET /api/rest/worklogs?api-version=3.2`
  - `PUT /api/rest/worklogs/{id}?api-version=3.2`
  - `DELETE /api/rest/worklogs/{id}?api-version=3.2`
  - `GET /api/rest/reports/time?api-version=3.2`