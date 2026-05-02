# METS — Stock Replenishment Request System

A full-stack feature built on **ASP.NET Core 10 + Blazor Server + MudBlazor**, implementing a factory stock replenishment workflow with async stock validation, role-based UI, and a clean REST API.

---

## 🏗️ Architecture

```
METS/
├── METS.Api/                     # ASP.NET Core Web API
│   ├── Controllers/              # REST endpoints
│   ├── Models/                   # EF Core entities + enums
│   ├── DTOs/                     # Request/Response shapes + mappers
│   ├── Services/                 # Business logic (ReplenishmentService)
│   ├── BackgroundServices/       # Async stock validation worker (Channel<T>)
│   └── Data/                     # DbContext + seed data
│
├── METS.Blazor/                  # Blazor Server UI
│   ├── Pages/                    # Routable pages
│   ├── Shared/                   # Reusable components + dialogs
│   ├── Services/                 # API client + UserSession
│   └── Models/                   # Client-side DTOs + form models
│
└── METS.Tests/                   # NUnit + NSubstitute unit tests
```

---

## 🚀 Running Locally

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Start the API

```bash
cd METS.Api
dotnet run
```

API runs on **http://localhost:5000**  
Swagger UI: **http://localhost:5000/swagger**

### 2. Start the Blazor UI (separate terminal)

```bash
cd METS.Blazor
dotnet run
```

Blazor app runs on **http://localhost:5001**

### Run tests

```bash
cd METS.Tests
dotnet test
```

---

## 🔄 Request Lifecycle

```
Draft ──► Submitted ──► Approved ──► Fulfilled
               │
               └──► Rejected
```

| Status    | Who can act        | What happens                                       |
|-----------|--------------------|----------------------------------------------------|
| Draft     | Worker (creator)   | Edit, submit                                       |
| Submitted | Background worker  | Async stock check runs (3–8s), result stored in DB |
| Submitted | Reviewer           | Approve or reject (with reason)                    |
| Approved  | Reviewer           | Mark fulfilled with actual quantities              |

---

## ⚡ Async Stock Validation Design

The external stock check is deliberately slow (simulated with 3–8 second delay).

**Solution: Non-blocking background processing**

1. `POST /api/requests/{id}/submit` returns **`202 Accepted`** immediately
2. A `StockValidationWorkItem` is written to a `System.Threading.Channels.Channel<T>`
3. `StockValidationWorker` (a `BackgroundService`) reads from the channel and runs the check
4. Result is persisted to the DB (`ValidationStatus` + `ValidationMessage`)
5. Blazor UI **polls** `GET /api/requests/{id}/validation` every 2.5 seconds until resolved
6. UI updates reactively via `StateHasChanged()`

This means the API is never blocked, the user sees an immediate response, and the validation result appears automatically when ready.

---

## 🌐 REST API Endpoints

| Method | Endpoint                          | Description                          |
|--------|-----------------------------------|--------------------------------------|
| GET    | `/api/requests`                   | List + filter + paginate             |
| GET    | `/api/requests/{id}`              | Get request detail                   |
| GET    | `/api/requests/{id}/validation`   | Poll validation status               |
| POST   | `/api/requests`                   | Create draft                         |
| PUT    | `/api/requests/{id}`              | Update draft                         |
| POST   | `/api/requests/{id}/submit`       | Submit → triggers async stock check  |
| POST   | `/api/requests/{id}/approve`      | Approve (reviewer only)              |
| POST   | `/api/requests/{id}/reject`       | Reject with reason (reviewer only)   |
| POST   | `/api/requests/{id}/fulfill`      | Mark fulfilled with quantities       |
| GET    | `/api/locations`                  | List stock locations                 |
| GET    | `/api/users`                      | List users (filterable by role)      |

### Filter parameters for `GET /api/requests`

| Param      | Values                                    |
|------------|-------------------------------------------|
| `status`   | Draft, Submitted, Approved, Rejected, Fulfilled |
| `priority` | Low, Normal, Urgent                       |
| `locationId` | integer                                 |
| `page`     | integer (default: 1)                      |
| `pageSize` | integer (default: 20)                     |

---

## 🖥️ UI Features

- **Role picker**: Select a Worker or Reviewer identity on startup (no auth required)
- **Dashboard**: Live stats and recent requests at a glance
- **Request list**: Filter by status, priority, and location; paginated table
- **Request detail**: Full workflow actions based on current user role
  - Workers: Submit, Edit (Draft only)
  - Reviewers: Approve, Reject, Fulfill
- **Validation banner**: Spins while stock check runs, updates automatically
- **New Request form**: Multi-line-item form with dynamic add/remove rows
- **Dialogs**: Rejection reason dialog, fulfillment quantities dialog

---

## 🗄️ Database

Uses **SQLite in-memory** (shared connection, persists for the application lifetime).

Seed data includes:
- 3 Workers, 2 Reviewers
- 5 Stock Locations (Assembly, Welding, Packaging)
- 7 Requests covering all statuses (Draft, Submitted×2, Approved, Rejected, Fulfilled, Urgent Draft)

---

## 🧪 Tests

Unit tests cover `ReplenishmentService` business logic:
- Create request (valid, multiple items)
- Submit (success, already submitted, not found, queue assertion)
- Approve (success, by worker = rejected, wrong status)
- Reject (success with reason)
- Fulfill (success, wrong status)
- Filter by status
- Pagination

Run with: `dotnet test`

---

## 🏷️ Tech Stack

| Layer      | Technology                         |
|------------|------------------------------------|
| Backend    | ASP.NET Core 10, Controller-based  |
| Data       | EF Core 9 + SQLite in-memory       |
| Async      | System.Threading.Channels + IHostedService |
| Frontend   | Blazor Server + MudBlazor 8        |
| Testing    | NUnit 4 + NSubstitute              |
