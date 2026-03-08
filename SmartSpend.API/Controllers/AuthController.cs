using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSpend.API.DTOs;
using SmartSpend.API.Services;
using System.Threading.Tasks;

namespace SmartSpend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null)
                return BadRequest("Registration failed.");

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized("Invalid credentials.");

            return Ok(result);
        }
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthDto request)
        {
            var result = await _authService.GoogleLoginAsync(request);
            if (result == null)
                return Unauthorized("Invalid Google token.");

            return Ok(result);
        }

        [HttpPut("currency")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrency([FromBody] UpdateCurrencyDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result = await _authService.UpdateCurrencyAsync(userId, dto.CurrencyId);
            if (result == null)
                return BadRequest("Failed to update currency.");
            return Ok(result);
        }

        [HttpDelete("reset")]
        [Authorize]
        public async Task<IActionResult> ResetData()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            await _authService.ResetUserDataAsync(userId);
            return Ok(new { message = "All your data has been reset successfully." });
        }
    }
}
