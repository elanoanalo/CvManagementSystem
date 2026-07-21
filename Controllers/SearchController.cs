using CvManagementSystem.Data;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Search?query=...
        public async Task<IActionResult> Index(string? query)
        {
            var model = new SearchResultsViewModel
            {
                Query = query ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(query))
                return View(model);

            // Язык для морфологии
            var currentLang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            var config = currentLang == "ru" ? "russian" : "english";

            // === ПОЗИЦИИ ===
            var positions = await _context.Positions
                .Include(p => p.Tags)
                .Where(p => p.IsPublished &&
                    EF.Functions.ToTsVector(config, p.Title + " " + (p.Description ?? ""))
                        .Matches(EF.Functions.PlainToTsQuery(config, query)))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            foreach (var p in positions)
            {
                model.Positions.Add(new SearchPositionItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Tags = p.Tags.Select(t => t.Tag).ToList()
                });
            }

            // === АТРИБУТЫ ===
            var attributes = await _context.AttributeDefinitions
                .Where(a =>
                    EF.Functions.ToTsVector(config, a.Name + " " + (a.Description ?? ""))
                        .Matches(EF.Functions.PlainToTsQuery(config, query)))
                .OrderBy(a => a.Name)
                .Take(20)
                .ToListAsync();

            foreach (var a in attributes)
            {
                model.Attributes.Add(new SearchAttributeItem
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description
                });
            }

            return View(model);
        }
    }
}