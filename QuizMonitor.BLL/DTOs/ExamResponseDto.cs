using System;

namespace QuizMonitor.BLL.DTOs
{
    public class ExamResponseDto
    {
        public int ExamId { get; set; }
        public string ExamCode { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool CameraRequired { get; set; }
        public bool TabSwitchingDetection { get; set; }
        public bool EyeTrackingEnabled { get; set; }
        public bool MultiplePersonDetection { get; set; }
        public int? MaxTabSwitches { get; set; }
        public int? MaxEyeAwaySeconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
