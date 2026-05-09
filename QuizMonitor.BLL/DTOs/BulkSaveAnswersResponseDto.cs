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

        /// <summary>
        /// Exam submission result — included automatically because this endpoint
        /// saves answers AND submits the exam in one call.
        /// Check SubmitResult.GradingStatus:
        ///   "auto_graded"            → all MCQ, FinalScore is ready now
        ///   "pending_manual_grading" → has short-answer, instructor must grade first
        /// </summary>
        public SubmitExamResponseDto SubmitResult { get; set; } = new();
    }
}
