using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSpend.API.DTOs;

namespace SmartSpend.API.Services
{
    public interface IRecurringExpenseService
    {
        Task<IEnumerable<RecurringExpenseDto>> GetRecurringExpensesAsync(int userId);
        Task<RecurringExpenseDto> GetRecurringExpenseByIdAsync(int userId, int recurringId);
        Task<RecurringExpenseDto> CreateRecurringExpenseAsync(int userId, CreateRecurringExpenseDto dto);
        Task<RecurringExpenseDto> UpdateRecurringExpenseAsync(int userId, int recurringId, UpdateRecurringExpenseDto dto);
        Task<bool> DeleteRecurringExpenseAsync(int userId, int recurringId);
        
        // Background process logically
        Task<int> ProcessDueExpensesAsync(int userId);
    }
}
