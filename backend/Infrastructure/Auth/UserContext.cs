using backend.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace backend.Infrastructure.Auth
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public Guid UserId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null) return Guid.Empty;

                // Be extremely permissive: try all possible claim names for the ID
                var userIdClaim = user.FindFirst("id")?.Value
                               ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? user.FindFirst("uid")?.Value;

                return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
            }
        }

        public string GlobalRole
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null) return string.Empty;

                return user.FindFirst("role")?.Value 
                    ?? user.FindFirst(ClaimTypes.Role)?.Value 
                    ?? user.FindFirst("roles")?.Value
                    ?? string.Empty;
            }
        }
    }
}
