using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace QuizMonitor.DAL.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Get Operations
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        
        // Add Operations
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        
        // Update Operations
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        
        // Delete Operations
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);
        
        // Count Operations
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        
        // Exist Operations
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}
