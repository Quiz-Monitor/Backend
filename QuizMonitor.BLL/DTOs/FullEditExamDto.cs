using System;
using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    /// <summary>
    /// Request body for PUT /api/exams/{examId}/full.
    /// All metadata fields are nullable (partial update — omit to keep existing value).
    /// The questions array represents the COMPLETE desired state:
    ///   with questionId   → update that question
    ///   without questionId → create new question
    ///   existing questions not in the array → soft-deleted
    /// </summary>
    public class FullEditExamDto
    {
        // ── Exam metadata (all nullable = partial update) ──────────────────
        public string?   Title                  { get; set; }
        public string?   Description            { get; set; }
        public int?      DurationMinutes        { get; set; }
        public DateTime? StartTime              { get; set; }
        public DateTime? EndTime                { get; set; }
        public bool?     CameraRequired         { get; set; }
        public bool?     TabSwitchingDetection  { get; set; }
        public bool?     EyeTrackingEnabled     { get; set; }
        public bool?     MultiplePersonDetection{ get; set; }
        public int?      MaxTabSwitches         { get; set; }
        public int?      MaxEyeAwaySeconds      { get; set; }

        // ── Questions — complete desired state (null = don't touch questions) ─
        public List<FullEditQuestionDto>? Questions { get; set; }
    }

    public class FullEditQuestionDto
    {
        /// <summary>null → create new question.  Has value → update that question.</summary>
        public int?    QuestionId      { get; set; }
        public string  QuestionType    { get; set; } = string.Empty;
        public string  QuestionText    { get; set; } = string.Empty;
        public string? QuestionImageUrl{ get; set; }
        public decimal Points          { get; set; }
        public int     OrderNumber     { get; set; }
        public bool    IsRequired      { get; set; } = true;

        /// <summary>
        /// Choices for MCQ questions.
        /// ChoiceId present → update.  ChoiceId absent → create.
        /// Existing choices not in list → hard-deleted (they have no soft-delete column).
        /// </summary>
        public List<ChoiceDto>? Choices { get; set; }
    }
}
