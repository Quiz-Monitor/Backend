using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class QuestionDetailDto
    {
        public string QuestionText { get; set; } = string.Empty;
        public int? TimeSpentSeconds { get; set; }
        public List<string> Violations { get; set; } = new List<string>();
    }
}
