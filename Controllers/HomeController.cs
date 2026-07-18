using CvManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Загружаем всю статистику ОДНИМ запросом каждую
            // Не используем запросы внутри циклов — всё параллельно!
            var totalPositions = await _context.Positions
                .CountAsync(p => p.IsPublished);

            var totalCandidates = await _context.Users
                .CountAsync(u => u.Role == Entities.UserRole.Candidate);

            var totalRecruiters = await _context.Users
                .CountAsync(u => u.Role == Entities.UserRole.Recruiter);

            var totalCvs = await _context.Cvs.CountAsync();

            var last24Hours = DateTime.UtcNow.AddHours(-24);

            var newCvsLast24Hours = await _context.Cvs
                .CountAsync(cv => cv.CreatedAt >= last24Hours);

            var totalAttributes = await _context.AttributeDefinitions.CountAsync();

            // Топ тегов позиций — tag cloud
            var positionTags = await _context.PositionTags
                .GroupBy(t => t.Tag)
                .Select(g => new { Tag = g.Key, Count = g.Count() })
                .OrderByDescending(t => t.Count)
                .Take(20)
                .ToListAsync();

            // Последние 5 опубликованных позиций
            var recentPositions = await _context.Positions
                .Where(p => p.IsPublished)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalPositions = totalPositions;
            ViewBag.TotalCandidates = totalCandidates;
            ViewBag.TotalRecruiters = totalRecruiters;
            ViewBag.TotalCvs = totalCvs;
            ViewBag.NewCvsLast24Hours = newCvsLast24Hours;
            ViewBag.TotalAttributes = totalAttributes;
            ViewBag.PositionTags = positionTags;
            ViewBag.RecentPositions = recentPositions;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}