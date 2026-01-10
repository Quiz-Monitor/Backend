using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class Choice
{
    public int ChoiceId { get; set; }

    public int QuestionId { get; set; }

    public string ChoiceText { get; set; } = null!;

    public bool? IsCorrect { get; set; }

    public int OrderNumber { get; set; }

    public virtual Question Question { get; set; } = null!;
}
