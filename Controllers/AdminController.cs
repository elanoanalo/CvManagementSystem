using CvManagementSystem.Data;
using CvManagementSystem.Entities;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            var projectCounts = await _context.Projects
                .GroupBy(p => p.CandidateId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var cvCounts = await _context.Cvs
                .GroupBy(c => c.CandidateId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var positionCounts = await _context.Positions
                .GroupBy(p => p.CreatedByUserId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var attributeCounts = await _context.AttributeValues
                .GroupBy(av => av.CandidateId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var result = new List<AdminUserListViewModel>();

            foreach (var u in users)
            {
                result.Add(new AdminUserListViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt,
                    ProjectsCount = projectCounts.GetValueOrDefault(u.Id),
                    CvsCount = cvCounts.GetValueOrDefault(u.Id),
                    PositionsCount = positionCounts.GetValueOrDefault(u.Id),
                    FilledAttributesCount = attributeCounts.GetValueOrDefault(u.Id)
                });
            }

            return View(result);
        }

        // GET: /Admin/UserDetails/id
        public async Task<IActionResult> UserDetails(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var model = new AdminUserDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Location = user.Location,
                PhotoUrl = user.PhotoUrl
            };

            var values = await _context.AttributeValues
                .Include(av => av.AttributeDefinition)
                .Include(av => av.SelectedOption)
                .Where(av => av.CandidateId == id)
                .ToListAsync();

            foreach (var v in values)
            {
                if (v.AttributeDefinition == null) continue;

                model.Attributes.Add(new AdminAttributeItem
                {
                    Name = v.AttributeDefinition.Name,
                    Type = v.AttributeDefinition.Type,
                    DisplayValue = FormatValue(v, v.AttributeDefinition.Type)
                });
            }

            var projects = await _context.Projects
                .Include(p => p.Tags)
                .Where(p => p.CandidateId == id)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            foreach (var p in projects)
            {
                model.Projects.Add(new AdminProjectItem
                {
                    Title = p.Title,
                    Description = p.Description,
                    StartDate = p.StartDate?.ToString("dd.MM.yyyy"),
                    EndDate = p.EndDate?.ToString("dd.MM.yyyy"),
                    Tags = p.Tags.Select(t => t.Tag).ToList()
                });
            }

            var cvs = await _context.Cvs
                .Include(c => c.Position)
                .Where(c => c.CandidateId == id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            foreach (var c in cvs)
            {
                model.Cvs.Add(new AdminCvItem
                {
                    Id = c.Id,
                    PositionTitle = c.Position?.Title ?? "—",
                    CreatedAt = c.CreatedAt
                });
            }

            var positions = await _context.Positions
                .Where(p => p.CreatedByUserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            foreach (var p in positions)
            {
                model.CreatedPositions.Add(new AdminPositionItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    IsPublished = p.IsPublished
                });
            }

            return View(model);
        }

        private static string? FormatValue(AttributeValue v, AttributeType type)
        {
            switch (type)
            {
                case AttributeType.String:
                case AttributeType.Text:
                    return v.ValueString;

                case AttributeType.Numeric:
                    return v.ValueNumber?.ToString("G29");

                case AttributeType.Date:
                    return v.ValueDate?.ToString("dd.MM.yyyy");

                case AttributeType.Boolean:
                    return v.ValueBoolean.HasValue ? (v.ValueBoolean.Value ? "✓" : "✗") : null;

                case AttributeType.Image:
                    return v.ValueImageUrl;

                case AttributeType.Period:
                    if (v.PeriodStart.HasValue || v.PeriodEnd.HasValue)
                    {
                        var start = v.PeriodStart?.ToString("dd.MM.yyyy") ?? "?";
                        var end = v.PeriodEnd?.ToString("dd.MM.yyyy") ?? "...";
                        return $"{start} — {end}";
                    }
                    return null;

                case AttributeType.Dropdown:
                    return v.SelectedOption?.Value;

                default:
                    return null;
            }
        }
    }
}