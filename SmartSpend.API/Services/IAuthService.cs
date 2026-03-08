using SmartSpend.API.DTOs;
using System.Threading.Tasks;

namespace SmartSpend.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto request);
        Task<AuthResponseDto> LoginAsync(LoginDto request);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleAuthDto request);
        Task<AuthResponseDto> UpdateCurrencyAsync(int userId, int currencyId);
        Task ResetUserDataAsync(int userId);
    }
}
