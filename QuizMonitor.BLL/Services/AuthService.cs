using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.DAL.Data;
using QuizMonitor.DAL.Models;
using Microsoft.Extensions.Configuration;

namespace QuizMonitor.BLL.Services
{
    public class AuthService
    {
        private readonly QuizMonitorDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(QuizMonitorDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO dto)
        {
            // check if the user is already exist
            var existingUser = await _context.Users
                .Where(u => u.Email == dto.Email && u.DeletedAt == null)
                .FirstOrDefaultAsync();
            
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            // hash password

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // create new user

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = passwordHash,
                FullName = dto.FullName,
                Role = dto.Role,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate tokens
            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO dto)
        {
            // Find user by email (excluding soft-deleted users)
            var user = await _context.Users
                .Where(u => u.Email == dto.Email && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens
            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(string refreshToken)
        {
            // Find user by refresh token (excluding soft-deleted users)
            var user = await _context.Users
                .Where(u => u.RefreshToken == refreshToken && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            // Generate new tokens
            return await GenerateTokensAsync(user);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.UserId == userId && u.DeletedAt == null)
                .FirstOrDefaultAsync();
        }

        private async Task<AuthResponseDTO> GenerateTokensAsync(User user)
        {
            // Read JWT settings from configuration

            var secretKey = _configuration["JwtSettings:SecretKey"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "30");
            var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7");

            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");
            }

            // VCreate JWT claims

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)   
            };
            // Create signing credentials

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // Set expiration At
            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
            // Create JWT token
            var token = new JwtSecurityToken
            (
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            // Generate refresh token using cryptographically secure random bytes
            var refreshTokenBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(refreshTokenBytes);
            }
            var refreshToken = Convert.ToBase64String(refreshTokenBytes);
            // Save refresh token to database
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Return response

            return new AuthResponseDTO
            {
                Token = tokenString,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UserInfoDTO
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role
                }
            };
        }
    }
}