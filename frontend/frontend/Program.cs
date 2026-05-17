using frontend.Client.Pages;
using frontend.Components;
using frontend.Client.Services.Http;
using frontend.Client.Services.Storage;
using frontend.Services.Storage;
using frontend.Client.Core.State;
using frontend.Client.Features.Auth.Services;
using frontend.Client.Features.Workspace.Services;
using frontend.Client.Features.Support.Services;
using frontend.Client.Features.User.Services;
using Microsoft.AspNetCore.Components.Authorization;
using frontend.Client.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System;
using System.Net.Http;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var backendUrl = builder.Configuration["BackendUrl"] ?? "http://localhost:5087/";

// Core HTTP
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(backendUrl) });
builder.Services.AddScoped<IBackendClient, BackendClient>();

// Clean Architecture Services
builder.Services.AddScoped<WorkspaceStateService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<ICookieService, ServerCookieService>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<LayoutStateService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<UserService>();

// Add Auth services
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication("JWT_OR_COOKIE")
    .AddCookie("JWT_OR_COOKIE", options => {
        options.Cookie.Name = "access_token";
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorization();

// SSR safe token reader
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Custom Middleware to set User from Cookie JWT
app.Use(async (context, next) =>
{
    var token = context.Request.Cookies["access_token"];
    if (!string.IsNullOrEmpty(token))
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                if (jwt.ValidTo > DateTime.UtcNow)
                {
                    var claims = jwt.Claims.ToList();
                    
                    // Standardize names for Authorization
                    var nameClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == ClaimTypes.Name)?.Value;
                    var roleValue = claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role || c.Type == "GlobalRole")?.Value ?? "user";

                    var identity = new ClaimsIdentity(claims, "jwt", "name", "role");
                    
                    // Ensure mandatory claims are present with the types we specified in constructor
                    if (!identity.HasClaim(c => c.Type == "name") && nameClaim != null)
                        identity.AddClaim(new Claim("name", nameClaim));
                    
                    if (!identity.HasClaim(c => c.Type == "role"))
                        identity.AddClaim(new Claim("role", roleValue));

                    context.User = new ClaimsPrincipal(identity);
                }
            }
        }
        catch { /* ignored */ }
    }
    await next();
});

// Skip app.UseAuthentication() as we handle it manually above
app.UseAuthorization();

app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<frontend.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(frontend.Client._Imports).Assembly);

app.Run();

// SSR Authentication State Provider
public class ServerAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user != null && user.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(new AuthenticationState(user));
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
