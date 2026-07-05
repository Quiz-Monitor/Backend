namespace QuizMonitor.BLL.DTOs
{
    public class ReviewChoiceDto
    {
        public int ChoiceId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }
    }
}
