using System;

namespace QuizMonitor.BLL.DTOs
{
    public class UpdateExamDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool? CameraRequired { get; set; }
        public bool? TabSwitchingDetection { get; set; }
        public bool? EyeTrackingEnabled { get; set; }
        public bool? MultiplePersonDetection { get; set; }
        public int? MaxTabSwitches { get; set; }
        public int? MaxEyeAwaySeconds { get; set; }
    }
}
