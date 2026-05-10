using backend.Application.Configurations;
using backend.Infrastructure.Auth;
using backend.Infrastructure.Config;
using backend.Infrastructure.Persistence;
using backend.Infrastructure.Repositories;
using DotNetEnv;
using backend.Application.Interfaces;
using backend.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using backend.API.Middleware;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using Dapper;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Async(a => a.File("logs/syslog.txt", 
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        retainedFileCountLimit: 14,
        flushToDiskInterval: TimeSpan.FromSeconds(5)
    ))
    .CreateLogger();

try 
{
    Log.Information("Starting web application...");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Load .env file
EnvLoader.Load();

// Calculate max upload size from .env or use default 500MB
long maxUploadSizeMB = long.TryParse(Env.GetString("MAX_UPLOAD_SIZE_MB"), out var parsedSize) ? parsedSize : 500;
long maxUploadSizeBytes = maxUploadSizeMB * 1024 * 1024;

// Configure Kestrel Server limits for large file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadSizeBytes;
});

// Configure Form limits for large file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = maxUploadSizeBytes;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Add CORS configuration
var corsOrigins = Env.GetString("CORS_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR and Cookies if used
        }
        else
        {
            // Fallback for development if no env variable is set
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DB
builder.Services.AddSingleton(new DbConfig
{
    Host = Env.GetString("DB_HOST"),
    Port = Env.GetString("DB_PORT"),
    Database = Env.GetString("DB_NAME"),
    Username = Env.GetString("DB_USER"),
    Password = Env.GetString("DB_PASS")
});
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IDbSession, DbSession>();

// Configure Redis
var redisConnStr = Env.GetString("REDIS_URL");
if (string.IsNullOrEmpty(redisConnStr)) redisConnStr = "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnStr)
);

// Configure JWT Config
var jwtConfig = new JwtConfig
{
    Secret = Env.GetString("JWT_SECRET"),
    Issuer = Env.GetString("JWT_ISSUER"),
    Audience = Env.GetString("JWT_AUDIENCE"),
    ExpireMinutes = int.Parse(Env.GetString("JWT_EXPIRE_MINUTES") ?? "60")
};
builder.Services.AddSingleton(jwtConfig);

// Add JWT Auth (using our new extension method)
builder.Services.AddJwtAuth(jwtConfig);

// HttpContextAccessor for UserContext
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>(); // Admin Phase 15
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>(); // UC9
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>(); // UC6

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>(); // Admin Phase 15
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IDiffService, DiffService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ICommentService, CommentService>(); // UC9
builder.Services.AddScoped<IFeedbackService, FeedbackService>(); // UC6
builder.Services.AddScoped<IFileSearchService, FileSearchService>(); // UC*.1
builder.Services.AddScoped<ITextExtractionService, TextExtractionService>(); // UC*.1

// SignalR (UC9)
builder.Services.AddSignalR();

// Hosted Services (Background Workers)
builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => new backend.Infrastructure.Workers.BackgroundTaskQueue(100));
builder.Services.AddHostedService<backend.Infrastructure.Workers.QueueHostedService>();
builder.Services.AddHostedService<backend.Infrastructure.Workers.SafeCleanupWorker>();

// Rate limiting assesment
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    var permitPerMinute = builder.Configuration.GetValue<int>("RATE_LIMIT_PER_MINUTE", 100);

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User?.FindFirst("sub")?.Value
                  ?? httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var key = !string.IsNullOrEmpty(userId)
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permitPerMinute,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});


var app = builder.Build();

app.UseSerilogRequestLogging();

// Thêm Middleware Global Exception xử lý lỗi NotFound, Forbidden, Validation...
app.UseMiddleware<GlobalExceptionMiddleware>();

// Test DB Connection and Seed Default Admin on Startup
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    try
    {
        using var conn = dbFactory.Create();
        conn.Open();
        Console.WriteLine("DB CONNECTED SUCCESSFULLY");

        // --- SEED DEFAULT ADMIN ---
        var checkAdminSql = "SELECT COUNT(*) FROM users WHERE global_role = 'admin';";
        int adminCount = conn.ExecuteScalar<int>(checkAdminSql);

        if (adminCount == 0)
        {
            var adminEmail = Env.GetString("DEFAULT_ADMIN_EMAIL");
            var adminPassword = Env.GetString("DEFAULT_ADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                Console.WriteLine("WARNING: DEFAULT_ADMIN_EMAIL or DEFAULT_ADMIN_PASSWORD is not set in .env. Skipping admin seeding.");
            }
            else
            {
                Console.WriteLine("No admin found. Seeding default admin account from .env...");
                var adminUsername = "System Administrator";

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

                var insertAdminSql = @"
                    INSERT INTO users (username, email, password_hash, global_role, is_locked)
                    VALUES (@Username, @Email, @PasswordHash, 'admin', false);
                ";

                conn.Execute(insertAdminSql, new
                {
                    Username = adminUsername,
                    Email = adminEmail,
                    PasswordHash = passwordHash
                });

                Console.WriteLine($"Default admin seeded successfully: {adminEmail}");
            }
        }
        else
        {
            Console.WriteLine($"Found {adminCount} admin(s) in DB. Skipping seed.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("DB CONNECTION OR SEEDING FAILED");
        Console.WriteLine(ex.Message);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowFrontend");

//ASP.NET network routing 
app.UseRouting();

// Authentication MUST be before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Rate limiter after auth to access user claims
app.UseRateLimiter();

app.MapControllers();
app.MapHub<backend.API.Hubs.WorkspaceHub>("/hubs/workspace"); // UC9

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
