using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSpend.API.DTOs;
using SmartSpend.API.Services;

namespace SmartSpend.API.Controllers
{
    [ApiController]
    [Route("api/recurring-expenses")]
    [Authorize]
    public class RecurringExpensesController : ControllerBase
    {
        private readonly IRecurringExpenseService _recurringService;

        public RecurringExpensesController(IRecurringExpenseService recurringService)
        {
            _recurringService = recurringService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        [HttpGet]
        public async Task<IActionResult> GetRecurringExpenses()
        {
            var userId = GetUserId();
            var expenses = await _recurringService.GetRecurringExpensesAsync(userId);
            return Ok(expenses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecurringExpense(int id)
        {
            var userId = GetUserId();
            var expense = await _recurringService.GetRecurringExpenseByIdAsync(userId, id);
            if (expense == null) return NotFound("Recurring expense not found.");
            return Ok(expense);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecurringExpense([FromBody] CreateRecurringExpenseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            try
            {
                var created = await _recurringService.CreateRecurringExpenseAsync(userId, dto);
                return CreatedAtAction(nameof(GetRecurringExpense), new { id = created.RecurringId }, created);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecurringExpense(int id, [FromBody] UpdateRecurringExpenseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            try
            {
                var updated = await _recurringService.UpdateRecurringExpenseAsync(userId, id, dto);
                if (updated == null) return NotFound("Recurring expense not found.");
                return Ok(updated);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecurringExpense(int id)
        {
            var userId = GetUserId();
            var deleted = await _recurringService.DeleteRecurringExpenseAsync(userId, id);
            if (!deleted) return NotFound("Recurring expense not found.");
            return NoContent();
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessDueExpenses()
        {
            try
            {
                var userId = GetUserId();
                int count = await _recurringService.ProcessDueExpensesAsync(userId);
                return Ok(new { message = $"Processed {count} due expenses.", processedCount = count });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
