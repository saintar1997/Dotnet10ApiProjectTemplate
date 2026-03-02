# EnterpriseWeb — Architecture Guide

## 1. Project Overview

EnterpriseWeb is a robust enterprise-grade backend system built to manage users, roles, and permissions securely while serving a modular front-end application. It is engineered for fast database access and secure operations using a modern .NET tech stack.

### Technology Stack

| Layer/Component | Technology Used |
|---|---|
| **Language** | C# 12+ (leveraging Primary Constructors & Records) |
| **Framework** | ASP.NET Core 10 Web API |
| **Database** | SQL Server |
| **ORM / Query Builder** | Dapper (Micro-ORM for raw SQL performance) |
| **Authentication** | JWT (JSON Web Tokens) with Policy-based Authorization |
| **Logging** | Serilog (routing to Console, File, and Seq) |
| **API Documentation** | OpenAPI integrated with Scalar API Reference |
| **Security/Resilience** | Partitioned Rate Limiting, CORS |

### Core Architectural Decisions

- **Clean Architecture:** The codebase is strictly divided into `Domain`, `Application`, `Infrastructure`, and `API` layers. The core logic does not depend on external frameworks.
- **Micro-ORM (Dapper) & Raw SQL:** To maximize performance and keep exact control over database queries, Dapper is used instead of Entity Framework.
- **Unit of Work Pattern:** Transactions are explicitly managed in the Application layer using an `IUnitOfWork` (calling `.Begin()`, `.Commit()`, and `.Rollback()`) to ensure atomicity across multiple repository operations.
- **Immutable Entities:** Domain models are implemented using C# `record` types with `init` properties. Updates are handled using `with` expressions.
- **Global Error Handling:** Minimal try-catch blocks in business logic. Exceptions bubble up to a centralized `ExceptionHandlingMiddleware` for consistent JSON error responses.

---

## 2. Solution Structure

```text
EnterpriseWeb.slnx
├── EnterpriseWeb.Domain/         (Core Business Models)
│   ├── Entities/                 (e.g., User.cs, Role.cs)
│   └── Interfaces/               (e.g., IUserRepository.cs, IUnitOfWork.cs)
├── EnterpriseWeb.Application/    (Use Cases & Business Logic)
│   ├── DTOs/                     (Data Transfer Objects per feature)
│   └── Services/                 (e.g., UserService.cs, AuthService.cs)
├── EnterpriseWeb.Infrastructure/ (External Dependencies & DB Access)
│   ├── Data/                     (e.g., DapperContext.cs)
│   └── Repositories/             (e.g., UserRepository.cs)
└── EnterpriseWeb.API/            (Presentation Layer)
    ├── Controllers/              (e.g., UsersController.cs)
    ├── Middleware/               (e.g., ExceptionHandlingMiddleware.cs)
    └── Program.cs                (Composition Root & App Setup)
```

### Dependency Rules

- **Domain** depends on NOTHING. It is the heart of the system.
- **Application** depends ONLY on **Domain**.
- **Infrastructure** depends ONLY on **Domain** and **Application**.
- **API** depends on **Application** and **Infrastructure** (only for DI registration).

> ⚠️ **Warning**: Never reference `Microsoft.AspNetCore.*` in the Domain or Application layers. Never write SQL queries outside of the Infrastructure layer.

---

## 3. Layer/Module Responsibilities

### Domain Layer (`EnterpriseWeb.Domain`)

- **Purpose**: Define the core state, business entities, and repository contracts.
- **Belongs Here**: `record` entities, Enums, custom Domain exceptions, Interface definitions for Repositories.
- **NEVER Here**: DTOs, SQL queries, HTTP Context, or third-party packages (outside of primitives).

```csharp
// Example: Domain Entity
public record User
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<Role> Roles { get; init; } = [];
}
```

### Application Layer (`EnterpriseWeb.Application`)

- **Purpose**: Implement business use cases, coordinate state changes, and map entities to DTOs.
- **Belongs Here**: Services (`UserService`), DTOs (`UserDto`), interfaces for services, validation logic.
- **NEVER Here**: SQL strings (`SELECT * FROM...`), HTTP status codes, or Controller bindings.

```csharp
// Example: Application Service Contract
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<Guid> CreateUserAsync(CreateUserDto dto, Guid? createdBy = null);
}
```

### Infrastructure Layer (`EnterpriseWeb.Infrastructure`)

- **Purpose**: Implement the technical details to talk to the database and external systems.
- **Belongs Here**: Dapper Repositories, SQL queries, Dapper Context, UnitOfWork implementation.
- **NEVER Here**: Business validation rules or HTTP Request/Response handling.

```csharp
// Example: Infrastructure Repository
public class UserRepository(DapperContext context) : IUserRepository
{
    public async Task DeleteAsync(Guid id, IDbConnection conn, IDbTransaction tx)
    {
        const string sql = "DELETE FROM [dbo].[Users] WHERE [Id] = @Id";
        await conn.ExecuteAsync(sql, new { Id = id }, tx);
    }
}
```

### API Layer (`EnterpriseWeb.API`)

- **Purpose**: Receive HTTP requests, enforce Authentication/Authorization, and return HTTP responses.
- **Belongs Here**: Controllers, Middlewares, Program.cs setup, OpenAPI configuration.
- **NEVER Here**: Business logic calculation, database context initialization, or manual transaction management.

```csharp
// Example: API Controller
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "users:view")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }
}
```

> **TL;DR**: Controllers route traffic, Services orchestrate logic, Repositories run SQL, Entities define structure.

---

## 4. SOLID Principles — How They Apply Here

### Single Responsibility Principle (SRP)

- **Explanation**: A class should do exactly one thing.
- **Enforcement**: By separating DTO mapping, DB querying, and HTTP parsing into distinct layers.
- **Good Example**: `ExceptionHandlingMiddleware.cs` — only catches exceptions and maps to JSON.
- **Bad Example**: Updating a user's roles and sending an email inside the `UsersController`.
- **Self-Test**: "Can I describe what this class does without using the word 'and'?"

### Open/Closed Principle (OCP)

- **Explanation**: Code should be extensible without modifying existing code.
- **Enforcement**: Using Policy-based authorization endpoints (`[Authorize(Policy = "users:create")]`). You can add new permissions without modifying the core login logic.
- **Self-Test**: "If I add a new feature, do I have to rewrite the core framework classes?"

### Liskov Substitution Principle (LSP)

- **Explanation**: Implementations must fulfill the contract of their interface without throwing unexpected `NotImplementedException`s.
- **Enforcement**: `UserRepository` reliably returns what `IUserRepository` promises.

### Interface Segregation Principle (ISP)

- **Explanation**: Don't force clients to depend on methods they don't use.
- **Good Example**: `IUserService` and `IRoleService` are separated. Controllers don't inject a massive "GodService".
- **Self-Test**: "Is the controller calling only 1 method on a service that exposes 50 methods?"

### Dependency Inversion Principle (DIP)

- **Explanation**: High-level modules don't depend on low-level modules; both depend on abstractions.
- **Enforcement**: `UserService` relies on `IUserRepository`, not `UserRepository`.
- **Good Example**:

```csharp
// UserService injects an interface instead of the concrete Dapper class.
public class UserService(IUserRepository userRepository, IUnitOfWork unitOfWork) 
{ ... }
```

> **TL;DR**: Isolate logic to interfaces to keep the codebase modular and testable.

---

## 5. Step-by-Step: Adding a New Feature

When adding a CRUD feature (e.g., `Product`), follow this exact order:

### Step 1. Create Domain Entity (`EnterpriseWeb.Domain/Entities/Product.cs`)

Use `record` and `init` properties for immutability.

```csharp
namespace EnterpriseWeb.Domain.Entities;

public record Product
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### Step 2. Create Repository Interface (`EnterpriseWeb.Domain/Interfaces/IProductRepository.cs`)

Define the DB operations, ensuring you pass the transaction for mutable operations.

```csharp
namespace EnterpriseWeb.Domain.Interfaces;
using System.Data;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(Product entity, IDbConnection conn, IDbTransaction tx);
}
```

### Step 3. Create DTOs (`EnterpriseWeb.Application/DTOs/Product/`)

Create immutable request/response DTOs.

```csharp
public record ProductDto(Guid Id, string Name, decimal Price);
public record CreateProductDto(string Name, decimal Price);
```

### Step 4. Create Repository Implementation (`EnterpriseWeb.Infrastructure/Repositories/ProductRepository.cs`)

Write the explicit SQL using Dapper.

```csharp
namespace EnterpriseWeb.Infrastructure.Repositories;
using Dapper;

public class ProductRepository(DapperContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM [dbo].[Products] WHERE [Id] = @Id";
        using var conn = context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(Product entity, IDbConnection conn, IDbTransaction tx)
    {
        const string sql = "INSERT INTO [dbo].[Products] ([Id], [Name]) VALUES (@Id, @Name)";
        await conn.ExecuteAsync(sql, new { entity.Id, entity.Name }, tx);
        return entity.Id;
    }
}
```

### Step 5. Create Application Service (`EnterpriseWeb.Application/Services/ProductService.cs`)

Orchestrate the Unit of Work.

```csharp
public class ProductService(IProductRepository repository, IUnitOfWork unitOfWork) : IProductService
{
    public async Task<Guid> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product { Id = Guid.NewGuid(), Name = dto.Name };
        
        unitOfWork.Begin();
        try
        {
            var id = await repository.CreateAsync(product, unitOfWork.Connection, unitOfWork.Transaction!);
            unitOfWork.Commit();
            return id;
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }
}
```

### Step 6. Create Controller (`EnterpriseWeb.API/Controllers/ProductsController.cs`)

Handle the HTTP routing and Policies.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "products:create")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var id = await productService.CreateProductAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}
```

> **TL;DR**: Domain -> Repo Interface -> DTOs -> Repo Implementation -> Service -> Controller.

---

## 6. Request Lifecycle — What Happens When You Hit an Endpoint

When a request arrives at `POST /api/users`:

1. **Rate Limiter:** Checks if the IP has exceeded 100 req/min (or 10/min for auth). If yes, returns HTTP `429 Too Many Requests`.
2. **ExceptionHandlingMiddleware:** Wraps the entire pipeline in a `try/catch`.
3. **Authentication:** The JWT Bearer token in the `Authorization` header is validated against the secret `Key`, `Issuer`, and `Expiration`.
4. **Authorization:** The `[Authorize(Policy = "users:create")]` attribute checks if the token contains the claim `permission: users:create`.
5. **Controller Routing:** Hits `UsersController.Create()`. Body is automatically mapped to `CreateUserDto`.
6. **Service Execution:** Calls `UserService.CreateUserAsync()`.
   - Hashes password.
   - Calls `unitOfWork.Begin()`.
   - Executes Dapper SQL INSERT.
   - If success, `unitOfWork.Commit()`. If SQL fails, `catch` block fires `unitOfWork.Rollback()` and re-throws.
7. **Response Shaping:** Controller returns `201 Created` with the new ID.
8. **Logging:** `SerilogRequestLogging` outputs the execution time and HTTP status to the console/Seq.

> **TL;DR**: Rate Limit -> Middleware -> JWT Auth -> Policy Auth -> Controller -> Service -> DB -> Response.

---

## 7. Error Handling & Response Pattern

### The Core Pattern

Instead of wrapping every controller action in a `try/catch`, this project relies on global middleware to swallow unhandled exceptions. Standard C# exceptions are mapped directly to HTTP boundaries.

### How to trigger errors

In your Application Service, simply throw standard exceptions if business validation explicitly fails:

```csharp
if (user is null) throw new KeyNotFoundException("User not found.");
if (!hasAccess) throw new UnauthorizedAccessException();
```

### Centralized Middleware Anatomy (`ExceptionHandlingMiddleware.cs`)

```csharp
catch (KeyNotFoundException ex)
{
    await WriteErrorResponse(context, HttpStatusCode.NotFound, "Resource not found.");
}
catch (UnauthorizedAccessException ex)
{
    await WriteErrorResponse(context, HttpStatusCode.Forbidden, "Access denied.");
}
```

### HTTP Responses

Always use strict ASP.NET Core `IActionResult` methods:

- `Ok(data)` for 200 GET
- `CreatedAtAction(...)` for 201 POST
- `NoContent()` for 204 PUT/DELETE
- `NotFound(new { message = "..." })` when IDs are invalid.
- `BadRequest(new { message = "..." })` when payload logic fails (e.g. ID mismatch).

> **TL;DR**: Throw exceptions for catastrophic failures; return `BadRequest` or `NotFound` from controllers for user input errors.

---

## 8. Naming Conventions

| Item | Case Style | Example | Rule |
|---|---|---|---|
| **Classes / Records** | PascalCase | `UserService`, `User` | Nouns. Always singular for records. |
| **Interfaces** | PascalCase | `IUserService` | Always prefixed with `I`. |
| **Methods** | PascalCase | `GetUserByIdAsync` | Always suffixed with `Async` if returning a `Task`. |
| **Variables / Params** | camelCase | `existingUser`, `userId` | Clear, descriptive nouns. |
| **DTOs** | PascalCase | `CreateUserDto` | Suffixed with `Dto`. Records prefered. |
| **SQL Tables** | TitleCase plural | `[dbo].[Users]` | Wrapped in brackets, pluralized. |
| **Service Injections** | camelCase | `userService` | Injected via primary constructor. |

> **TL;DR**: PascalCase for types and methods, camelCase for variables, prefix interfaces with 'I'.

---

## 9. Dependency Management & Registration

Dependencies are injected using Microsoft's built-in Dependency Injection via constructor injection (using C# 12 Primary Constructors).
Registrations are organized by layer via Extension Methods.

### How to Register a New Component

When you create a new Service or Repository, you **MUST** register it in its corresponding extension class layer.

**For Services** (in `EnterpriseWeb.Application/DependencyInjection.cs` or similar):

```csharp
services.AddScoped<IProductService, ProductService>();
```

**For Repositories** (in `EnterpriseWeb.Infrastructure/DependencyInjection.cs` or similar):

```csharp
services.AddScoped<IProductRepository, ProductRepository>();
```

### Scope Rules

- **AddScoped**: Use for almost everything (Services, Repositories). A new instance is created per HTTP request.
- **AddTransient**: Use only for lightweight classes with no state and no shared resources. A new instance is created every time one is injected.
- **AddSingleton**: A single instance is created for the entire lifetime of the app. Only use this for completely stateless classes that are guaranteed to be thread-safe — for example, a helper that takes an input and immediately returns a value. Never use for DB connections or anything that holds mutable state!

> **TL;DR**: Use `AddScoped` for Services/Repos and register them in layer-specific installer files.

---

## 10. Common Mistakes & How to Avoid Them

| # | Mistake | Why It's Wrong | Correct Approach |
|---|---|---|---|
| **1** | Writing SQL in Controllers | Breaks layer separation. UI shouldn't know about DB layout. | Put SQL entirely in `Infrastructure` Repositories. |
| **2** | Forgetting `unitOfWork.Commit()` | The transaction will rollback at the end of the request. Data won't save. | Always `Begin()`, `Commit()`, and wrap rollback in a `catch`. |
| **3** | Leaking Domain Entities to UI | Reveals DB structure and passwords to users. | Map Entities to `Dto` objects in the Application layer before returning. |
| **4** | Using standard `new SqlConnection()` everywhere | Circumvents the established context flow and leaks connections. | Inject `DapperContext`, use `context.CreateConnection()`. |
| **5** | Hardcoding Authentication logic | Hard to test and reuse. | Use `[Authorize(Policy = "...")]` on Controllers. |
| **6** | Not using `async/await` for DB calls | Blocks threads under high load. | Always use `ExecuteAsync`/`QueryAsync` and `Task`. |
| **7** | Modifying `record` properties directly | `Records` are designed to be immutable with `init`. | Use the C# `with` keyword: `existingUser with { Email = "new" }`. |
| **8** | Returning 200 OK after Creation | Violates REST standards. | Return `201 CreatedAtAction(...)`. |
| **9** | Forgetting to add `.slnx` to Solution | Files won't open in Visual Studio cleanly. | Ensure files belong to solution tree. |
| **10** | Placing AppSettings paths in methods | Hardcoded paths crash in Docker/Prod. | Use `IConfiguration` or `IOptions<>`. |

> **TL;DR**: Avoid shortcuts. Keep Controller dumb, Services smart, Repositories strict.

---

## 11. Quick Reference Card

### ✅ New Feature Checklist

1. Create `Entity` (record) in Domain.
2. Create `IRepository` in Domain.
3. Create `Create/Update/Response DTOs` in Application.
4. Create `Repository` (Dapper + Transactions) in Infrastructure.
5. Create `Service` interface & implementation in Application.
6. Register Service and Repo in DI container.
7. Create `Controller` mapping HTTP routes to Service.

### 📝 File Naming Cheat Sheet

- Entity: `Feature.cs`
- Service Interface: `IFeatureService.cs`
- Service Code: `FeatureService.cs`
- DTO: `CreateFeatureDto.cs`
- Controller: `FeaturesController.cs` (Plural)

### 🚀 Standard HTTP Responses

- **GET -> 200 OK**: `return Ok(dto);`
- **GET (not found) -> 404 Not Found**: `return NotFound(new { message = "..." });`
- **POST -> 201 Created**: `return CreatedAtAction(nameof(GetById), new { id }, new { id });`
- **PUT -> 204 No Content**: `return NoContent();`
- **DELETE -> 204 No Content**: `return NoContent();`
- **Validation Fail -> 400 Bad Request**: `return BadRequest(new { message = "..." });`
- **Unauthorized (not logged in) -> 401 Unauthorized**: `return Unauthorized(new { message = "..." });`
- **Forbidden (no permission) -> 403 Forbidden**: `return StatusCode(403, new { message = "..." });`
- **Conflict (duplicate data) -> 409 Conflict**: `return Conflict(new { message = "..." });`
- **Too Many Requests -> 429 Too Many Requests**: `return StatusCode(429, new { message = "..." });`
- **Server Error -> 500 Internal Server Error**: (handled by Middleware automatically)
