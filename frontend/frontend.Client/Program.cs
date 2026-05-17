using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using frontend.Client.Services;
using frontend.Client.Services.Http;
using frontend.Client.Services.Storage;
using frontend.Client.Core.State;
using frontend.Client.Features.Auth.Services;
using frontend.Client.Features.Workspace.Services;
using frontend.Client.Features.Support.Services;
using frontend.Client.Features.User.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var backendUrl = "http://localhost:5087/";

// Core HTTP
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(backendUrl) });
builder.Services.AddScoped<IBackendClient, BackendClient>();

// Clean Architecture Services
builder.Services.AddScoped<WorkspaceStateService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<LayoutStateService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ITokenStorageService, TokenStorageService>();

// Add Auth services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>();

await builder.Build().RunAsync();
