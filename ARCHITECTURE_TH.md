# EnterpriseWeb — คู่มือสถาปัตยกรรมระบบ

## 1. ภาพรวมของโปรเจกต์

EnterpriseWeb คือระบบ Backend ระดับองค์กรที่แข็งแกร่ง ออกแบบมาเพื่อจัดการผู้ใช้ บทบาท และสิทธิ์การเข้าถึงอย่างปลอดภัย พร้อมให้บริการแอปพลิเคชัน Front-end แบบโมดูลาร์ ระบบนี้ถูกสร้างขึ้นเพื่อการเข้าถึงฐานข้อมูลที่รวดเร็วและปลอดภัยด้วย .NET stack ยุคใหม่

### เทคโนโลยีที่ใช้

| ชั้น/ส่วนประกอบ | เทคโนโลยีที่ใช้ |
|---|---|
| **ภาษา** | C# 12+ (ใช้ Primary Constructors & Records) |
| **Framework** | ASP.NET Core 10 Web API |
| **ฐานข้อมูล** | SQL Server |
| **ORM / Query Builder** | Dapper (Micro-ORM สำหรับประสิทธิภาพ Raw SQL) |
| **การยืนยันตัวตน** | JWT (JSON Web Tokens) พร้อม Policy-based Authorization |
| **Logging** | Serilog (ส่งออกไปยัง Console, File, และ Seq) |
| **เอกสาร API** | OpenAPI รวมกับ Scalar API Reference |
| **ความปลอดภัย/ความทนทาน** | Partitioned Rate Limiting, CORS |

### การตัดสินใจด้านสถาปัตยกรรมหลัก

- **Clean Architecture:** โค้ดแบ่งอย่างเข้มงวดเป็น 4 ชั้น ได้แก่ `Domain`, `Application`, `Infrastructure`, และ `API` โดยตรรกะหลักไม่ขึ้นอยู่กับ Framework ภายนอก
- **Micro-ORM (Dapper) & Raw SQL:** เพื่อประสิทธิภาพสูงสุดและการควบคุม Query ฐานข้อมูลอย่างแม่นยำ จึงใช้ Dapper แทน Entity Framework
- **Unit of Work Pattern:** การจัดการ Transaction อย่างชัดเจนในชั้น Application ผ่าน `IUnitOfWork` (เรียกใช้ `.Begin()`, `.Commit()`, และ `.Rollback()`) เพื่อรับประกันความถูกต้องครบถ้วนของข้อมูลในทุก Repository Operation
- **Immutable Entities:** Domain Models ใช้ C# `record` types พร้อม `init` properties การอัปเดตจัดการด้วย `with` expressions
- **Global Error Handling:** ใช้ try-catch น้อยที่สุดใน Business Logic โดย Exception จะส่งต่อไปที่ `ExceptionHandlingMiddleware` ส่วนกลาง เพื่อตอบกลับ JSON Error อย่างสม่ำเสมอ

---

## 2. โครงสร้าง Solution

```text
EnterpriseWeb.slnx
├── EnterpriseWeb.Domain/         (Business Models หลัก)
│   ├── Entities/                 (เช่น User.cs, Role.cs)
│   └── Interfaces/               (เช่น IUserRepository.cs, IUnitOfWork.cs)
├── EnterpriseWeb.Application/    (Use Cases & Business Logic)
│   ├── DTOs/                     (Data Transfer Objects แยกตาม Feature)
│   └── Services/                 (เช่น UserService.cs, AuthService.cs)
├── EnterpriseWeb.Infrastructure/ (External Dependencies & DB Access)
│   ├── Data/                     (เช่น DapperContext.cs)
│   └── Repositories/             (เช่น UserRepository.cs)
└── EnterpriseWeb.API/            (Presentation Layer)
    ├── Controllers/              (เช่น UsersController.cs)
    ├── Middleware/               (เช่น ExceptionHandlingMiddleware.cs)
    └── Program.cs                (Composition Root & การตั้งค่า App)
```

### กฎการพึ่งพา (Dependency Rules)

- **Domain** ไม่ขึ้นอยู่กับอะไรทั้งสิ้น — มันคือหัวใจของระบบ
- **Application** ขึ้นอยู่กับ **Domain** เท่านั้น
- **Infrastructure** ขึ้นอยู่กับ **Domain** และ **Application** เท่านั้น
- **API** ขึ้นอยู่กับ **Application** และ **Infrastructure** (เฉพาะการลงทะเบียน DI)

> ⚠️ **คำเตือน**: ห้ามอ้างอิง `Microsoft.AspNetCore.*` ใน Domain หรือ Application layers เด็ดขาด และห้ามเขียน SQL Queries นอก Infrastructure layer

---

## 3. ความรับผิดชอบของแต่ละชั้น

### Domain Layer (`EnterpriseWeb.Domain`)

- **จุดประสงค์**: กำหนด State หลัก, Business Entities, และ Repository Contracts
- **สิ่งที่อยู่ที่นี่**: `record` entities, Enums, Domain Exceptions แบบกำหนดเอง, นิยาม Interface สำหรับ Repositories
- **ห้ามมีที่นี่**: DTOs, SQL Queries, HTTP Context, หรือ Package จากภายนอก (นอกจาก Primitives)

```csharp
// ตัวอย่าง: Domain Entity
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

- **จุดประสงค์**: นำ Business Use Cases ไปใช้งาน, จัดการการเปลี่ยนแปลงข้อมูล, และแปลง Entities เป็น DTOs
- **สิ่งที่อยู่ที่นี่**: Services (`UserService`), DTOs (`UserDto`), Interfaces สำหรับ Services, ตรรกะการตรวจสอบข้อมูล
- **ห้ามมีที่นี่**: SQL strings (`SELECT * FROM...`), HTTP Status Codes, หรือ Controller Bindings

```csharp
// ตัวอย่าง: Application Service Contract
public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<Guid> CreateUserAsync(CreateUserDto dto, Guid? createdBy = null);
}
```

### Infrastructure Layer (`EnterpriseWeb.Infrastructure`)

- **จุดประสงค์**: นำรายละเอียดทางเทคนิคไปใช้งานเพื่อติดต่อกับฐานข้อมูลและระบบภายนอก
- **สิ่งที่อยู่ที่นี่**: Dapper Repositories, SQL Queries, Dapper Context, UnitOfWork implementation
- **ห้ามมีที่นี่**: กฎ Business Validation หรือการจัดการ HTTP Request/Response

```csharp
// ตัวอย่าง: Infrastructure Repository
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

- **จุดประสงค์**: รับ HTTP Requests, บังคับใช้ Authentication/Authorization, และส่งคืน HTTP Responses
- **สิ่งที่อยู่ที่นี่**: Controllers, Middlewares, การตั้งค่า Program.cs, OpenAPI configuration
- **ห้ามมีที่นี่**: การคำนวณ Business Logic, การเริ่มต้น Database Context, หรือการจัดการ Transaction ด้วยตนเอง

```csharp
// ตัวอย่าง: API Controller
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

> **สรุปสั้น**: Controllers จัดการ Traffic, Services ประสาน Logic, Repositories รัน SQL, Entities กำหนดโครงสร้าง

---

## 4. หลัก SOLID — การนำไปใช้ในระบบนี้

### Single Responsibility Principle (SRP) — หลักความรับผิดชอบเดียว

- **คำอธิบาย**: คลาสหนึ่งควรทำสิ่งเดียวเท่านั้น
- **การบังคับใช้**: แยก DTO Mapping, DB Querying, และ HTTP Parsing ออกเป็นชั้นที่แตกต่างกัน
- **ตัวอย่างที่ดี**: `ExceptionHandlingMiddleware.cs` — ทำแค่จับ Exception และแปลงเป็น JSON เท่านั้น
- **ตัวอย่างที่ไม่ดี**: อัปเดต Role ของผู้ใช้และส่งอีเมลภายใน `UsersController` เดียวกัน
- **การทดสอบตัวเอง**: "ฉันสามารถอธิบายสิ่งที่คลาสนี้ทำได้โดยไม่ใช้คำว่า 'และ' หรือไม่?"

### Open/Closed Principle (OCP) — หลักเปิด/ปิด

- **คำอธิบาย**: โค้ดควรขยายได้โดยไม่ต้องแก้ไขโค้ดที่มีอยู่
- **การบังคับใช้**: ใช้ Policy-based Authorization (`[Authorize(Policy = "users:create")]`) สามารถเพิ่ม Permission ใหม่ได้โดยไม่ต้องแก้ไข Logic การ Login หลัก
- **การทดสอบตัวเอง**: "ถ้าฉันเพิ่ม Feature ใหม่ ต้องเขียนใหม่ทับ Framework Classes หลักหรือเปล่า?"

### Liskov Substitution Principle (LSP) — หลักการแทนที่

- **คำอธิบาย**: Implementation ต้องปฏิบัติตาม Contract ของ Interface โดยไม่โยน `NotImplementedException` ที่ไม่คาดคิด
- **การบังคับใช้**: `UserRepository` ส่งคืนผลลัพธ์ตามที่ `IUserRepository` สัญญาไว้เสมอ

### Interface Segregation Principle (ISP) — หลักการแยก Interface

- **คำอธิบาย**: อย่าบังคับให้ Client ขึ้นอยู่กับ Method ที่ไม่ได้ใช้
- **ตัวอย่างที่ดี**: `IUserService` และ `IRoleService` แยกจากกัน Controllers ไม่ต้อง Inject "GodService" ขนาดใหญ่
- **การทดสอบตัวเอง**: "Controller เรียก Method แค่ 1 อย่างจาก Service ที่มี 50 Methods หรือเปล่า?"

### Dependency Inversion Principle (DIP) — หลักการพึ่งพา Abstraction

- **คำอธิบาย**: โมดูลระดับสูงไม่พึ่งพาโมดูลระดับต่ำ ทั้งสองพึ่งพา Abstractions แทน
- **การบังคับใช้**: `UserService` พึ่งพา `IUserRepository` ไม่ใช่ `UserRepository` โดยตรง
- **ตัวอย่างที่ดี**:

```csharp
// UserService Inject Interface แทนคลาส Dapper โดยตรง
public class UserService(IUserRepository userRepository, IUnitOfWork unitOfWork) 
{ ... }
```

> **สรุปสั้น**: แยก Logic ออกเป็น Interfaces เพื่อให้ Codebase มีความยืดหยุ่นและทดสอบได้ง่าย

---

## 5. ขั้นตอนการเพิ่ม Feature ใหม่

เมื่อเพิ่ม CRUD Feature (เช่น `Product`) ให้ทำตามลำดับนี้อย่างเคร่งครัด:

### ขั้นตอนที่ 1. สร้าง Domain Entity (`EnterpriseWeb.Domain/Entities/Product.cs`)

ใช้ `record` และ `init` properties เพื่อความไม่เปลี่ยนแปลง (Immutability)

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

### ขั้นตอนที่ 2. สร้าง Repository Interface (`EnterpriseWeb.Domain/Interfaces/IProductRepository.cs`)

กำหนด DB Operations โดยต้องส่ง Transaction สำหรับ Operation ที่เปลี่ยนแปลงข้อมูล

```csharp
namespace EnterpriseWeb.Domain.Interfaces;
using System.Data;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(Product entity, IDbConnection conn, IDbTransaction tx);
}
```

### ขั้นตอนที่ 3. สร้าง DTOs (`EnterpriseWeb.Application/DTOs/Product/`)

สร้าง Request/Response DTOs แบบ Immutable

```csharp
public record ProductDto(Guid Id, string Name, decimal Price);
public record CreateProductDto(string Name, decimal Price);
```

### ขั้นตอนที่ 4. สร้าง Repository Implementation (`EnterpriseWeb.Infrastructure/Repositories/ProductRepository.cs`)

เขียน SQL อย่างชัดเจนด้วย Dapper

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

### ขั้นตอนที่ 5. สร้าง Application Service (`EnterpriseWeb.Application/Services/ProductService.cs`)

ประสานการทำงานของ Unit of Work

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

### ขั้นตอนที่ 6. สร้าง Controller (`EnterpriseWeb.API/Controllers/ProductsController.cs`)

จัดการ HTTP Routing และ Policies

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

> **สรุปสั้น**: Domain -> Repo Interface -> DTOs -> Repo Implementation -> Service -> Controller

---

## 6. ลำดับการทำงานของ Request — เกิดอะไรขึ้นเมื่อเรียก Endpoint

เมื่อ Request มาถึง `POST /api/users`:

1. **Rate Limiter:** ตรวจสอบว่า IP เกิน 100 req/min (หรือ 10/min สำหรับ Auth) หรือไม่ ถ้าเกินจะส่งคืน HTTP `429 Too Many Requests`
2. **ExceptionHandlingMiddleware:** ครอบ Pipeline ทั้งหมดด้วย `try/catch`
3. **Authentication:** ตรวจสอบ JWT Bearer Token ใน Header `Authorization` กับ `Key`, `Issuer`, และ `Expiration`
4. **Authorization:** Attribute `[Authorize(Policy = "users:create")]` ตรวจสอบว่า Token มี Claim `permission: users:create` หรือไม่
5. **Controller Routing:** เข้าสู่ `UsersController.Create()` โดย Body ถูกแปลงเป็น `CreateUserDto` อัตโนมัติ
6. **Service Execution:** เรียกใช้ `UserService.CreateUserAsync()`
   - Hash รหัสผ่าน
   - เรียก `unitOfWork.Begin()`
   - รัน Dapper SQL INSERT
   - ถ้าสำเร็จ เรียก `unitOfWork.Commit()` ถ้า SQL ล้มเหลว Block `catch` จะรัน `unitOfWork.Rollback()` และส่ง Exception ขึ้นไป
7. **Response Shaping:** Controller ส่งคืน `201 Created` พร้อม ID ใหม่
8. **Logging:** `SerilogRequestLogging` แสดงเวลาประมวลผลและ HTTP Status ไปยัง Console/Seq

> **สรุปสั้น**: Rate Limit -> Middleware -> JWT Auth -> Policy Auth -> Controller -> Service -> DB -> Response

---

## 7. การจัดการ Error และรูปแบบ Response

### รูปแบบหลัก

แทนที่จะครอบทุก Controller Action ด้วย `try/catch` โปรเจกต์นี้พึ่งพา Global Middleware เพื่อจัดการ Exception ที่ไม่ได้รับการจัดการ โดย C# Exception มาตรฐานถูกแปลงเป็น HTTP Response โดยตรง

### วิธีทำให้เกิด Error

ใน Application Service ให้ส่ง Exception มาตรฐานเมื่อ Business Validation ล้มเหลว:

```csharp
if (user is null) throw new KeyNotFoundException("User not found.");
if (!hasAccess) throw new UnauthorizedAccessException();
```

### โครงสร้าง Centralized Middleware (`ExceptionHandlingMiddleware.cs`)

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

ใช้ ASP.NET Core `IActionResult` Methods อย่างเคร่งครัด:

- `Ok(data)` สำหรับ GET 200
- `CreatedAtAction(...)` สำหรับ POST 201
- `NoContent()` สำหรับ PUT/DELETE 204
- `NotFound(new { message = "..." })` เมื่อ ID ไม่ถูกต้อง
- `BadRequest(new { message = "..." })` เมื่อ Payload Logic ล้มเหลว (เช่น ID ไม่ตรงกัน)

> **สรุปสั้น**: ส่ง Exception สำหรับความล้มเหลวร้ายแรง; ส่งคืน `BadRequest` หรือ `NotFound` จาก Controllers สำหรับข้อผิดพลาดจาก Input ของผู้ใช้

---

## 8. รูปแบบการตั้งชื่อ

| รายการ | รูปแบบตัวอักษร | ตัวอย่าง | กฎ |
|---|---|---|---|
| **Classes / Records** | PascalCase | `UserService`, `User` | ใช้คำนาม เสมอ Singular สำหรับ Records |
| **Interfaces** | PascalCase | `IUserService` | ขึ้นต้นด้วย `I` เสมอ |
| **Methods** | PascalCase | `GetUserByIdAsync` | ลงท้ายด้วย `Async` ถ้าส่งคืน `Task` |
| **Variables / Params** | camelCase | `existingUser`, `userId` | คำนามที่ชัดเจนและสื่อความหมาย |
| **DTOs** | PascalCase | `CreateUserDto` | ลงท้ายด้วย `Dto` นิยมใช้ Records |
| **SQL Tables** | TitleCase ลงท้ายด้วย s เสมอ | `[dbo].[Users]` | ครอบด้วย Brackets, ลงท้ายด้วย s เสมอ |
| **Service Injections** | camelCase | `userService` | Inject ผ่าน Primary Constructor |

> **สรุปสั้น**: PascalCase สำหรับ Types และ Methods, camelCase สำหรับตัวแปร, ขึ้นต้น Interface ด้วย 'I'

---

## 9. การจัดการและลงทะเบียน Dependency

Dependencies ถูก Inject ด้วย Microsoft's built-in Dependency Injection ผ่าน Constructor Injection (ใช้ C# 12 Primary Constructors) การลงทะเบียนจัดระเบียบตามชั้นด้วย Extension Methods

### วิธีลงทะเบียน Component ใหม่

เมื่อสร้าง Service หรือ Repository ใหม่ **ต้องลงทะเบียน** ใน Extension Class ของชั้นที่สอดคล้องกัน

**สำหรับ Services** (ใน `EnterpriseWeb.Application/DependencyInjection.cs` หรือไฟล์ที่คล้ายกัน):

```csharp
services.AddScoped<IProductService, ProductService>();
```

**สำหรับ Repositories** (ใน `EnterpriseWeb.Infrastructure/DependencyInjection.cs` หรือไฟล์ที่คล้ายกัน):

```csharp
services.AddScoped<IProductRepository, ProductRepository>();
```

### กฎเรื่อง Scope

- **AddScoped**: ใช้สำหรับเกือบทุกอย่าง (Services, Repositories) — สร้าง Instance ใหม่ทุก HTTP Request
- **AddTransient**: ใช้เฉพาะกับ Class ที่เบา ไม่มี State และไม่แชร์ทรัพยากรใดๆ — สร้าง Instance ใหม่ทุกครั้งที่มีการ Inject
- **AddSingleton**: สร้างแค่ครั้งเดียวตลอดอายุของแอป ใช้เฉพาะกับ Class ที่ไม่มี State เลย และต้องมั่นใจว่า Thread-safe เช่น Helper ที่รับ Input แล้วคืนค่าออกมาเลย ห้ามใช้กับ DB Connections หรืออะไรก็ตามที่มี State ที่เปลี่ยนแปลงได้เด็ดขาด!

> **สรุปสั้น**: ใช้ `AddScoped` สำหรับ Services/Repos และลงทะเบียนในไฟล์ Installer แยกตามชั้น

---

## 10. ข้อผิดพลาดที่พบบ่อยและวิธีหลีกเลี่ยง

| # | ข้อผิดพลาด | ทำไมจึงผิด | แนวทางที่ถูกต้อง |
|---|---|---|---|
| **1** | เขียน SQL ใน Controllers | ละเมิดการแยกชั้น UI ไม่ควรรู้เรื่อง DB Layout | วาง SQL ทั้งหมดใน `Infrastructure` Repositories |
| **2** | ลืมเรียก `unitOfWork.Commit()` | Transaction จะ Rollback เมื่อสิ้นสุด Request ข้อมูลจะไม่ถูกบันทึก | เรียก `Begin()`, `Commit()` เสมอ และครอบ `Rollback` ใน `catch` |
| **3** | ส่ง Domain Entities ออกไปยัง UI | เปิดเผยโครงสร้าง DB และรหัสผ่านให้ผู้ใช้เห็น | แปลง Entities เป็น `Dto` objects ใน Application Layer ก่อนส่งคืน |
| **4** | ใช้ `new SqlConnection()` ทั่วไป | ข้ามการจัดการ Context ที่กำหนดไว้และทำให้ Connection รั่ว | Inject `DapperContext` และใช้ `context.CreateConnection()` |
| **5** | Hardcode Authentication Logic | ทดสอบและนำกลับมาใช้ยาก | ใช้ `[Authorize(Policy = "...")]` บน Controllers |
| **6** | ไม่ใช้ `async/await` สำหรับ DB Calls | บล็อก Thread เมื่อโหลดสูง | ใช้ `ExecuteAsync`/`QueryAsync` และ `Task` เสมอ |
| **7** | แก้ไข `record` properties โดยตรง | `Records` ออกแบบมาเป็น Immutable ด้วย `init` | ใช้คีย์เวิร์ด `with`: `existingUser with { Email = "new" }` |
| **8** | ส่งคืน 200 OK หลังการสร้าง | ละเมิดมาตรฐาน REST | ส่งคืน `201 CreatedAtAction(...)` |
| **9** | ลืมเพิ่มไฟล์ `.slnx` ใน Solution | ไฟล์จะไม่เปิดใน Visual Studio อย่างถูกต้อง | ตรวจสอบให้ไฟล์อยู่ใน Solution Tree |
| **10** | วาง AppSettings Paths ในเมธอด | Path ที่ Hardcode จะ Crash ใน Docker/Production | ใช้ `IConfiguration` หรือ `IOptions<>` |

> **สรุปสั้น**: หลีกเลี่ยง Shortcuts รักษา Controller ให้เรียบง่าย, Services ฉลาด, Repositories เคร่งครัด

---

## 11. สรุปอ้างอิงฉบับย่อ

### ✅ Checklist สำหรับ Feature ใหม่

1. สร้าง `Entity` (record) ใน Domain
2. สร้าง `IRepository` ใน Domain
3. สร้าง `Create/Update/Response DTOs` ใน Application
4. สร้าง `Repository` (Dapper + Transactions) ใน Infrastructure
5. สร้าง Interface และ Implementation ของ `Service` ใน Application
6. ลงทะเบียน Service และ Repo ใน DI Container
7. สร้าง `Controller` เพื่อ Map HTTP Routes ไปยัง Service

### 📝 รูปแบบชื่อไฟล์

- Entity: `Feature.cs`
- Service Interface: `IFeatureService.cs`
- Service Code: `FeatureService.cs`
- DTO: `CreateFeatureDto.cs`
- Controller: `FeaturesController.cs` (ลงท้ายด้วย s เสมอ)

### 🚀 HTTP Responses มาตรฐาน

- **GET -> 200 OK**: `return Ok(dto);`
- **GET (ไม่พบ) -> 404 Not Found**: `return NotFound(new { message = "..." });`
- **POST -> 201 Created**: `return CreatedAtAction(nameof(GetById), new { id }, new { id });`
- **PUT -> 204 No Content**: `return NoContent();`
- **DELETE -> 204 No Content**: `return NoContent();`
- **Validation ล้มเหลว -> 400 Bad Request**: `return BadRequest(new { message = "..." });`
- **Unauthorized (ไม่ได้ login) -> 401 Unauthorized**: `return Unauthorized(new { message = "..." });`
- **Forbidden (ไม่มีสิทธิ์) -> 403 Forbidden**: `return StatusCode(403, new { message = "..." });`
- **Conflict (ข้อมูลซ้ำ) -> 409 Conflict**: `return Conflict(new { message = "..." });`
- **Too Many Requests -> 429 Too Many Requests**: `return StatusCode(429, new { message = "..." });`
- **Server Error -> 500 Internal Server Error**: (จัดการโดย Middleware อัตโนมัติ)