namespace QuizMonitor.BLL.DTOs.NotificationDTOs
{
    public class NotificationPaginationDto
    {
        public int Total { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        public bool HasMore { get; set; }
    }
}
