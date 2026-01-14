using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class UpdateQuestionDto
    {
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string? QuestionImageUrl { get; set; }
        public decimal Points { get; set; }
        public int OrderNumber { get; set; }
        public bool IsRequired { get; set; } = true;
        public List<ChoiceDto>? Choices { get; set; }
    }
}
