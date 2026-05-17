using backend.Application.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace backend.Infrastructure.Auth
{
    public static class AuthExtensions
    {
        public static IServiceCollection AddJwtAuth(this IServiceCollection services, JwtConfig jwtConfig)
        {
            // Clear default inbound claim mapping to keep 'sub', 'role' as is
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            if (string.IsNullOrEmpty(jwtConfig.Secret))
            {
                throw new Exception("JWT Secret is missing in configuration!");
            }

            var key = Encoding.UTF8.GetBytes(jwtConfig.Secret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false, // Loosen for troubleshooting
                    ValidateAudience = false, // Loosen for troubleshooting
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Add diagnostic header
                        context.Response.Headers.Append("X-Auth-Error", context.Exception.Message);
                        
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context => 
                    {
                        // Success - can log here if needed
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}
