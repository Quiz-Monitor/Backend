using System;
using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class QuestionResponseDto
    {
        public int QuestionId { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string? QuestionImageUrl { get; set; }
        public decimal Points { get; set; }
        public int OrderNumber { get; set; }
        public bool IsRequired { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ChoiceDto>? Choices { get; set; }
    }
}
