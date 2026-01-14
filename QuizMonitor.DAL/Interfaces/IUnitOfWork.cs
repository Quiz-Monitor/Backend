using System;
using System.Threading.Tasks;
using QuizMonitor.DAL.Models;

namespace QuizMonitor.DAL.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository properties for each entity
        IGenericRepository<User> Users { get; }
        IGenericRepository<Exam> Exams { get; }
        IGenericRepository<Question> Questions { get; }
        IGenericRepository<Choice> Choices { get; }
        IGenericRepository<ExamAttempt> ExamAttempts { get; }
        IGenericRepository<QuestionAnswer> QuestionAnswers { get; }
        IGenericRepository<ViolationEvent> ViolationEvents { get; }
        IGenericRepository<AnswerViolation> AnswerViolations { get; }
        IGenericRepository<Notification> Notifications { get; }
        IGenericRepository<UserNotification> UserNotifications { get; }
        
        // Save changes
        Task<int> SaveChangesAsync();
        int SaveChanges();
        
        // Transaction support
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
