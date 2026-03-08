using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartSpend.API.Data;
using SmartSpend.API.DTOs;
using SmartSpend.API.Models;
using System.Linq;
using Google.Apis.Auth;

namespace SmartSpend.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return null!; // Email exists

            var currency = await _context.Currencies.FindAsync(request.CurrencyId);
            if (currency == null) return null!;

            var user = new User
            {
                FullName = request.FullName!,
                Email = request.Email!,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CurrencyId = request.CurrencyId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return new AuthResponseDto
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                DefaultCurrencyId = user.CurrencyId,
                CurrencyCode = currency.Code,
                CurrencySymbol = currency.Symbol
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            var user = await _context.Users
                .Include(u => u.Currency)
                .SingleOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null!;

            var token = GenerateJwtToken(user);
            return new AuthResponseDto
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                DefaultCurrencyId = user.CurrencyId,
                CurrencyCode = user.Currency?.Code ?? "USD",
                CurrencySymbol = user.Currency?.Symbol ?? "$"
            };
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleAuthDto request)
        {
            var clientId = _config["Google:ClientId"];
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException("Google:ClientId is missing in appsettings.");

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return null!; // Invalid token
            }

            var user = await _context.Users
                .Include(u => u.Currency)
                .SingleOrDefaultAsync(u => u.Email == payload.Email);

            if (user == null)
            {
                // Sign up flow
                var currencyId = 1; // Default to INR based on Phase 1 Seed (or any default)
                var currency = await _context.Currencies.FindAsync(currencyId);
                
                // Generate random password hash as google users don't have passwords
                var rng = new Random();
                var randomStr = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*", 32)
                  .Select(s => s[rng.Next(s.Length)]).ToArray());

                user = new User
                {
                    FullName = payload.Name,
                    Email = payload.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(randomStr),
                    CurrencyId = currencyId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                user.Currency = currency;
            }

            var token = GenerateJwtToken(user);
            return new AuthResponseDto
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                DefaultCurrencyId = user.CurrencyId,
                CurrencyCode = user.Currency?.Code ?? "USD",
                CurrencySymbol = user.Currency?.Symbol ?? "$"
            };
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyStr = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(keyStr)) throw new ArgumentNullException("Jwt:Key is missing");

            var key = Encoding.UTF8.GetBytes(keyStr);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim(ClaimTypes.Name, user.FullName!)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<AuthResponseDto> UpdateCurrencyAsync(int userId, int currencyId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null!;

            var currency = await _context.Currencies.FindAsync(currencyId);
            if (currency == null) return null!;

            user.CurrencyId = currencyId;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            return new AuthResponseDto
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                DefaultCurrencyId = user.CurrencyId,
                CurrencyCode = currency.Code,
                CurrencySymbol = currency.Symbol
            };
        }

        public async Task ResetUserDataAsync(int userId)
        {
            // Delete in dependency order
            var expenses = _context.Expenses.Where(e => e.UserId == userId);
            _context.Expenses.RemoveRange(expenses);

            var recurring = _context.RecurringExpenses.Where(r => r.UserId == userId);
            _context.RecurringExpenses.RemoveRange(recurring);

            var budgets = _context.Budgets.Where(b => b.UserId == userId);
            _context.Budgets.RemoveRange(budgets);

            var transfers = _context.WalletTransfers.Where(t =>
                _context.Wallets.Where(w => w.UserId == userId).Select(w => w.WalletId).Contains(t.FromWalletId) ||
                _context.Wallets.Where(w => w.UserId == userId).Select(w => w.WalletId).Contains(t.ToWalletId));
            _context.WalletTransfers.RemoveRange(transfers);

            var wallets = _context.Wallets.Where(w => w.UserId == userId);
            _context.Wallets.RemoveRange(wallets);

            var categories = _context.Categories.Where(c => c.UserId == userId);
            _context.Categories.RemoveRange(categories);

            await _context.SaveChangesAsync();
        }
    }
}
