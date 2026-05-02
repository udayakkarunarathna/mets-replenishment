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

### 2. Start the Blazor UI (separate terminal)

```bash
cd METS.Blazor
dotnet run
```

Blazor app runs on **http://localhost:5001**

<img width="1896" height="955" alt="image" src="https://github.com/user-attachments/assets/33154ae9-6ddc-43e2-8aaf-e7c5aca3b969" />



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
  <img width="725" height="548" alt="image" src="https://github.com/user-attachments/assets/7a3a2d56-8b3a-411b-adc7-a9228a587a19" />

- **Dashboard**: Live stats and recent requests at a glance
  <img width="1907" height="902" alt="image" src="https://github.com/user-attachments/assets/d1d8f19c-b709-4dd0-851b-67c0d6843f4a" />

- **Request list**: Filter by status, priority, and location; paginated table
  <img width="1903" height="826" alt="image" src="https://github.com/user-attachments/assets/9c6d3229-cdf8-45ea-8a5d-5a7e9b659f8a" />

- **Request detail**: Full workflow actions based on current user role
  - Workers: Submit, Edit (Draft only)
    <img width="1907" height="572" alt="image" src="https://github.com/user-attachments/assets/b4c2f940-5ca5-4ce2-b8e9-50124ba237af" />

  - Reviewers: Approve, Reject, Fulfill
    <img width="1893" height="643" alt="image" src="https://github.com/user-attachments/assets/f111d393-b38e-444b-ace9-1fa86a76152f" />

- **Validation banner**: Spins while stock check runs, updates automatically
  <img width="1887" height="657" alt="image" src="https://github.com/user-attachments/assets/fc8227fe-68e8-43cb-8226-8c1af6a00940" />

- **New Request form**: Multi-line-item form with dynamic add/remove rows
  <img width="1902" height="897" alt="image" src="https://github.com/user-attachments/assets/a0d5e763-6797-4ede-9afd-ac3639beb347" />

- **Dialogs**: Rejection reason dialog, fulfillment quantities dialog
  <img width="1907" height="650" alt="image" src="https://github.com/user-attachments/assets/a1bdf312-9c2e-43fc-aaa5-43b94c3172ab" />

  <img width="1902" height="627" alt="image" src="https://github.com/user-attachments/assets/d7c15c1c-664e-425a-9884-1b076732ef18" />


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
