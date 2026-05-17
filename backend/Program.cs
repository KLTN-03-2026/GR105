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

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Initial logger for configuration phase
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try 
{
    Log.Information("Starting web application startup...");
    
    // Load .env file BEFORE building anything that might need it
    EnvLoader.Load();

    var builder = WebApplication.CreateBuilder(args);

    // Re-configure Serilog with full settings after Env is loaded
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

    builder.Host.UseSerilog();

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
    Log.Information("CORS Origins: {Origins}", string.Join(", ", corsOrigins));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // In Dev, be more permissive with local domains like frontend.dev.localhost
                policy.SetIsOriginAllowed(origin => 
                {
                    var host = new Uri(origin).Host;
                    return host == "localhost" || host.EndsWith(".localhost");
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            }
            else if (corsOrigins.Length > 0)
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
        });
    });

    builder.Services.AddControllers(options =>
{
    options.Filters.Add<backend.API.Middleware.ResponseWrapperFilter>();
});
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure DB
    var dbConfig = new DbConfig
    {
        Host = Env.GetString("DB_HOST"),
        Port = Env.GetString("DB_PORT"),
        Database = Env.GetString("DB_NAME"),
        Username = Env.GetString("DB_USER"),
        Password = Env.GetString("DB_PASS")
    };
    
    Log.Information("DB Config loaded: Host={Host}, Port={Port}, Database={Database}, User={User}", 
        dbConfig.Host, dbConfig.Port, dbConfig.Database, dbConfig.Username);

    builder.Services.AddSingleton(dbConfig);
    builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
    builder.Services.AddScoped<IDbSession, DbSession>();

    // Configure Redis (Safe connection with short timeout)
    var redisConnStr = Env.GetString("REDIS_URL");
    if (string.IsNullOrEmpty(redisConnStr)) redisConnStr = "localhost:6379";
    
    Log.Information("Connecting to Redis at {Url}...", redisConnStr);
    
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => {
        try {
            var options = ConfigurationOptions.Parse(redisConnStr);
            options.ConnectTimeout = 1000; // Reduce to 1s to prevent long startup lag
            options.SyncTimeout = 1000;
            options.AbortOnConnectFail = false; 
            return ConnectionMultiplexer.Connect(options);
        } catch (Exception ex) {
            Log.Error(ex, "Failed to connect to Redis. Features relying on Redis might fail.");
            throw; 
        }
    });

    // Configure JWT Config
    var jwtConfig = new JwtConfig
    {
        Secret = Env.GetString("JWT_SECRET") ?? "fallback_secret_for_dev_only_123456",
        Issuer = Env.GetString("JWT_ISSUER") ?? "dms",
        Audience = Env.GetString("JWT_AUDIENCE") ?? "dms_user",
        ExpireMinutes = int.Parse(Env.GetString("JWT_EXPIRE_MINUTES") ?? "60")
    };
    builder.Services.AddSingleton(jwtConfig);
    builder.Services.AddJwtAuth(jwtConfig);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserContext, UserContext>();

    // Repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAdminRepository, AdminRepository>();
    builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
    builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
    builder.Services.AddScoped<IFileRepository, FileRepository>();
    builder.Services.AddScoped<ICommentRepository, CommentRepository>();
    builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

    // Services
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
    builder.Services.AddScoped<IFileService, FileService>();
    builder.Services.AddScoped<IDiffService, DiffService>();
    builder.Services.AddScoped<IMediaService, MediaService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<IFeedbackService, FeedbackService>();
    builder.Services.AddScoped<IFileSearchService, FileSearchService>();
    builder.Services.AddScoped<ITextExtractionService, TextExtractionService>();

    builder.Services.AddSignalR();

    builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => new backend.Infrastructure.Workers.BackgroundTaskQueue(100));
    builder.Services.AddHostedService<backend.Infrastructure.Workers.QueueHostedService>();
    builder.Services.AddHostedService<backend.Infrastructure.Workers.SafeCleanupWorker>();

    builder.Services.AddRateLimiter(options => {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext => {
            var userId = httpContext.User?.FindFirst("sub")?.Value;
            var key = !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{httpContext.Connection.RemoteIpAddress}";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions {
                AutoReplenishment = true,
                PermitLimit = 100,
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
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // --- SEEDING & DB TEST ---
    using (var scope = app.Services.CreateScope())
    {
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        try
        {
            using var conn = dbFactory.Create();
            conn.Open();
            Log.Information("DATABASE CONNECTED SUCCESSFULLY");

            // Check if 'is_locked' column exists to avoid crash if migrations weren't run
            var columnExists = conn.ExecuteScalar<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name='users' AND column_name='is_locked'
                );");

            var adminCountSql = "SELECT COUNT(*) FROM users WHERE global_role = 'admin';";
            int adminCount = conn.ExecuteScalar<int>(adminCountSql);

            if (adminCount == 0)
            {
                var adminEmail = Env.GetString("DEFAULT_ADMIN_EMAIL");
                var adminPassword = Env.GetString("DEFAULT_ADMIN_PASSWORD");

                if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
                {
                    Log.Information("Seeding default admin: {Email}", adminEmail);
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

                    var insertAdminSql = columnExists 
                        ? "INSERT INTO users (username, email, password_hash, global_role, is_locked) VALUES (@U, @E, @P, 'admin', false);"
                        : "INSERT INTO users (username, email, password_hash, global_role) VALUES (@U, @E, @P, 'admin');";

                    conn.Execute(insertAdminSql, new { U = "System Administrator", E = adminEmail, P = passwordHash });
                    Log.Information("Default admin seeded successfully.");
                }
            }
            else
            {
                Log.Information("Admin already exists ({Count}), skipping seed.", adminCount);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database connection or seeding failed during startup.");
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowFrontend");
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();
    app.MapHub<backend.API.Hubs.WorkspaceHub>("/hubs/workspace");

    Log.Information("Application is ready and running.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup.");
}
finally
{
    Log.CloseAndFlush();
}
