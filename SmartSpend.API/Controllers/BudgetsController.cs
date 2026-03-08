using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSpend.API.DTOs;
using SmartSpend.API.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSpend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BudgetsController : ControllerBase
    {
        private readonly IBudgetService _budgetService;

        public BudgetsController(IBudgetService budgetService)
        {
            _budgetService = budgetService;
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
                return userId;
            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetBudgets([FromQuery] string month)
        {
            // Month format should be YYYY-MM
            if (string.IsNullOrEmpty(month))
                return BadRequest("Month parameter 'YYYY-MM' is required.");

            var userId = GetUserId();
            var budgets = await _budgetService.GetBudgetsAsync(userId, month);
            return Ok(budgets);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetDto request)
        {
            var userId = GetUserId();
            var budget = await _budgetService.CreateBudgetAsync(userId, request);

            if (budget == null)
                return BadRequest("Failed to create budget. Check if category is valid or if a budget already exists for this month.");

            return CreatedAtAction(nameof(GetBudgets), new { id = budget.BudgetId }, budget);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBudget(int id, [FromBody] UpdateBudgetDto request)
        {
            var userId = GetUserId();
            var budget = await _budgetService.UpdateBudgetAsync(userId, id, request);

            if (budget == null)
                return NotFound("Budget not found.");

            return Ok(budget);
        }
    }
}
