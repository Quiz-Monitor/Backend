using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.DTOs
{
    public class SaveAnswerDto
    {
        public int QuestionId { get; set; }
        
        // For MCQ questions
        public List<int>? SelectedChoices { get; set; } = new();
        
        // For open-ended questions
        public string? AnswerText { get; set; }
        
        public DateTime StartedAt { get; set; }
        public DateTime AnsweredAt { get; set; }
        public int TimeSpentSeconds { get; set; }
    }
}