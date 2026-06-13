using System.ComponentModel.DataAnnotations;

namespace QuizMonitor.BLL.DTOs
{
    public class DeleteAccountDTO
    {
        [Required(ErrorMessage = "Password is required to confirm account deletion")]
        public string Password { get; set; } = string.Empty;
    }
}
