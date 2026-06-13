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
using QuizMonitor.BLL.Interfaces;
using QuizMonitor.DAL.Data;
using QuizMonitor.DAL.Interfaces;
using QuizMonitor.DAL.Models;
using Microsoft.Extensions.Configuration;

namespace QuizMonitor.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO dto)
        {
            // check if the user is already exist
            var existingUser = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.DeletedAt == null);
            
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
                Role = dto.Role.ToLower(),
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Generate tokens
            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO dto)
        {
            // Find user by email (excluding soft-deleted users)
            var user = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.DeletedAt == null);

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
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Generate tokens
            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(string refreshToken)
        {
            // Find user by refresh token (excluding soft-deleted users)
            var user = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.DeletedAt == null);

            if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            // Generate new tokens
            return await GenerateTokensAsync(user);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedAt == null);
        }

        public async Task LogoutAsync(int userId)
        {
            var user = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedAt == null);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            // Invalidate refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAccountAsync(int userId, string password)
        {
            var user = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedAt == null);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                throw new InvalidOperationException("Incorrect password");
            }

            // Soft delete
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = userId;
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDTO dto)
        {
            var user = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedAt == null);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Current password is incorrect");
            }

            // Validate new password matches confirmation
            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                throw new InvalidOperationException("New password and confirmation do not match");
            }

            // Validate new password is different from current
            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("New password must be different from current password");
            }

            // Hash and update
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<string?> ForgotPasswordAsync(string email)
        {
            var user = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null);

            if (user == null)
            {
                // Return null — caller will still return 200 to prevent email enumeration
                return null;
            }

            // Generate 6-digit reset code
            var random = new Random();
            var resetCode = random.Next(100000, 999999).ToString();

            // Store in RefreshToken fields with 15-minute expiry
            user.RefreshToken = resetCode;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return resetCode;
        }

        public async Task ResetPasswordAsync(ResetPasswordDTO dto)
        {
            var user = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.DeletedAt == null);

            if (user == null)
            {
                throw new InvalidOperationException("Invalid email or reset token");
            }

            // Verify reset token
            if (user.RefreshToken != dto.ResetToken || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Invalid or expired reset token");
            }

            // Validate passwords match
            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                throw new InvalidOperationException("New password and confirmation do not match");
            }

            // Hash and update
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
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
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

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