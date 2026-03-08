using SmartSpend.API.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSpend.API.Services
{
    public interface IWalletService
    {
        Task<IEnumerable<WalletDto>> GetUserWalletsAsync(int userId);
        Task<WalletDto> CreateWalletAsync(int userId, CreateWalletDto request);
        Task<WalletDto> UpdateWalletAsync(int userId, int walletId, UpdateWalletDto request);
        Task<bool> DeleteWalletAsync(int userId, int walletId);
    }
}
