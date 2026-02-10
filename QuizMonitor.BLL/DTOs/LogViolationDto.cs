using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.DTOs
{
    public class LogViolationDto
    {
        public int? QuestionId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DurationSeconds { get; set; }
        public string? ScreenshotUrl { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

    }
}