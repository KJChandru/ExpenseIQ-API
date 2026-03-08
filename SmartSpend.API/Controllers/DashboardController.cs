using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSpend.API.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSpend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
                return userId;
            return 0;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userId = GetUserId();
            var summary = await _dashboardService.GetSummaryAsync(userId);
            return Ok(summary);
        }

        [HttpGet("trend")]
        public async Task<IActionResult> GetMonthlyTrend()
        {
            var userId = GetUserId();
            var trend = await _dashboardService.GetMonthlyTrendAsync(userId);
            return Ok(trend);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategoryBreakdown()
        {
            var userId = GetUserId();
            var categories = await _dashboardService.GetCategoryBreakdownAsync(userId);
            return Ok(categories);
        }
    }
}
