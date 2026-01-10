namespace QuizMonitor.BLL.DTOs;

public class CreateUserDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!; // "instructor", "student", "admin"
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
}
