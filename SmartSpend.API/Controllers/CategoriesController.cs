using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSpend.API.Data;
using SmartSpend.API.DTOs;
using SmartSpend.API.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSpend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var userId = GetUserId();
            var categories = await _context.Categories
                .Where(c => c.IsDefault || c.UserId == userId)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Icon = c.Icon,
                    Color = c.Color,
                    IsDefault = c.IsDefault
                })
                .OrderBy(c => !c.IsDefault)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto request)
        {
            var userId = GetUserId();
            var category = new Category
            {
                UserId = userId,
                Name = request.Name,
                Icon = request.Icon ?? "📂",
                Color = request.Color ?? "#6366f1",
                IsDefault = false
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategories), new { id = category.CategoryId }, new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Icon = category.Icon,
                Color = category.Color,
                IsDefault = category.IsDefault
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto request)
        {
            var userId = GetUserId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && c.UserId == userId && !c.IsDefault);
            if (category == null) return NotFound("Category not found or is a system category.");

            category.Name = request.Name;
            category.Icon = request.Icon ?? category.Icon;
            category.Color = request.Color ?? category.Color;
            await _context.SaveChangesAsync();

            return Ok(new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Icon = category.Icon,
                Color = category.Color,
                IsDefault = category.IsDefault
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = GetUserId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id && c.UserId == userId && !c.IsDefault);
            if (category == null) return NotFound("Category not found or is a system category.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
