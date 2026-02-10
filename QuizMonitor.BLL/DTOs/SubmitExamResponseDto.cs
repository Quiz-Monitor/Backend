using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.DTOs
{
    public class SubmitExamResponseDto
    {
        public string Status { get; set; } = string.Empty;
        public decimal McqScore { get; set; }
        public decimal? ManualScore { get; set; }
        public decimal FinalScore { get; set; }
        public int TotalViolations { get; set; }
        public string CheatingStatus { get; set; } = string.Empty;

    }
}