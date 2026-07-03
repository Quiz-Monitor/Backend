namespace QuizMonitor.BLL.DTOs
{
    public class InstructorExamOverviewDto
    {
        public int TotalExamsCreated { get; set; }
        public int TotalExamsPublished { get; set; }
        public int TotalExamsDraft { get; set; }
        public int TotalExamsWithAttempts { get; set; }
    }

    public class InstructorStudentOverviewDto
    {
        public int TotalUniqueStudents { get; set; }
        public int TotalAttempts { get; set; }
        public int TotalGradedAttempts { get; set; }
        public int TotalPendingGradingAttempts { get; set; }
    }

    public class ExamAverageScoreDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = null!;
        public decimal AverageScorePercentage { get; set; }
        public int AttemptCount { get; set; }
    }

    public class InstructorScoreStatisticsDto
    {
        public decimal? AverageScorePercentage { get; set; }
        public ExamAverageScoreDto? HighestAverageExam { get; set; }
        public ExamAverageScoreDto? LowestAverageExam { get; set; }
        public decimal? PassRate { get; set; }
    }

    public class InstructorIntegrityStatisticsDto
    {
        public int TotalViolationsAcrossAllExams { get; set; }
        public decimal AverageViolationsPerAttempt { get; set; }
        public int CleanAttempts { get; set; }
        public int WarningAttempts { get; set; }
        public int FlaggedAttempts { get; set; }
    }

    public class InstructorStatisticsResponseDto
    {
        public InstructorExamOverviewDto ExamOverview { get; set; } = new();
        public InstructorStudentOverviewDto StudentOverview { get; set; } = new();
        public InstructorScoreStatisticsDto ScoreStatistics { get; set; } = new();
        public InstructorIntegrityStatisticsDto IntegrityStatistics { get; set; } = new();
    }
}
