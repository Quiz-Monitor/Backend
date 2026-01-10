using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? ProfilePicture { get; set; }

    public DateTime? LastLogin { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public virtual User? DeletedByNavigation { get; set; }

    public virtual ICollection<ExamAttempt> ExamAttemptDeletedByNavigations { get; set; } = new List<ExamAttempt>();

    public virtual ICollection<ExamAttempt> ExamAttemptGradedByNavigations { get; set; } = new List<ExamAttempt>();

    public virtual ICollection<ExamAttempt> ExamAttemptStudents { get; set; } = new List<ExamAttempt>();

    public virtual ICollection<Exam> ExamDeletedByNavigations { get; set; } = new List<Exam>();

    public virtual ICollection<Exam> ExamInstructors { get; set; } = new List<Exam>();

    public virtual ICollection<User> InverseDeletedByNavigation { get; set; } = new List<User>();

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
}
