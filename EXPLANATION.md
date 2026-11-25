# FinTrack — Architecture & Implementation Explanation

This document explains how the **Expense & Personal Finance Management System** (`FinTrack`) is structured, how data flows between the frontend and backend, and why key architectural decisions were made.

---

## 1. System Architecture Overview

The repository is organized into three main areas:

```text
Expense-tracker/
├── backend/                  # ASP.NET Core (.NET 9) Clean Architecture Web API
│   ├── src/
│   │   ├── ExpenseTracker.Domain/          # Entities, enums, domain exceptions
│   │   ├── ExpenseTracker.Application/     # DTOs, FluentValidation, business services
│   │   ├── ExpenseTracker.Infrastructure/  # EF Core (PostgreSQL), Identity, JWT, seeding
│   │   └── ExpenseTracker.Api/             # Controllers, middleware, filters, Swagger
│   └── tests/
│       ├── ExpenseTracker.UnitTests/       # Service-level xUnit tests
│       └── ExpenseTracker.IntegrationTests/# End-to-end API tests via WebApplicationFactory
├── frontend/                 # React 19 + TypeScript + Vite SPA
├── docker-compose.yml        # PostgreSQL 16 + Backend API + Frontend Nginx
└── .github/workflows/ci.yml  # Automated build, lint, and test pipeline
```

All financial calculations (totals, balances, savings rates, month-over-month comparisons, category breakdowns, and budget utilization) are computed on the **backend** rather than in the browser, ensuring a single source of truth and strict per-user data isolation.

---

## 2. Backend Layers

### A. Domain Layer (`ExpenseTracker.Domain`)
Contains core business models with no external framework dependencies (except Identity base class):
- **Entities**:
  - `ApplicationUser`: Extends `IdentityUser<Guid>` with `FirstName`, `LastName`, `CurrencyCode` (default `"USD"`), and `MonthlySavingsTarget`.
  - `RefreshToken`: Stores `TokenHash` (SHA-256 hash, never the raw token), expiration, IP metadata, revocation timestamps, and `ReplacedByTokenHash` for rotation tracking.
  - `Category`: Supports both system defaults (`IsDefault = true`, `UserId = null`) and user-created custom categories (`IsDefault = false`, `UserId = <guid>`).
  - `Transaction`: Tracks `Title`, `Amount` (`decimal(18,2)`), `Type`, `CategoryId`, `TransactionDate`, `PaymentMethod`, `Description`, and `Notes`.
  - `Budget`: Monthly spending limit per `(UserId, CategoryId, Year, Month)`.
  - `SavingsGoal`: Tracks `TargetAmount`, `CurrentAmount`, `TargetDate`, and computed `IsCompleted`.
- **Enums**: `TransactionType` (`Income`, `Expense`) and `PaymentMethod` (`Cash`, `CreditCard`, `DebitCard`, `BankTransfer`, `Upi`, `DigitalWallet`, `Other`).
- **Exceptions**: `DomainException` hierarchy (`NotFoundException`, `BadRequestException`, `ConflictException`, `ForbiddenException`, `UnauthorizedException`) mapped cleanly to HTTP status codes by middleware.

### B. Application Layer (`ExpenseTracker.Application`)
Defines contracts (`IAppDbContext`, `IJwtTokenService`, `IEmailService`, `ICurrentUserService`), DTOs, FluentValidation validators, and business services:
- **`AuthService`**: Handles registration, login, refresh-token rotation with reuse detection, token revocation, forgot-password / reset-password, and profile management.
- **`CategoryService`**:
  - Lists system default categories alongside the current user's custom categories, including per-user transaction counts.
  - Prevents editing or deleting system default categories or another user's categories (`ForbiddenException`).
  - **Deletion Protection**: Prevents deleting a custom category that is referenced by transactions (`ConflictException`) unless a valid `reassignToCategoryId` of the same `TransactionType` is provided, in which case transactions are reassigned before deletion.
- **`TransactionService`**: Supports full CRUD plus server-side search, date-range filtering, category/type filtering, sorting (`date` or `amount`, `asc` or `desc`), and pagination (`PagedResult<TransactionDto>`). Validates that the chosen category is accessible to the user and matches the transaction's `TransactionType`.
- **`BudgetService`**: Manages monthly category budgets (restricted to `Expense` categories) and calculates `SpentAmount`, `RemainingAmount`, and `UtilizationPercentage` for the requested month.
- **`SavingsGoalService`**: Manages savings goals and supports incremental contributions/withdrawals (`ContributeAsync`), preventing negative balances.
- **`DashboardService`**: Computes `DashboardOverviewDto` in one backend call:
  - `TotalIncome`, `TotalExpenses`, `SavingsAmount` (`TotalIncome - TotalExpenses`), and `MonthlySavingsRate` (`%`).
  - `CurrentBalance`: Cumulative all-time income minus expenses up to the end of the selected month.
  - `ComparisonWithPreviousMonth`: Percentage changes for income, expenses, and savings vs. the prior month.
  - `SpendingByCategory`: Expense distribution grouped by category.
  - `MonthlyCashflow`: 6-month trend ending at the selected month.
  - `BudgetUtilization`: Per-category budget progress and `IsOverBudget` flag.
  - `RecentTransactions`: Latest 5 transactions for the user.

### C. Infrastructure Layer (`ExpenseTracker.Infrastructure`)
- **`AppDbContext`**: Configures `IdentityDbContext`, decimal `(18,2)` precisions, foreign key delete behaviors (`Restrict` on `Transaction -> Category` to reinforce deletion safety), and database indexes (e.g., `(UserId, TransactionDate)` on `Transactions`, unique `(UserId, CategoryId, Year, Month)` on `Budgets`, unique `TokenHash` on `RefreshTokens`).
- **`JwtTokenService`**: Issues HMAC-SHA256 JWT access tokens and cryptographically random 64-byte refresh tokens hashed with SHA-256 before persistence.
- **`DataSeeder`**: Seeds `User` and `Admin` roles, 5 default Income categories, 10 default Expense categories, and a development demo account (`demo@expensetracker.dev` / `Demo@1234`) with sample transactions, budgets, and savings goals.

### D. Web API Layer (`ExpenseTracker.Api`)
- **Middleware & Filters**:
  - `ExceptionHandlingMiddleware` converts `ValidationException` and `DomainException` into RFC 7807 `ProblemDetails` JSON responses.
  - `ValidationActionFilter` automatically runs registered FluentValidation `IValidator<T>` instances on controller action arguments.
  - Serilog logs structured request and application telemetry.
- **Controllers**: `AuthController`, `ProfileController`, `CategoriesController`, `TransactionsController`, `BudgetsController`, `SavingsGoalsController`, `DashboardController`, and `AdminController` (protected by `[Authorize(Roles = "Admin")]`).
- **Health & OpenAPI**: Exposes `/health` and Swagger UI (in Development) with JWT Bearer authentication support.

---

## 3. Security & Authentication Flow

1. **Login / Register**: The API returns a short-lived JWT `accessToken` and a `refreshToken`.
2. **Request Authentication**: The frontend attaches `Authorization: Bearer <accessToken>` via an Axios request interceptor. Every query in Application services filters by `currentUser.UserId` so users can only access their own records.
3. **Refresh-Token Rotation**:
   - When an API call returns `401 Unauthorized`, the frontend Axios response interceptor calls `POST /api/auth/refresh` once (deduplicating concurrent 401s via a shared `refreshPromise`) and retries the original request.
   - On the server, `AuthService.RefreshTokenAsync` marks the old refresh token as revoked (`ReplacedByTokenHash`) and issues a new pair.
   - **Reuse Detection**: If a previously revoked refresh token is presented again, all active refresh tokens for that user are immediately revoked and `401 Unauthorized` is returned.

---

## 4. Frontend Architecture (`frontend/`)

- **Routing & Auth Context**: `AuthContext` manages the current user profile (`GET /api/profile`) and session tokens. `ProtectedRoute` guards authenticated routes inside `AppLayout` (responsive sidebar + mobile drawer).
- **Server State**: `@tanstack/react-query` caches queries (`dashboard`, `transactions`, `categories`, `budgets`, `savings-goals`) and invalidates dependent queries after mutations so dashboard metrics stay in sync.
- **Forms & Validation**: `react-hook-form` with `@hookform/resolvers/zod` validates user input client-side before hitting the API.
- **Data Visualization**: `recharts` renders the 6-month Income vs. Expenses `BarChart` and the Spending by Category donut `PieChart`.

---

## 5. Testing & CI/CD

- **Unit Tests (`ExpenseTracker.UnitTests`)**: Test `CategoryService`, `TransactionService`, and `DashboardService` against isolated EF Core In-Memory databases.
- **Integration Tests (`ExpenseTracker.IntegrationTests`)**: Use `WebApplicationFactory<Program>` (`CustomWebApplicationFactory`) to test HTTP endpoints end-to-end, including registration, refresh-token rotation and reuse detection, role-based authorization (`403 Forbidden` for non-admins on `/api/admin/stats`), and dashboard aggregation.
- **Docker & Nginx**: `docker-compose.yml` orchestrates PostgreSQL 16 (with healthchecks), the .NET 9 API, and the Nginx-served React frontend (which proxies `/api/` requests to `http://backend:8080/api/`).
- **GitHub Actions (`.github/workflows/ci.yml`)**: Runs `dotnet restore`, `dotnet build`, and `dotnet test` for the backend, and `npm ci`, `npm run lint`, and `npm run build` for the frontend on pushes and pull requests.
