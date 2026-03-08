using Microsoft.EntityFrameworkCore;
using SmartSpend.API.Data;
using SmartSpend.API.DTOs;
using SmartSpend.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSpend.API.Services
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WalletDto>> GetUserWalletsAsync(int userId)
        {
            return await _context.Wallets
                .Include(w => w.Currency)
                .Where(w => w.UserId == userId && w.IsActive)
                .Select(w => new WalletDto
                {
                    WalletId = w.WalletId,
                    Name = w.Name,
                    WalletType = w.WalletType,
                    Balance = w.Balance,
                    CurrencyId = w.CurrencyId,
                    CurrencyCode = w.Currency!.Code,
                    CurrencySymbol = w.Currency!.Symbol
                })
                .ToListAsync();
        }

        public async Task<WalletDto> CreateWalletAsync(int userId, CreateWalletDto request)
        {
            var currency = await _context.Currencies.FindAsync(request.CurrencyId);
            if (currency == null || !currency.IsActive) return null!;

            var validTypes = new[] { "Cash", "CreditCard", "UPI" };
            if (string.IsNullOrEmpty(request.WalletType) || !validTypes.Contains(request.WalletType))
                return null!; // Invalid wallet type

            var wallet = new Wallet
            {
                UserId = userId,
                Name = request.Name!,
                WalletType = request.WalletType,
                Balance = request.OpeningBalance,
                CurrencyId = request.CurrencyId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return new WalletDto
            {
                WalletId = wallet.WalletId,
                Name = wallet.Name,
                WalletType = wallet.WalletType,
                Balance = wallet.Balance,
                CurrencyId = wallet.CurrencyId,
                CurrencyCode = currency.Code,
                CurrencySymbol = currency.Symbol
            };
        }

        public async Task<WalletDto> UpdateWalletAsync(int userId, int walletId, UpdateWalletDto request)
        {
            var wallet = await _context.Wallets
                .Include(w => w.Currency)
                .FirstOrDefaultAsync(w => w.WalletId == walletId && w.UserId == userId && w.IsActive);

            if (wallet == null) return null!;

            var currency = await _context.Currencies.FindAsync(request.CurrencyId);
            if (currency == null || !currency.IsActive) return null!;

            var validTypes = new[] { "Cash", "CreditCard", "UPI" };
            if (string.IsNullOrEmpty(request.WalletType) || !validTypes.Contains(request.WalletType))
                return null!; // Invalid wallet type

            wallet.Name = request.Name!;
            wallet.WalletType = request.WalletType;
            wallet.CurrencyId = request.CurrencyId;

            await _context.SaveChangesAsync();

            return new WalletDto
            {
                WalletId = wallet.WalletId,
                Name = wallet.Name,
                WalletType = wallet.WalletType,
                Balance = wallet.Balance,
                CurrencyId = wallet.CurrencyId,
                CurrencyCode = currency.Code,
                CurrencySymbol = currency.Symbol
            };
        }

        public async Task<bool> DeleteWalletAsync(int userId, int walletId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.WalletId == walletId && w.UserId == userId && w.IsActive);

            if (wallet == null) return false;

            // Soft delete
            wallet.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
