using System;
using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class ExamQuestionsResponseDto
    {
        /// <summary>Store this — needed for POST /api/exam-attempts/{AttemptId}/answers/bulk</summary>
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;

        /// <summary>Use this to run the student-side countdown timer</summary>
        public DateTime StartedAt { get; set; }
        public int DurationMinutes { get; set; }

        public int TotalQuestions { get; set; }
        public List<QuestionResponseDto> Questions { get; set; } = new();
    }
}
