# Task Manager - Blazor WASM + ASP.NET API Solution Overview

## Project Structure

```
src/
├── WebApi/                  # ASP.NET Core 10.0 REST API
├── WebApp/                  # Blazor WebAssembly 10.0 Client
├── Core/                    # Core business logic & domain models
├── ServiceLayer/            # Application services & CQRS handlers
├── DataLayer/               # Entity Framework Core data access
├── ViewModel/               # DTOs, commands, queries, validators
└── Sql/                     # (retired — schema via EF Core + PostgreSQL)
```

## Tech Stack

### Backend (WebApi)
- **Framework**: ASP.NET Core 10.0 (minimal hosting model)
- **Architecture**: CQRS with MediatR
- **Database**: PostgreSQL with Entity Framework Core 10.0.0 (Npgsql)
- **Authentication**: OAuth2 with Auth0 (JWT Bearer tokens)
- **API Documentation**: Swagger/Swashbuckle 6.8.0
- **Validation**: FluentValidation 11.8.1
- **Mapping**: AutoMapper 12.0.1
- **Features**:
  - Azure App Configuration support
  - Application Insights telemetry
  - Custom authorization with role-based policies
  - CORS configuration
  - XML documentation

### Frontend (WebApp)
- **Framework**: Blazor WebAssembly 10.0
- **Authentication**: OIDC with Auth0 (RemoteAuthenticatorView)
- **HTTP Client**: Custom authorization message handler
- **Validation**: FluentValidation 11.8.1 + Accelist.FluentValidation.Blazor 4.0.0
- **Features**:
  - Server-side auth0 logout flow
  - Cascading authentication state
  - Protected routes with AuthorizeRouteView
  - Service/state management pattern
  - Todo CRUD operations

## Dependency Injection & Configuration

### WebApi (Program.cs)
```csharp
- Services registered via extensions:
  - AddCommonServices() → Swagger, Auth0, CORS, Authorization
  - AddServiceLayer() → MediatR, AutoMapper, ServiceLayer
  - AddControllers() → FluentValidation
- Configuration sources:
  - appsettings.json (base)
  - Azure App Configuration (if AppConfigConnectionString env var set)
  - Environment variables
```

### WebApp (Program.cs)
```csharp
- Services registered via extensions:
  - SetupAuth0Service() → OIDC authentication
  - SetupDefaultApiClient() → Named HttpClient with auth handler
  - AddValidatorsFromAssembly() → FluentValidation validators
- Configuration:
  - wwwroot/appsettings.json (client-side)
  - Auth0 settings
  - API endpoint URL
```

## Authentication Flow

### Auth0 Integration
1. **WebApp**: User initiates login → RemoteAuthenticatorView navigates to Auth0
2. **Auth0**: User authenticates, returns ID token + access token
3. **WebApp**: Tokens stored in browser session storage (via RemoteAuthenticatorView)
4. **API Calls**: CustomAuthorizationMessageHandler automatically attaches access token
5. **WebApi**: Validates JWT token against Auth0 authority
6. **Claims**: Token validated, claims extracted (user identity, roles, etc.)
7. **Logout**: Auth0 v2 logout endpoint (custom implementation in Authentication.razor)

## Project-Specific Patterns

### CQRS Pattern (WebApi)
- Commands: `CreateTodoTaskCommand`, etc. (write operations)
- Queries: `GetAllTodoTasksQuery`, etc. (read operations)
- Handlers: Registered with MediatR
- Validators: Fluent validators for each command/query

### State Management (WebApp)
- `TodoDataService`: Direct API communication
- `TodoStateService`: Client-side state management
- Components: Stateless presentation layer

### Authorization Levels
- **Authenticated**: All logged-in users (implicit)
- **Admin**: Users with `Role: Admin` claim (explicit policy)
  - Set via `ICurrentUserService.HasAdminProfile`

## Configuration Files

### appsettings.json (WebApi)
```json
- ConnectionStrings: PostgreSQL (see docker-compose.yml)
- App: CORS origins, API access key
- Auth0: Authority, Audience, ClientId
- Swagger: Version, title
- Azure: Storage config (optional)
```

### appsettings.json (WebApp - wwwroot/)
```json
- Auth0: Authority, ClientId, Scope
- API: Base URL (https://localhost:5001/)
```

### launchSettings.json
- Removed deprecated `dotnetRunMessages` property
- Both projects use HTTPS with port binding
- WebApi: 5000 (HTTP), 5001 (HTTPS)
- WebApp: 5200 (HTTP), 5002 (HTTPS)

## Key Services

### WebApi Services
- **CurrentUserService**: Extracts current user from HTTP context
- **TodoDataService**: (WebApp) Calls API endpoints
- **TodoStateService**: (WebApp) Manages local todo state
- **CustomAuthorizationMessageHandler**: Adds token to API requests

### Extension Methods
- `ServiceCollectionExtension`: DI registration (WebApi)
- `ServiceCollectionExtentions`: DI registration (WebApp)
- `ApplicationBuilderExtension`: Middleware setup (WebApi)
- `MasterConfigExtension`: Config population (WebApi)

## .NET 10 Compatibility Status

### ✅ Fully Compatible
- All core ASP.NET packages (10.0.0)
- Entity Framework Core (10.0.0)
- Blazor WebAssembly (10.0.0)
- Authentication packages (10.0.0)
- FluentValidation (11.8.1)
- AutoMapper (12.0.1)

### ✅ Modernized
- Migrated WebApi from Startup.cs → minimal hosting (Program.cs)
- Removed deprecated `Router.PreferExactMatches`
- Removed deprecated `SignOutSessionStateManager`
- Fixed deprecated `launchSettings.json` property
- Updated packages for .NET 10 support

### ⚠️ Known Issues
- Accelist.FluentValidation.Blazor may have validation runtime issues (pre-existing)
- SurveyPrompt component namespace resolution in Index.razor (cosmetic warning)
- AutoMapper 12.0.1 has security advisory (low-impact, requires investigation)

## Build & Deployment

### Development
```bash
# WebApi
cd src/WebApi
dotnet run

# WebApp
cd src/WebApp
dotnet run
```

### Build
```bash
dotnet build src/WebApi/WebApi.csproj
dotnet build src/WebApp/WebApp.csproj
```

### Production
- WebApi: Deploy as standalone ASP.NET Core app (HTTPS required)
- WebApp: Deploy `wwwroot` folder as static content (CORS must allow origin)

## Security Considerations

1. **API Security**:
   - JWT validation against Auth0
   - CORS restricted to configured origins
   - Role-based authorization for admin features
   - HTTPS only in production

2. **Client Security**:
   - Tokens stored in session storage (not localStorage)
   - OIDC handles token refresh automatically
   - CustomAuthorizationMessageHandler prevents token leaks

3. **Database**:
   - PostgreSQL with Entity Framework Core (Npgsql)
   - Parameterized queries prevent SQL injection

## Future Enhancements

1. Consider upgrading Accelist.FluentValidation.Blazor to latest compatible version
2. Resolve AutoMapper vulnerability
3. Add automated integration tests
4. Implement SignalR for real-time updates
5. Add PWA capabilities to WebApp
6. Implement caching strategy for API responses

---

**Last Updated**: March 2026
**Target Framework**: .NET 10.0
**Status**: Production Ready
