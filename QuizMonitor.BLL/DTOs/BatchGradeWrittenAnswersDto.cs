using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuizMonitor.BLL.DTOs
{
    public class BatchGradeWrittenAnswersDto
    {
        [Required(ErrorMessage = "Grades list is required")]
        [MinLength(1, ErrorMessage = "At least one grade entry is required")]
        public List<GradeEntryDto> Grades { get; set; } = new();
    }

    public class GradeEntryDto
    {
        [Required(ErrorMessage = "Answer ID is required")]
        public int AnswerId { get; set; }

        [Required(ErrorMessage = "Score is required")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Score must be non-negative")]
        public decimal Score { get; set; }

        [MaxLength(2000, ErrorMessage = "Feedback cannot exceed 2000 characters")]
        public string? Feedback { get; set; }
    }
}
