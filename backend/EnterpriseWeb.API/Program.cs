using System.Threading.RateLimiting;
using EnterpriseWeb.API.Middleware;
using EnterpriseWeb.API.OpenApi;
using EnterpriseWeb.Application;
using EnterpriseWeb.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ─────────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .WriteTo.Seq(
                context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

    // ── Layers ──────────────────────────────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure();

    // ── Health Checks ───────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "sqlserver",
            tags: ["db", "ready"]);

    // ── Controllers & OpenAPI ────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi(opt =>
    {
        opt.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    // ── JWT Authentication ────────────────────────────────────────────────────────
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSection["Key"]
        ?? throw new InvalidOperationException("Jwt:Key is not configured."));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(key),
            ClockSkew                = TimeSpan.Zero
        };
    });

    // ── Policy-based Authorization ────────────────────────────────────────────────
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("RequireAdminRole", p => p.RequireRole("System Admin"))
        .AddPolicy("users:view",       p => p.RequireClaim("permission", "users:view"))
        .AddPolicy("users:create",     p => p.RequireClaim("permission", "users:create"))
        .AddPolicy("users:update",     p => p.RequireClaim("permission", "users:update"))
        .AddPolicy("users:delete",     p => p.RequireClaim("permission", "users:delete"))
        .AddPolicy("dashboard:view",   p => p.RequireClaim("permission", "dashboard:view"));

    // ── CORS ──────────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                        ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:5175"])
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    // ── Rate Limiting ────────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Global policy: 100 requests per minute per IP
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        // Strict policy for auth endpoints: 10 requests per minute per IP
        options.AddFixedWindowLimiter("auth-strict", limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                status = 429,
                message = "Too many requests. Please try again later.",
                retryAfter = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retryAfter)
                    ? retryAfter.TotalSeconds
                    : 60
            }, cancellationToken);
        };
    });

    // ─────────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Health endpoints (anonymous) ─────────────────────────────────────────────
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => false, // Liveness: no dependency checks
        ResponseWriter = WriteHealthResponse
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthResponse
    }).AllowAnonymous();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opt =>
        {
            opt.Title = "EnterpriseWeb API";
            opt.Theme = ScalarTheme.DeepSpace;
            opt.Authentication = new ScalarAuthenticationOptions
            {
                PreferredSecuritySchemes = ["Bearer"],
            };
        });
    }

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ── Health check response writer ─────────────────────────────────────────────
static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var result = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds
        }),
        totalDuration = report.TotalDuration.TotalMilliseconds
    };
    return context.Response.WriteAsJsonAsync(result);
}
