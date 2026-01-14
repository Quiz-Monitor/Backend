using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using QuizMonitor.DAL.Data;
using QuizMonitor.DAL.Interfaces;
using QuizMonitor.DAL.Models;

namespace QuizMonitor.DAL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly QuizMonitorDbContext _context;
        private IDbContextTransaction? _transaction;
        
        // Repository instances
        private IGenericRepository<User>? _users;
        private IGenericRepository<Exam>? _exams;
        private IGenericRepository<Question>? _questions;
        private IGenericRepository<Choice>? _choices;
        private IGenericRepository<ExamAttempt>? _examAttempts;
        private IGenericRepository<QuestionAnswer>? _questionAnswers;
        private IGenericRepository<ViolationEvent>? _violationEvents;
        private IGenericRepository<AnswerViolation>? _answerViolations;
        private IGenericRepository<Notification>? _notifications;
        private IGenericRepository<UserNotification>? _userNotifications;

        public UnitOfWork(QuizMonitorDbContext context)
        {
            _context = context;
        }

        // Lazy initialization of repositories
        public IGenericRepository<User> Users
        {
            get { return _users ??= new GenericRepository<User>(_context); }
        }

        public IGenericRepository<Exam> Exams
        {
            get { return _exams ??= new GenericRepository<Exam>(_context); }
        }

        public IGenericRepository<Question> Questions
        {
            get { return _questions ??= new GenericRepository<Question>(_context); }
        }

        public IGenericRepository<Choice> Choices
        {
            get { return _choices ??= new GenericRepository<Choice>(_context); }
        }

        public IGenericRepository<ExamAttempt> ExamAttempts
        {
            get { return _examAttempts ??= new GenericRepository<ExamAttempt>(_context); }
        }

        public IGenericRepository<QuestionAnswer> QuestionAnswers
        {
            get { return _questionAnswers ??= new GenericRepository<QuestionAnswer>(_context); }
        }

        public IGenericRepository<ViolationEvent> ViolationEvents
        {
            get { return _violationEvents ??= new GenericRepository<ViolationEvent>(_context); }
        }

        public IGenericRepository<AnswerViolation> AnswerViolations
        {
            get { return _answerViolations ??= new GenericRepository<AnswerViolation>(_context); }
        }

        public IGenericRepository<Notification> Notifications
        {
            get { return _notifications ??= new GenericRepository<Notification>(_context); }
        }

        public IGenericRepository<UserNotification> UserNotifications
        {
            get { return _userNotifications ??= new GenericRepository<UserNotification>(_context); }
        }

        // Save changes
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        // Transaction support
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Dispose
        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
