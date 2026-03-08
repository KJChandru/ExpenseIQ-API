using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSpend.API.DTOs;
using SmartSpend.API.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSpend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
                return userId;
            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetExpenses([FromQuery] int? walletId, [FromQuery] int? categoryId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? search, [FromQuery] string? transactionType)
        {
            var userId = GetUserId();
            var expenses = await _expenseService.GetExpensesAsync(userId, walletId, categoryId, startDate, endDate, search, transactionType);
            return Ok(expenses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDto request)
        {
            var userId = GetUserId();
            try
            {
                var expense = await _expenseService.CreateExpenseAsync(userId, request);

                if (expense == null)
                    return BadRequest("Failed to create expense. Check if wallet and category are valid.");

                return CreatedAtAction(nameof(GetExpenses), new { id = expense.ExpenseId }, expense);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseDto request)
        {
            var userId = GetUserId();
            try
            {
                var expense = await _expenseService.UpdateExpenseAsync(userId, id, request);

                if (expense == null)
                    return NotFound("Expense not found or invalid inputs.");

                return Ok(expense);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var userId = GetUserId();
            var success = await _expenseService.DeleteExpenseAsync(userId, id);

            if (!success)
                return NotFound("Expense not found.");

            return NoContent();
        }
    }
}
