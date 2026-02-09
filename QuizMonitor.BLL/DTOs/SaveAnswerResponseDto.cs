using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.DTOs
{
    public class SaveAnswerResponseDto
    {
        public int AnswerId {get; set;}
        public bool IsCorrect {get; set;}
        public decimal Score {get; set;}
    }
}