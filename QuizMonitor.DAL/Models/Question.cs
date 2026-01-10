using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int ExamId { get; set; }

    public string QuestionType { get; set; } = null!;

    public string QuestionText { get; set; } = null!;

    public string? QuestionImageUrl { get; set; }

    public decimal Points { get; set; }

    public int OrderNumber { get; set; }

    public bool? IsRequired { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Choice> Choices { get; set; } = new List<Choice>();

    public virtual Exam Exam { get; set; } = null!;

    public virtual ICollection<QuestionAnswer> QuestionAnswers { get; set; } = new List<QuestionAnswer>();

    public virtual ICollection<ViolationEvent> ViolationEvents { get; set; } = new List<ViolationEvent>();
}
