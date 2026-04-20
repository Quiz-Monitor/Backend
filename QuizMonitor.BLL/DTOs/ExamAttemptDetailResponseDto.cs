using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class ExamAttemptDetailResponseDto
    {
        public List<QuestionDetailDto> Questions { get; set; } = new List<QuestionDetailDto>();
        public ViolationSummaryDto ViolationSummary { get; set; } = new ViolationSummaryDto();
    }
}
