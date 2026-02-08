using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.DTOs
{
    public class StartExamResponseDto
    {
        public int AttemptId {get; set;}
        public DateTime StartTime {get; set;}

        public ExamBasicInfoDto Exam { get; set; } = null!;
        public QuestionResponseDto FirstQuestion { get; set; } = null!;

    }

    public class ExamBasicInfoDto
    {
        public string Title { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
    }

}