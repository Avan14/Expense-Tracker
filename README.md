# FinTrack — Full-Stack Expense & Personal Finance Management System

A production-quality personal finance management platform built with **ASP.NET Core (.NET 9)**, **PostgreSQL**, **Entity Framework Core**, and **React + TypeScript**.

## Architecture

### Backend (`backend/`)
- **`ExpenseTracker.Domain`**: Core entities (`ApplicationUser`, `RefreshToken`, `Category`, `Transaction`, `Budget`, `SavingsGoal`), enums, and domain exceptions.
- **`ExpenseTracker.Application`**: DTOs, FluentValidation validators, and business services (`AuthService`, `CategoryService`, `TransactionService`, `BudgetService`, `SavingsGoalService`, `DashboardService`).
- **`ExpenseTracker.Infrastructure`**: `AppDbContext` (EF Core + ASP.NET Core Identity + PostgreSQL), `JwtTokenService` (JWT + SHA-256 hashed refresh token rotation), `EmailService`, and `DataSeeder`.
- **`ExpenseTracker.Api`**: REST API controllers, global `ExceptionHandlingMiddleware` (`ProblemDetails`), FluentValidation action filter, Serilog structured logging, and Swagger/OpenAPI.
- **`tests/`**: xUnit unit tests and `WebApplicationFactory` integration tests.

### Frontend (`frontend/`)
- **React 19 + TypeScript + Vite**
- **Tailwind CSS v4 + shadcn/ui-style components**
- **TanStack React Query** for server-state caching and invalidation
- **React Hook Form + Zod** for type-safe form validation
- **Recharts** for interactive cashflow and category spending charts

## Quick Start with Docker

```bash
cp .env.example .env
docker compose up --build
```

- Frontend: http://localhost:3000
- Backend API / Swagger: http://localhost:5000/swagger
- Demo Account (seeded in Development): `demo@expensetracker.dev` / `Demo@1234`

## Local Development

### Backend
```bash
cd backend
dotnet restore
dotnet test
dotnet run --project src/ExpenseTracker.Api
```

### Frontend
```bash
cd frontend
npm install
npm run dev
```
