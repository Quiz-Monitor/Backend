using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class AnswerViolation
{
    public int AnswerViolationId { get; set; }

    public int AnswerId { get; set; }

    public int ViolationId { get; set; }

    public virtual QuestionAnswer Answer { get; set; } = null!;

    public virtual ViolationEvent Violation { get; set; } = null!;
}
