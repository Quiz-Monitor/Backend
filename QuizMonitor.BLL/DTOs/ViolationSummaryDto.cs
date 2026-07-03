namespace QuizMonitor.BLL.DTOs
{
    public class ViolationSummaryDto
    {
        public int TabSwitch        { get; set; }
        public int EyeAway          { get; set; }   // AI model: gaze_away
        public int MultiplePersons  { get; set; }   // AI model: multiple_persons
        public int ObjectDetected   { get; set; }   // AI model: object_detected
        public int FaceMissing      { get; set; }   // AI model: face_missing
        public int LowVisibility    { get; set; }   // AI model: low_visibility
        public int SuspiciousObject { get; set; }   // AI model: suspicious_object
    }
}
