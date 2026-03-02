# EnterpriseWeb — Backend API

## Tech Stack
| Layer | Technology |
|---|---|
| Backend API | .NET 10, ASP.NET Core Web API |
| ORM | Dapper (micro-ORM) |
| Database | SQL Server 2022 (with Temporal Tables) |
| Auth | JWT Bearer + Policy-based Authorization |
| Password | BCrypt.Net-Next |

---

## Project Structure

```
EnterpriseProject/
├── SQL_Script/
│   ├── 001_CreateDatabase.sql     # DB creation + RCSI + QueryStore
│   ├── 002_CreateTables.sql       # Tables, Temporal Tables, Indexes
│   └── 003_SeedData.sql           # Seed data (admin/staff users, roles, menus)
│
└── backend/
    ├── EnterpriseWeb.slnx
    ├── EnterpriseWeb.Domain/       # Entities (record), Interfaces
    ├── EnterpriseWeb.Application/  # DTOs, Service interfaces, AuthService, MenuService, UserService
    ├── EnterpriseWeb.Infrastructure/ # DapperContext, UserRepository, MenuRepository, BcryptPasswordHasher
    └── EnterpriseWeb.API/          # Controllers, Program.cs, appsettings.json
```

---

## Getting Started

### 1. Database
Run the SQL scripts in order against your SQL Server instance:
```sql
-- 1. Create database
-- 001_CreateDatabase.sql

-- 2. Create tables + temporal tables
-- 002_CreateTables.sql

-- 3. Seed mock data
-- 003_SeedData.sql
```

> **Note:** Update the `hashed_password_here` placeholder in `003_SeedData.sql` with a real BCrypt hash, or use the API's `POST /api/users` endpoint to create users programmatically.

### 2. Backend API
```powershell
# Update appsettings.json connection string if needed
# backend/EnterpriseWeb.API/appsettings.json

cd backend
dotnet run --project EnterpriseWeb.API
# API runs on https://localhost:5001 / http://localhost:5000
```

**⚠️ Important:** Change the `Jwt:Key` in `appsettings.json` to a strong secret before production!

---

## Quick Reference

| Item | Command / Value |
|---|---|
| API URL | `https://localhost:5001` |
| Default Admin | `admin_user` / (set password via register) |
| Default Staff | `staff_01` / (set password via register) |

---

## API Endpoints

| Method | Route | Auth | Permission |
|---|---|---|---|
| `POST` | `/api/auth/login` | ❌ Public | — |
| `GET` | `/api/menus` | ✅ JWT | Returns menus filtered by user permissions |
| `GET` | `/api/menus/all` | ✅ JWT | `System Admin` role only |
| `GET` | `/api/users` | ✅ JWT | `users:view` |
| `GET` | `/api/users/{id}` | ✅ JWT | `users:view` |
| `POST` | `/api/users` | ✅ JWT | `users:create` |

### Login Example
```json
POST /api/auth/login
{
  "username": "admin_user",
  "password": "your_plain_password"
}
```

Response includes a JWT with `permission` claims (e.g. `users:view`, `users:create`, `dashboard:view`) used for policy-based authorization.

---

## Notes for Production

1. **JWT Key** — Replace `Jwt:Key` in `appsettings.json` with a secret managed via environment variables or Azure Key Vault.
2. **Connection String** — Use environment variable `ConnectionStrings__DefaultConnection` to override.
3. **Password Seeding** — Run `dotnet user-secrets` or a seeder script to create the initial admin user with a properly hashed password.
