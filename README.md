# Task Management API

> RESTful API for managing projects and tasks — built with **.NET 9** and **Clean Architecture**.

---

## Table of Contents

1. [Project Structure](#1-project-structure)
2. [Tech Stack](#2-tech-stack)
3. [Getting Started](#3-getting-started)
4. [API Endpoints](#4-api-endpoints)
5. [Request & Response Examples](#5-request--response-examples)
6. [Error Response Format](#6-error-response-format)
7. [Running the Tests](#7-running-the-tests)
8. [Database Migrations](#8-database-migrations)
9. [Design Decisions](#9-design-decisions)

---

## 1. Project Structure

```
TaskManagement/
├── src/
│   ├── TaskManagement.Domain/          — Entities, enums, repository interfaces (no framework deps)
│   ├── TaskManagement.Application/     — Use cases (CQRS), DTOs, validators, AutoMapper profiles
│   ├── TaskManagement.Infrastructure/  — EF Core, SQL Server, JWT, BCrypt, Redis
│   └── TaskManagement.API/             — Controllers, middleware, Swagger, dependency injection
└── tests/
    └── TaskManagement.UnitTests/       — Unit tests for commands, queries, and handlers
```

**Dependency rule (strictly enforced):**

| Layer          | Depends On                                 |
| -------------- | ------------------------------------------ |
| Domain         | Nothing — no framework or external library |
| Application    | Domain only                                |
| Infrastructure | Implements interfaces defined in Domain    |
| API            | Wires everything via dependency injection  |

---

## 2. Tech Stack

| Technology                     | Version / Notes                            |
| ------------------------------ | ------------------------------------------ |
| .NET / ASP.NET Core            | 9                                          |
| Entity Framework Core          | 9 — code-first, SQL Server                 |
| MediatR                        | 12 — CQRS, one handler per use case        |
| FluentValidation               | Request validation in the MediatR pipeline |
| AutoMapper                     | Domain → DTO mapping                       |
| JWT Bearer                     | Stateless auth, 7-day token expiry         |
| BCrypt.Net                     | Password hashing — work factor 12          |
| StackExchange.Redis            | Optional distributed cache, no-op fallback |
| Asp.Versioning                 | 8 — URL segment versioning (`/api/v1/`)    |
| xUnit + Moq + FluentAssertions | Unit testing                               |

---

## 3. Getting Started

### 3.1 Prerequisites

- .NET 9 SDK
- SQL Server (local instance)

### 3.2 Run the API

```bash
git clone https://github.com/Mina-Salah/TaskManagement
cd TaskManagementAPI
dotnet run --project src/TaskManagement.API
```

> **Note:** Migrations run automatically on startup — no manual `dotnet ef` needed.
> Once running, Swagger opens at the root URL (e.g. `https://localhost:7241`). The exact port appears in the terminal.

### 3.3 Configuration

Open `src/TaskManagement.API/appsettings.json` and update the following:

#### Database

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"
}
```

> For SQL auth: `Server=localhost;Database=TaskManagementDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;`

#### Redis (optional)

```json
"Redis": ""
```

> Leave empty to disable — a no-op fallback is used automatically.
> To enable: `"Redis": "localhost:6379"`

#### JWT

```json
"Jwt": {
  "Key":      "YourSuperSecretKeyThatIsAtLeast32CharactersLong!2024",
  "Issuer":   "TaskManagementAPI",
  "Audience": "TaskManagementClients"
}
```

---

## 4. API Endpoints

**Base URL:** `/api/v1`

### 4.1 Authentication — no token required

| Method | Endpoint         | Description                   |
| ------ | ---------------- | ----------------------------- |
| `POST` | `/auth/register` | Create a new account          |
| `POST` | `/auth/login`    | Login and receive a JWT token |

### 4.2 Projects — JWT required

| Method   | Endpoint         | Description                            |
| -------- | ---------------- | -------------------------------------- |
| `GET`    | `/projects`      | List all projects for the current user |
| `GET`    | `/projects/{id}` | Get a project with its full task list  |
| `POST`   | `/projects`      | Create a new project                   |
| `PUT`    | `/projects/{id}` | Update project name and description    |
| `DELETE` | `/projects/{id}` | Delete project and all its tasks       |

### 4.3 Tasks — JWT required

| Method   | Endpoint                      | Description                                              |
| -------- | ----------------------------- | -------------------------------------------------------- |
| `GET`    | `/projects/{projectId}/tasks` | Get all tasks for a project, sorted by priority          |
| `POST`   | `/projects/{projectId}/tasks` | Create a task under the project                          |
| `PUT`    | `/tasks/{taskId}`             | Full update — title, description, status, date, priority |
| `PATCH`  | `/tasks/{taskId}/status`      | Update task status only                                  |
| `DELETE` | `/tasks/{taskId}`             | Delete a task                                            |

---

## 5. Request & Response Examples

### 5.1 Register

```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "fullName": "Ahmed Ali",
  "email":    "ahmed@example.com",
  "password": "Pass123!"
}
```

**Response:**

```json
{
  "success": true,
  "message": "Registration successful.",
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fullName": "Ahmed Ali",
    "email": "ahmed@example.com",
    "token": "eyJhbGci...",
    "role": "User"
  }
}
```

### 5.2 Create a Project

```http
POST /api/v1/projects
Authorization: Bearer {token}
Content-Type: application/json

{
  "name":        "Backend Rewrite",
  "description": "Migrating to clean architecture"
}
```

### 5.3 Create a Task

```http
POST /api/v1/projects/{projectId}/tasks
Authorization: Bearer {token}
Content-Type: application/json

{
  "title":       "Set up EF Core migrations",
  "description": "Configure DbContext and run the initial migration",
  "priority":    "High",
  "dueDate":     "2026-06-01T00:00:00Z"
}
```

> **Priority values:** `Low` · `Medium` · `High` · `Critical`

### 5.4 Update Task Status

```http
PATCH /api/v1/tasks/{taskId}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "InProgress"
}
```

> **Status values:** `Todo` · `InProgress` · `Done`

---

## 6. Error Response Format

All responses — success or failure — share the same envelope:

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": ["Title is required.", "DueDate must be in the future."]
}
```

**HTTP status codes:**

| Code  | Meaning                                        |
| ----- | ---------------------------------------------- |
| `200` | Success                                        |
| `201` | Resource created                               |
| `400` | Validation error or bad input                  |
| `401` | Missing or invalid JWT token                   |
| `403` | Token valid — resource belongs to another user |
| `404` | Resource not found                             |
| `409` | Conflict — email already registered            |
| `500` | Unexpected server error                        |

---

## 7. Running the Tests

```bash
dotnet test tests/TaskManagement.UnitTests
```

The suite contains **22 tests** across three classes:

#### AuthCommandsTests

- Register with unique email → success
- Register with duplicate email → `409`
- Login with valid credentials → token returned
- Login with unknown email → `400`
- Login with wrong password → `400`

#### ProjectCommandsTests

- Create a project → returns created project
- Delete non-existent project → `404`
- Delete project owned by another user → `403`
- Delete own project → success

#### TaskCommandsTests

- Create task on existing project → success
- Create task on project not owned → `404`
- Update task → returns updated task
- Update non-existent task → `404`
- Update task on project not owned → `403`
- Update task status → reflects new status
- Update status on non-existent task → `404`
- Update status on task in project not owned → `403`
- Delete task → success
- Delete non-existent task → `404`
- Delete task in project not owned → `403`
- Get tasks by project → returns task list
- Get tasks for project not owned → `404`

---

## 8. Database Migrations

Migrations run automatically on startup. For manual control:

#### Add a migration

```bash
dotnet ef migrations add MigrationName \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API
```

#### Apply pending migrations

```bash
dotnet ef database update \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API
```

#### Rollback to a previous migration

```bash
dotnet ef database update PreviousMigrationName \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API
```

---

## 9. Design Decisions

### 9.1 CQRS with MediatR

Reads and writes are completely separated. Each command or query has exactly one handler, making every use case easy to locate, test, and change without touching unrelated code.

### 9.2 Ownership Enforcement

Every project/task operation verifies the resource belongs to the current user. A valid JWT alone is not enough — accessing another user's resource returns `403`, not `404`, to avoid leaking resource existence.

### 9.3 Specific Repositories

EF Core is abstracted behind dedicated repository interfaces (`IUserRepository`, `IProjectRepository`, `ITaskRepository`) defined in the Domain layer. Each repository exposes only the operations relevant to its aggregate, keeping contracts focused and avoiding the over-generalization of a single generic interface. The Application layer never imports EF Core directly, keeping domain logic clean and unit testing straightforward with Moq.

### 9.4 Global Exception Middleware

No `try-catch` blocks in controllers. `ExceptionHandlingMiddleware` catches all exceptions and maps them to the correct HTTP status code with a consistent response body.

### 9.5 Optional Redis Caching

`ICacheService` has two implementations: `RedisCacheService` and `NoOpCacheService`. If the Redis connection string is empty, the no-op version is registered automatically — the rest of the codebase never knows which is active.

### 9.6 Consistent Response Wrapper

Every endpoint returns `ApiResponse<T>`. Clients always receive the same JSON structure regardless of whether the call succeeded or failed.
