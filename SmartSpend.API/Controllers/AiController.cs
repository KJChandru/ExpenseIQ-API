using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartSpend.API.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartSpend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public AiController(ApplicationDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        private int GetUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var uid) ? uid : 0;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request)
        {
            var userId = GetUserId();
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
                return BadRequest(new { reply = "Gemini API key not configured. Please add it to appsettings.json." });

            // ── Build Financial Context ──
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var wallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .Select(w => new { w.Name, w.Balance, w.WalletType })
                .ToListAsync();

            var recentExpenses = await _context.Expenses
                .Where(e => e.UserId == userId && e.ExpenseDate >= monthStart)
                .Include(e => e.Category)
                .OrderByDescending(e => e.ExpenseDate)
                .Take(20)
                .Select(e => new
                {
                    e.Amount,
                    e.TransactionType,
                    Category = e.Category != null ? e.Category.Name : "Uncategorized",
                    e.Description,
                    Date = e.ExpenseDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            var spending = recentExpenses.Where(e => e.TransactionType == "Expense").Sum(e => e.Amount);
            var income   = recentExpenses.Where(e => e.TransactionType == "Income").Sum(e => e.Amount);

            var categoryBreakdown = recentExpenses
                .Where(e => e.TransactionType == "Expense")
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            var user = await _context.Users.Include(u => u.Currency).FirstOrDefaultAsync(u => u.UserId == userId);
            var currencyCode = user?.Currency?.Code ?? "INR";

            // ── System Prompt with Financial Context ──
            var systemPrompt = $@"You are SmartSpend AI, a helpful personal finance assistant embedded inside the SmartSpend expense management app.
You have access to the user's real financial data for this month ({now:MMMM yyyy}). Use it to give smart, personalized insights.

## User Financial Data ({currencyCode}):
**Wallets:**
{string.Join("\n", wallets.Select(w => $"- {w.Name} ({w.WalletType}): {currencyCode} {w.Balance:F2}"))}

**This Month ({now:MMMM yyyy}):**
- Total Income: {currencyCode} {income:F2}
- Total Spending: {currencyCode} {spending:F2}
- Net Savings: {currencyCode} {(income - spending):F2}

**Spending by Category:**
{string.Join("\n", categoryBreakdown.Select(c => $"- {c.Category}: {currencyCode} {c.Total:F2}"))}

**Recent Transactions (last 20):**
{string.Join("\n", recentExpenses.Take(10).Select(e => $"- [{e.Date}] {e.TransactionType}: {currencyCode} {e.Amount:F2} ({e.Category}) - {e.Description}"))}

## Instructions:
- Be concise, friendly, and financially insightful.
- Use {currencyCode} when mentioning amounts.
- Give specific answers based on the data above when possible.
- If asked about something not in the data, say so honestly.
- Format structured responses with bullet points.
- Keep replies under 200 words unless user asks for detail.";

            // ── Call Gemini API ──
            var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            // ── Build Financial Context ──
            var fullMessage = $"{systemPrompt}\n\n---\nUser question: {request.Message}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = fullMessage } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 512
                }
            };

            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(geminiUrl, content);
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { reply = $"Failed to reach Gemini: {ex.Message}" });
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { reply = $"Gemini error: {err}" });
            }

            var resultJson = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(resultJson);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "Sorry, I couldn't generate a response.";

            return Ok(new { reply = text });
        }
    }

    public class AiChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
    }
}
