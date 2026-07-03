namespace QuizMonitor.BLL.DTOs
{
    public class StudentOverviewDto
    {
        public int TotalExamsSubmitted { get; set; }
        public int TotalExamsGraded { get; set; }
        public int TotalExamsPendingGrading { get; set; }
    }

    public class ExamScoreInfoDto
    {
        public string ExamTitle { get; set; } = null!;
        public decimal ScorePercentage { get; set; }
        public decimal FinalScore { get; set; }
        public decimal ExamTotalPoints { get; set; }
    }

    public class StudentScoreStatisticsDto
    {
        public decimal? AverageScorePercentage { get; set; }
        public decimal? HighestScorePercentage { get; set; }
        public decimal? LowestScorePercentage { get; set; }
        public ExamScoreInfoDto? HighestScoringExam { get; set; }
        public ExamScoreInfoDto? LowestScoringExam { get; set; }
    }

    public class StudentIntegrityStatisticsDto
    {
        public int TotalViolationsAcrossAllExams { get; set; }
        public decimal AverageViolationsPerExam { get; set; }
        public int CleanExams { get; set; }
        public int WarningExams { get; set; }
        public int FlaggedExams { get; set; }
    }

    public class StudentTimeStatisticsDto
    {
        public int AverageTimeSpentSeconds { get; set; }
        public int TotalTimeSpentSeconds { get; set; }
    }

    public class StudentStatisticsResponseDto
    {
        public StudentOverviewDto Overview { get; set; } = new();
        public StudentScoreStatisticsDto ScoreStatistics { get; set; } = new();
        public StudentIntegrityStatisticsDto IntegrityStatistics { get; set; } = new();
        public StudentTimeStatisticsDto TimeStatistics { get; set; } = new();
    }
}
