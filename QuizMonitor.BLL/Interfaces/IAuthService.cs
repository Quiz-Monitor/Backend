using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.DAL.Models;

namespace QuizMonitor.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> RegisterAsync(RegisterDTO dto);
        Task<AuthResponseDTO> LoginAsync(LoginDTO dto);
        Task<AuthResponseDTO> RefreshTokenAsync(string refreshToken);
        Task<User?> GetUserByIdAsync(int userId);
    }
}
