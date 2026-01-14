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
    }
}