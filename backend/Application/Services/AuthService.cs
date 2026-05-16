using backend.Application.Configurations;
using backend.Application.DTOs;
using backend.Application.DTOs.Auth;
using backend.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtConfig _jwtConfig;

        public AuthService(IUserRepository userRepository, JwtConfig jwtConfig)
        {
            _userRepository = userRepository;
            _jwtConfig = jwtConfig;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmail(request.Email);
            if (user == null)
            {
                throw new Exception("Invalid email or password.");
            }

            if (user.IsLocked)
            {
                throw new UnauthorizedAccessException("Account is locked");
            }

            // Verify password using BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new Exception("Invalid email or password.");
            }

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token,
                User = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email
                }
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userRepository.GetByEmail(request.Email);
            if (existingUser != null)
            {
                throw new Exception("Email is already in use.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUserId = await _userRepository.Create(request.Username, request.Email, passwordHash);

            var user = await _userRepository.GetByIdAsync(newUserId);

            var token = GenerateJwtToken(user!);

            return new AuthResponse
            {
                Token = token,
                User = new UserResponse
                {
                    Id = user!.Id,
                    Email = user.Email
                }
            };
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetByEmail(email);
            if (user == null)
            {
                // To prevent email enumeration, return gracefully without telling the user
                return;
            }

            var token = Guid.NewGuid().ToString("N");
            var expiredAt = DateTime.UtcNow.AddMinutes(15);

            await _userRepository.CreatePasswordResetTokenAsync(user.Id, token, expiredAt);

            // In a real application, send this token via email here.
            // For now, we will simulate this by logging it.
            Console.WriteLine($"[EMAIL SENT TO {email}] Password Reset Link: {token}");
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            var userId = await _userRepository.GetUserIdByValidResetTokenAsync(token);
            if (userId == null)
            {
                throw new Exception("Invalid or expired password reset token.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdatePasswordAsync(userId.Value, passwordHash);
            await _userRepository.DeletePasswordResetTokenAsync(token);
        }

        private string GenerateJwtToken(Domain.Entities.User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.GlobalRole ?? "user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtConfig.ExpireMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
