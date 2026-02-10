using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.DTOs
{
    public class LogViolationResponseDto
    {
        public int ViolationId { get; set; }
        public int TotalViolations { get; set; }
    }
}