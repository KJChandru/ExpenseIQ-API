using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSpend.API.Data;
using System.Threading.Tasks;

namespace SmartSpend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CurrenciesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CurrenciesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrencies()
        {
            var currencies = await _context.Currencies
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.CurrencyId,
                    c.Code,
                    c.Name,
                    c.Symbol,
                    c.Flag
                })
                .ToListAsync();

            return Ok(currencies);
        }
    }
}
