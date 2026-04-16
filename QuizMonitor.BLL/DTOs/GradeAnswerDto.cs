using System.ComponentModel.DataAnnotations;

namespace QuizMonitor.BLL.DTOs
{
    public class GradeAnswerDto
    {
        [Required(ErrorMessage = "Score is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Score must be non-negative")]
        public decimal Score { get; set; }

        [MaxLength(2000, ErrorMessage = "Feedback cannot exceed 2000 characters")]
        public string? Feedback { get; set; }
    }
}
