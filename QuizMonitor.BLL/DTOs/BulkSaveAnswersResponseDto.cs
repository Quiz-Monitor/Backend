using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class BulkSaveAnswersResponseDto
    {
        /// <summary>Total number of answers processed</summary>
        public int TotalAnswered { get; set; }

        /// <summary>Combined score of all auto-graded (MCQ) answers</summary>
        public decimal TotalScore { get; set; }

        /// <summary>Per-answer result, in the same order as the request list</summary>
        public List<SaveAnswerResponseDto> Results { get; set; } = new();
    }
}
