using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class ReviewQuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public decimal? EarnedPoints { get; set; }
        public int OrderNumber { get; set; }

        /// <summary>MCQ: selected choice text(s), comma-joined. Open-ended: AnswerText.</summary>
        public string? StudentAnswer { get; set; }

        /// <summary>MCQ: correct choice text(s), comma-joined. Open-ended: null.</summary>
        public string? CorrectAnswer { get; set; }

        /// <summary>MCQ only. Null for open-ended (manually graded).</summary>
        public bool? IsCorrect { get; set; }

        /// <summary>Open-ended only — instructor's written feedback.</summary>
        public string? InstructorFeedback { get; set; }

        /// <summary>MCQ only. Null for open-ended questions.</summary>
        public List<ReviewChoiceDto>? Choices { get; set; }
    }
}
