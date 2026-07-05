using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class StudentExamResultDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal? FinalScore { get; set; }
        public string CheatingStatus { get; set; } = string.Empty;

        /// <summary>Total violations EXCLUDING object_detected (attempt.TotalViolations - ObjectDetectedCount)</summary>
        public int TotalViolations { get; set; }

        public int TabSwitchCount { get; set; }
        public int EyeAwayCount { get; set; }
        public int MultiplePersonCount { get; set; }
        public int FaceMissingCount { get; set; }
        public int LowVisibilityCount { get; set; }
        public int SuspiciousObjectCount { get; set; }

        /// <summary>Individual violation events for this student, excluding object_detected type</summary>
        public List<ViolationDetailDto> Violations { get; set; } = new();
    }
}
