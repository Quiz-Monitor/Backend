using System.ComponentModel.DataAnnotations;

namespace QuizMonitor.BLL.DTOs
{
    public class GradeAnswerDto
    {
        [Required(ErrorMessage = "Score is required")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Score must be non-negative")]
        public decimal? Score { get; set; }

        [MaxLength(2000, ErrorMessage = "Feedback cannot exceed 2000 characters")]
        public string? Feedback { get; set; }
    }
}
