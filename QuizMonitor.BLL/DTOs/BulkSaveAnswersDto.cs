using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuizMonitor.BLL.DTOs
{
    public class BulkSaveAnswersDto
    {
        [Required(ErrorMessage = "Answers list is required")]
        [MinLength(1, ErrorMessage = "At least one answer must be provided")]
        public List<SaveAnswerDto> Answers { get; set; } = new();
    }
}
