# CLAUDE.md

## Project Overview
Single Page Application (SPA) with a .NET 10 backend and an HTML5/TypeScript frontend (Vite, no React).

---

## Features

Documentation lives in the Obsidian vault at:
`/home/agis/Documents/Obsidian Vault/Projects/Bebrakumpis/`

- Feature specs:        `Features/{YYYY-MM-DD}/{FeatureName}/Feature.md`
- Implementation plans: `Features/{YYYY-MM-DD}/{FeatureName}/Plan.md`
- PR reviews:           `Features/{YYYY-MM-DD}/{FeatureName}/PR-Review.md`
- Feature index (all features + status): `Features/Index.md`
- Project overview: `PROJECT.MD.md`

When a feature is spec-only (no plan yet), create `Features/Backlog/{FeatureName}/` and add `Feature.md` there.
When planning starts, move the feature from `Features/Backlog/{FeatureName}/` to `Features/{YYYY-MM-DD}/{FeatureName}/` (date = today's date; create the date folder first if it doesn't exist).
When a plan is created, save it to `Features/{YYYY-MM-DD}/{FeatureName}/Plan.md` in the vault and add the feature to `Features/Index.md` with status `Active`.
When a PR review is complete, save it to `Features/{YYYY-MM-DD}/{FeatureName}/PR-Review.md` in the vault.
Mark the feature as Done in `Features/Index.md` when complete.

## Architecture

### Backend — .NET 10 / C# / Dapper / Azure SQL Server / Onion Architecture

```
src/
├── Domain/                  # Entities, value objects, domain interfaces
│   ├── Entities/
│   └── Interfaces/          # IRepository<T>, etc.
├── Application/             # Use cases, DTOs, service interfaces
│   ├── Common/
│   │   ├── CQRS/            # IMediator, IRequest<T>, IRequestHandler<TReq,TRes>, Mediator
│   │   ├── Result/          # Result<T>, ErrorType
│   │   └── Behaviors/       # IPipelineBehavior, LoggingBehavior
│   ├── Features/            # Organized by feature (CQRS-style)
│   │   └── {Feature}/
│   │       ├── Commands/
│   │       ├── Queries/
│   │       ├── DTOs/
│   │       ├── Validators/
│   │       └── Mappings/    # present in some features
│   └── Interfaces/          # IService contracts (ITokenService, etc.)
├── Infrastructure/          # Dapper repos, DB connection, external services
│   ├── Persistence/
│   │   ├── Repositories/
│   │   └── DbConnectionFactory.cs   # SqlConnectionFactory (Microsoft.Data.SqlClient)
│   └── Services/
└── API/                     # ASP.NET Core entry point
    ├── Controllers/
    ├── Middleware/
    ├── Migrations/          # MigrationRunner — runs embedded SQL on startup
    ├── Common/              # UserExtensions, ResultExtensions
    ├── Program.cs
    └── appsettings.json
```

#### Key Backend Conventions
- Follow **Onion Architecture**: dependencies point inward (API → Application → Domain; Infrastructure → Domain)
- **Domain** has zero external dependencies
- **Application** depends only on Domain; no infrastructure concerns
- **Infrastructure** implements Domain interfaces (repositories, services)
- Use **Dapper** for all data access — no EF Core
- SQL lives inline in the repository method as a raw string literal (`"""..."""`) — no separate Queries files
- Repository pattern: one repository per aggregate root
- CQRS via custom mediator (`IMediator.SendAsync`) — **not MediatR**; commands/queries implement `IRequest<Result<T>>`, handlers implement `IRequestHandler<TReq, TResult>`
- Return `Result<T>` from all commands/queries — never return nulls silently; use `Result<T>.Success()`, `.NotFound()`, `.ValidationFailure()`, `.Conflict()` factory methods
- Use `IDbConnectionFactory` (`SqlConnectionFactory`) to inject and manage `IDbConnection`
- Validate input in the Application layer (FluentValidation)
- Use `CancellationToken` in all async methods
- Use `var` in C#
- Use **primary constructors** for all dependency injection — no explicit constructor bodies or private readonly field declarations for injected dependencies
- Database migrations are `.sql` files embedded in the API assembly; `MigrationRunner` applies them automatically on startup — do not run migrations manually

#### Naming Conventions (C#)
- Classes / Methods: `PascalCase`
- Variables / Parameters: `camelCase`
- Private fields: `_camelCase`
- Interfaces: `IInterfaceName`
- DTOs: `{Feature}Request`, `{Feature}Response`
- Commands/Queries: `{Action}{Entity}Command`, `{Action}{Entity}Query`

#### API Conventions
- RESTful routes: `api/v1/{resource}`
- Always return `ProblemDetails` for errors
- Use `[ProducesResponseType]` on all endpoints
- Versioning via URL prefix (`v1`, `v2`)
- Authentication: JWT Bearer (configure in `Program.cs`)

---

### Frontend — Vanilla TypeScript / HTML5 (Vite)

```
src/
├── api/                     # Typed fetch wrappers per feature
│   ├── client.ts            # Base fetch wrapper (credentials: 'include' for cookie auth)
│   └── {feature}.api.ts
├── components/              # Shared UI helpers (layout, badges, modals)
│   ├── layout.ts            # Sidebar shell — renderLayout(contentHtml)
│   ├── badge.ts             # Status badges, spinner, error message
│   └── modal.ts             # Modal open/close helpers
├── pages/                   # Page renderers (one function per route)
│   └── {page}.ts
├── types/                   # Shared TypeScript interfaces
│   └── index.ts
├── utils/                   # Pure utility functions
│   ├── escHtml.ts           # escHtml() / escAttr() — XSS-safe HTML insertion
│   └── date.ts              # Date formatting helpers
├── auth.ts                  # Auth helpers (read user from cookie claims, logout)
├── router.ts                # Hash-based SPA router
└── main.ts                  # Entry point — registers routes, starts router
```

#### Key Frontend Conventions
- **TypeScript** is mandatory — no `any`, use strict mode
- **No React, no framework** — vanilla DOM manipulation via `innerHTML`
- All CSS lives in `index.html` inside a single `<style>` block; use the `bh-*` class naming system
- Each page exports one `render{Page}(params)` async function; it calls `renderLayout(spinner())` first, then updates `#page-content` after data loads
- API modules use `fetch` with a shared `client.ts` wrapper — no axios
- Routing is hash-based (`#/bebras-tests`, `#/bebras-tests/:id`); register routes in `main.ts`
- State is module-level variables in each page file — no global store
- Responsive design: use `bh-form-grid bh-form-grid-2` for two-column form grids, `bh-stat-grid` for three-column stat cards, `bh-table-scroll` to wrap tables for horizontal scroll on mobile

#### Naming Conventions (TypeScript)
- Files / functions: `camelCase.ts`, `renderPageName()`
- API functions: `camelCase` (e.g., `getTests`, `createTest`)
- Types/Interfaces: `PascalCase`
- CSS utility classes: `bh-*` prefix

#### Page Rules
- Always call `renderLayout(spinner())` before any async work
- Update `document.getElementById('page-content')!.innerHTML` after data loads
- Handle loading, error, and empty states explicitly
- Escape all user-supplied strings with `escHtml()` / `escAttr()` before inserting into HTML

---

## Development Guidelines

### General
- Always write code in English (comments, variables, identifiers)
- Prefer explicit over implicit
- No commented-out dead code in commits
- Keep functions short (aim for < 30 lines)

### Testing
- **Backend unit**: xUnit + Moq; test Application layer handlers and validators
- **Backend integration**: xUnit + `WebApplicationFactory` + SQLite in-memory (`Microsoft.Data.Sqlite`); test full request/response via `HttpClient`
- Name tests: `{method/scenario}_Should{ExpectedBehavior}_When{Condition}`

### Git
- Conventional commits: `feat:`, `fix:`, `refactor:`, `chore:`, `test:`, `docs:`
- Branch naming: `feature/`, `fix/`, `chore/`
- Never commit directly to `main`

### Environment
- Store secrets in `appsettings.{Environment}.json` (backend) and `.env.local` (frontend)
- Never commit `.env` files or connection strings to source control
- Use `appsettings.json` for non-sensitive defaults only

---

## Running the Application

### Backend
```bash
# Migrations run automatically on startup — no manual step needed.
# Set the connection string in appsettings.Development.json or via env var.

cd backend/src/Bebrakumpis.API
dotnet run   # default: http://localhost:5156
```

### Frontend
```bash
cd frontend-html5
npm install
npm run dev   # http://localhost:5174
```

### Build
```bash
# Backend
cd backend && dotnet build Bebrakumpis.slnx

# Frontend
cd frontend-html5 && npm run build
```

### Default credentials
- Username: `admin`, Password: `Admin@123`
- Update `appsettings.Development.json` with an Azure SQL Server (or local SQL Server / SQLite) connection string

---

## Common Tasks

### Add a new feature (backend)
1. Define entity/value object in `Domain/`
2. Add repository interface in `Domain/Interfaces/`
3. Create Command/Query + DTO in `Application/Features/{Feature}/`
4. Implement repository in `Infrastructure/Persistence/Repositories/`
5. Expose endpoint in `API/Controllers/`

### Add a new feature (frontend)
1. Add types to `src/types/index.ts`
2. Add API call in `src/api/{feature}.api.ts`
3. Create page renderer in `src/pages/{page}.ts` (export `render{Page}()`)
4. Register route in `src/main.ts` via `router.on()`

---

## JWT Authentication

- Token is delivered via `bh_auth` **HttpOnly cookie** — the frontend never reads or sets it; `credentials: 'include'` on every fetch is sufficient
- `MapInboundClaims = false` is set on `AddJwtBearer` — raw JWT claim names are preserved as-is; .NET does **not** remap them to `ClaimTypes.*`
- `NameClaimType = "sub"`, `RoleClaimType = "role"` — `User.Identity.Name` resolves from `"sub"`, `User.IsInRole("Admin")` resolves from `"role"`
- `GetCurrentUserId()` in controllers reads `User.FindFirstValue(JwtRegisteredClaimNames.Sub)` only — **never** fall back to `ClaimTypes.NameIdentifier`
- `JwtTokenService` issues `"sub"` for userId and `"role"` (literal string, **not** `ClaimTypes.Role`) for the role claim — `ClaimTypes.Role` serializes as the long URI and would never match `RoleClaimType = "role"`
- Always guard non-admin endpoints: if `GetCurrentUserId()` returns null, return `Unauthorized()` rather than leaking data

---

## Deployment

- **Backend**: Dockerized .NET app deployed to **Azure App Service** (container). Image built and pushed to **Azure Container Registry** (ACR) by GitHub Actions on push to `main`.
- **Frontend**: Static files deployed to **Azure Static Web Apps** via `Azure/static-web-apps-deploy` action; `VITE_API_BASE_URL` injected at build time from GitHub secret.
- **Monitoring**: **Azure Application Insights** (`Microsoft.ApplicationInsights.AspNetCore`) — telemetry auto-collected.
- **CI**: GitHub Actions `ci.yml` — builds backend + frontend, runs xUnit tests on every PR. (planned — not yet created)
- **CD**: GitHub Actions `deploy.yml` — test → build Docker → push to ACR → deploy App Service → deploy SWA on push to `main`. (planned — not yet created)

---

## Do Not
- Do not make changes on main branch
- Do not use EF Core — Dapper only
- Do not put business logic in Controllers or Infrastructure
- Do not use `any` in TypeScript
- Do not bypass the Application layer from the API layer
- Do not store sensitive data in `localStorage` or `sessionStorage`
- Do not set `MapInboundClaims = true` — it breaks role/identity claim resolution in this project
