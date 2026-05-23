# Task Management API

A production-ready **Project & Task Management REST API** built with **.NET 9**, Clean Architecture, CQRS, MediatR, JWT Authentication, Redis caching, and Docker support.

---

## Architecture Overview

```
TaskManagement/
├── src/
│   ├── TaskManagement.Domain/          # Entities, Enums, Interfaces (no dependencies)
│   ├── TaskManagement.Application/     # CQRS, MediatR, DTOs, Validators, Mappings
│   ├── TaskManagement.Infrastructure/  # EF Core, Repositories, JWT, BCrypt, Redis
│   └── TaskManagement.API/             # Controllers, Middleware, Swagger, DI wiring
└── tests/
    └── TaskManagement.UnitTests/       # xUnit, Moq, FluentAssertions
```

### Layer Responsibilities

| Layer | Responsibility |
|---|---|
| **Domain** | Pure business entities & repository contracts. No framework dependencies. |
| **Application** | Use cases via CQRS (Commands/Queries). MediatR pipeline with validation. |
| **Infrastructure** | EF Core + SQL Server, JWT token generation, BCrypt passwords, Redis cache. |
| **API** | HTTP layer. Controllers, global exception middleware, versioning, Swagger. |

---

## Tech Stack

| Tech | Version | Purpose |
|---|---|---|
| .NET | 9.0 | Runtime |
| ASP.NET Core | 9.0 | Web API framework |
| Entity Framework Core | 9.0 | ORM + migrations |
| SQL Server | 2022 | Relational database |
| MediatR | 12.x | CQRS mediator |
| FluentValidation | 11.x | Input validation |
| AutoMapper | 13.x | Object mapping |
| BCrypt.Net | 4.x | Password hashing |
| JWT Bearer | 9.0 | Authentication |
| Redis (StackExchange) | 2.x | Distributed cache |
| Asp.Versioning | 8.x | API versioning |
| Swashbuckle | 7.x | Swagger/OpenAPI |
| xUnit + Moq | latest | Unit testing |

---

## Getting Started

### Option 1 — Docker (Recommended)

**Prerequisites:** Docker Desktop installed and running.

```bash
# 1. Clone the repository
git clone https://github.com/YOUR_USERNAME/TaskManagementAPI.git
cd TaskManagementAPI

# 2. Start all services (API + SQL Server + Redis)
docker-compose up --build -d

# 3. API is available at:
#    http://localhost:5000
#    Swagger UI: http://localhost:5000/index.html
```

### Option 2 — Local Setup

**Prerequisites:** .NET 9 SDK, SQL Server (local or Docker)

```bash
# 1. Clone the repository
git clone https://github.com/YOUR_USERNAME/TaskManagementAPI.git
cd TaskManagementAPI

# 2. Configure the connection string in:
#    src/TaskManagement.API/appsettings.Development.json

# 3. Apply database migrations
cd src/TaskManagement.API
dotnet ef database update --project ../TaskManagement.Infrastructure

# 4. Run the API
dotnet run

# Swagger UI: https://localhost:7xxx  or  http://localhost:5xxx
```

---

## Environment Configuration

Edit `appsettings.json` or set environment variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskManagementDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;",
    "Redis": ""
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!2024",
    "Issuer": "TaskManagementAPI",
    "Audience": "TaskManagementClients"
  }
}
```

> **Note:** Leave `Redis` connection string empty to disable caching (no-op fallback is used automatically).

---

## API Endpoints

Base URL: `http://localhost:5000/api/v1`

### Authentication

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/auth/register` | Register new user | No |
| POST | `/auth/login` | Login & get JWT token | No |

### Projects

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/projects` | Get all user projects | JWT |
| GET | `/projects/{id}` | Get project with tasks | JWT |
| POST | `/projects` | Create project | JWT |
| PUT | `/projects/{id}` | Update project | JWT |
| DELETE | `/projects/{id}` | Delete project + tasks | JWT |

### Tasks

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/projects/{projectId}/tasks` | Get tasks by project | JWT |
| POST | `/projects/{projectId}/tasks` | Create task | JWT |
| PUT | `/tasks/{taskId}` | Full update task | JWT |
| PATCH | `/tasks/{taskId}/status` | Update task status only | JWT |
| DELETE | `/tasks/{taskId}` | Delete task | JWT |

---

## Request & Response Examples

### Register
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "fullName": "Ahmed Ali",
  "email": "ahmed@example.com",
  "password": "Password123!"
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

### Create Project
```http
POST /api/v1/projects
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "My Project",
  "description": "Project description"
}
```

### Create Task
```http
POST /api/v1/projects/{projectId}/tasks
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Implement feature",
  "description": "Task description",
  "dueDate": "2025-12-31T00:00:00Z",
  "priority": "High"
}
```

Priority values: `Low`, `Medium`, `High`, `Critical`

### Update Task Status
```http
PATCH /api/v1/tasks/{taskId}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "InProgress"
}
```

Status values: `Todo`, `InProgress`, `Done`

---

## Error Response Format

All errors follow the same structure:

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Email is required.",
    "Password must be at least 6 characters."
  ]
}
```

| HTTP Code | Meaning |
|---|---|
| 200 | Success |
| 201 | Created |
| 400 | Validation / Bad request |
| 401 | Unauthorized (no/invalid token) |
| 403 | Forbidden (not your resource) |
| 404 | Not found |
| 409 | Conflict (duplicate email) |
| 500 | Internal server error |

---

## Running Tests

```bash
cd tests/TaskManagement.UnitTests
dotnet test
```

---

## Database Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API

# Apply migrations
dotnet ef database update \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API

# Rollback
dotnet ef database update PreviousMigrationName \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API
```

---

## Design Decisions

**Clean Architecture** — Dependencies point inward only. Domain has zero framework dependencies.

**CQRS with MediatR** — Commands (writes) and Queries (reads) are fully separated. Each handler is a single-responsibility class.

**Generic Response Wrapper** — Every endpoint returns `ApiResponse<T>` ensuring consistent structure for clients.

**Global Exception Middleware** — All exceptions are caught centrally and mapped to appropriate HTTP status codes without try-catch in controllers.

**Repository + Unit of Work** — Abstracts EF Core from Application layer, enabling easy testing via mocking.

**Redis Caching** — Project lists are cached per user. Cache is invalidated on create/update/delete. Falls back to no-op when Redis is unavailable.

**BCrypt Password Hashing** — Work factor 12, industry standard.

**Role-Based Authorization** — Users have `User` or `Admin` roles stored in JWT claims, ready for `[Authorize(Roles = "Admin")]`.

**API Versioning** — URL segment versioning (`/api/v1/...`) with header fallback (`X-Api-Version`).
