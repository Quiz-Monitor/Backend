namespace QuizMonitor.BLL.DTOs;

public class UserResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
