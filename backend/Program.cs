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

var builder = WebApplication.CreateBuilder(args);

// Load .env file
EnvLoader.Load();

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
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();

var app = builder.Build();

//GlobalException
app.UseMiddleware<GlobalExceptionMiddleware>();

// Test DB Connection on Startup
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    try
    {
        using var conn = dbFactory.Create();
        conn.Open();
        Console.WriteLine("DB CONNECTED SUCCESSFULLY");
    }
    catch (Exception ex)
    {
        Console.WriteLine("DB CONNECTION FAILED");
        Console.WriteLine(ex.Message);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();