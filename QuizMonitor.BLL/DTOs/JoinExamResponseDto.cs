using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.DTOs
{
    public class JoinExamResponseDto
    {
        public int ExamId {get; set;}
        public string InstructorName {get; set;} = string.Empty;
        public string Title {get; set;} = string.Empty;
        public string Status {get; set;} = string.Empty;
        public DateTime? StartTime {get; set;}
        public List<string> Rules {get; set;} = new();
    }
}