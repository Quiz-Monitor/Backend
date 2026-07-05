using System;

namespace QuizMonitor.BLL.DTOs
{
    public class InstructorRecentExamDto
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public int NumberOfStudents { get; set; }
        public DateTime? ScheduledAt { get; set; }

        /// <summary>(submitted+graded attempts / total attempts) * 100, rounded to 1 decimal.
        /// Only populated once the exam is over (EndTime &lt;= now). Null otherwise.</summary>
        public double? CompletionPercent { get; set; }

        /// <summary>Sum of TotalViolations across all attempts.
        /// Only populated once the exam is over. Null otherwise.</summary>
        public int? NumberOfFlags { get; set; }
    }
}
