namespace QuizMonitor.BLL.DTOs
{
    public class ChoiceDto
    {
        public int? ChoiceId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int OrderNumber { get; set; }
    }
}
