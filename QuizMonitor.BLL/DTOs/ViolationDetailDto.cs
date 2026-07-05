using System;

namespace QuizMonitor.BLL.DTOs
{
    public class ViolationDetailDto
    {
        public int ViolationId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Description { get; set; }
        public int? DurationSeconds { get; set; }
        public string? ScreenshotUrl { get; set; }
    }
}
