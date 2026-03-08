using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSpend.API.Data;
using SmartSpend.API.DTOs;
using SmartSpend.API.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSpend.API.Controllers
{
    public class TransferDto
    {
        public int FromWalletId { get; set; }
        public int ToWalletId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ApplicationDbContext _context;

        public WalletsController(IWalletService walletService, ApplicationDbContext context)
        {
            _walletService = walletService;
            _context = context;
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId)) return userId;
            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserWallets()
        {
            var userId = GetUserId();
            var wallets = await _walletService.GetUserWalletsAsync(userId);
            return Ok(wallets);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletDto request)
        {
            var userId = GetUserId();
            var wallet = await _walletService.CreateWalletAsync(userId, request);
            if (wallet == null) return BadRequest("Failed to create wallet. Check if currency and wallet type are valid.");
            return CreatedAtAction(nameof(GetUserWallets), new { id = wallet.WalletId }, wallet);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWallet(int id, [FromBody] UpdateWalletDto request)
        {
            var userId = GetUserId();
            var wallet = await _walletService.UpdateWalletAsync(userId, id, request);
            if (wallet == null) return NotFound("Wallet not found or invalid inputs.");
            return Ok(wallet);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWallet(int id)
        {
            var userId = GetUserId();
            var success = await _walletService.DeleteWalletAsync(userId, id);
            if (!success) return NotFound("Wallet not found.");
            return NoContent();
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto request)
        {
            var userId = GetUserId();
            if (request.Amount <= 0) return BadRequest("Amount must be greater than 0.");
            if (request.FromWalletId == request.ToWalletId) return BadRequest("Source and destination wallets must be different.");

            var fromWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.WalletId == request.FromWalletId && w.UserId == userId && w.IsActive);
            var toWallet   = await _context.Wallets.FirstOrDefaultAsync(w => w.WalletId == request.ToWalletId && w.UserId == userId && w.IsActive);

            if (fromWallet == null || toWallet == null) return NotFound("One or both wallets not found.");
            if (fromWallet.Balance < request.Amount) return BadRequest("Insufficient balance.");

            fromWallet.Balance -= request.Amount;
            toWallet.Balance   += request.Amount;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Transferred {request.Amount} from {fromWallet.Name} to {toWallet.Name} successfully." });
        }
    }
}
