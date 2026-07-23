using CvManagementSystem.Data;
using CvManagementSystem.Entities;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CvManagementSystem.Controllers
{
    [Authorize]
    public class CvController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<CvManagementSystem.LocalizationMarker> _L;

        public CvController(AppDbContext context, UserManager<User> userManager, IStringLocalizer<CvManagementSystem.LocalizationMarker> localizer)
        {
            _context = context;
            _userManager = userManager;
            _L = localizer;
        }

        // GET: /Cv — список CV кандидата
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> Index()
        {
            var result = new List<CvListViewModel>();
            var userIdString = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdString)) return Challenge();

            var currentUserId = Guid.Parse(userIdString);

            var cvs = await _context.Cvs
                .Include(cv => cv.Position)
                    .ThenInclude(p => p!.PositionAttributes)
                .Include(cv => cv.Position)
                    .ThenInclude(p => p!.Tags)
                .Where(cv => cv.CandidateId == currentUserId)
                .OrderByDescending(cv => cv.CreatedAt)
                .ToListAsync();

            var allProjects = await _context.Projects
                .Include(p => p.Tags)
                .Where(p => p.CandidateId == currentUserId)
                .ToListAsync();

            foreach (var cv in cvs)
            {
                // Защита от null, если Position вдруг не загрузился
                var positionTags = cv.Position?.Tags?
                    .Select(t => t.Tag)
                    .ToList() ?? new List<string>();

                int projectsCount;

                if (!positionTags.Any())
                {
                    projectsCount = Math.Min(allProjects.Count, cv.Position?.MaxProjectsInCv ?? 3);
                }
                else
                {
                    projectsCount = 0;
                    foreach (var project in allProjects)
                    {
                        var projectTags = project.Tags?.Select(t => t.Tag).ToList() ?? new List<string>();
                        var hasMatch = projectTags.Any(pt => positionTags.Contains(pt));
                        if (hasMatch) projectsCount++;
                    }
                    projectsCount = Math.Min(projectsCount, cv.Position?.MaxProjectsInCv ?? 3);
                }

                result.Add(new CvListViewModel
                {
                    Id = cv.Id,
                    PositionTitle = cv.Position?.Title ?? "Без названия",
                    CreatedAt = cv.CreatedAt,
                    AttributesCount = cv.Position?.PositionAttributes?.Count ?? 0,
                    ProjectsCount = projectsCount
                });
            }
            return View(result);
        }

        // GET: /Cv/Positions — список позиций для кандидата
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> Positions()
        {
            var userIdString = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdString)) return Challenge();

            var currentUserId = Guid.Parse(userIdString);

            var positions = await _context.Positions
                .Include(p => p.Tags)
                .Include(p => p.PositionAttributes)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var existingCvs = await _context.Cvs
                .Where(cv => cv.CandidateId == currentUserId)
                .ToListAsync();

            var result = new List<PositionForCandidateViewModel>();

            foreach (var position in positions)
            {
                var existingCv = existingCvs
                    .FirstOrDefault(cv => cv.PositionId == position.Id);

                result.Add(new PositionForCandidateViewModel
                {
                    Id = position.Id,
                    Title = position.Title,
                    Description = position.Description,
                    Tags = position.Tags?.Select(t => t.Tag).ToList() ?? new List<string>(),
                    AttributesCount = position.PositionAttributes?.Count ?? 0,
                    AlreadyHasCv = existingCv != null,
                    ExistingCvId = existingCv?.Id
                });
            }

            return View(result);
        }

        // POST: /Cv/Create — создаём CV для позиции
        [HttpPost]
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> Create(Guid positionId)
        {
            var userIdString = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdString)) return Challenge();

            var currentUserId = Guid.Parse(userIdString);

            var position = await _context.Positions
                .FirstOrDefaultAsync(p => p.Id == positionId && p.IsPublished);

            if (position == null)
                return NotFound();

            var existingCv = await _context.Cvs
                .FirstOrDefaultAsync(cv =>
                    cv.CandidateId == currentUserId &&
                    cv.PositionId == positionId);

            if (existingCv != null)
                return RedirectToAction(nameof(ViewCv), new { id = existingCv.Id });

            var cv = new Cv
            {
                CandidateId = currentUserId,
                PositionId = positionId
            };

            _context.Cvs.Add(cv);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewCv), new { id = cv.Id });
        }

        // GET: /Cv/View/id — просмотр CV
        [HttpGet]
        public async Task<IActionResult> ViewCv(Guid id)
        {
            var userIdString = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdString)) return Challenge();

            var currentUserId = Guid.Parse(userIdString);
            var isRecruiter = User.IsInRole("Recruiter") || User.IsInRole("Administrator");

            var cv = await _context.Cvs
                .Include(c => c.Candidate)
                .Include(c => c.Position)
                    .ThenInclude(p => p!.PositionAttributes)
                        .ThenInclude(pa => pa.AttributeDefinition)
                            .ThenInclude(ad => ad!.Options)
                .Include(c => c.Position)
                    .ThenInclude(p => p!.Tags)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cv == null)
                return NotFound();

            if (!isRecruiter && cv.CandidateId != currentUserId)
                return Forbid();

            var attributeValues = await _context.AttributeValues
                .Include(av => av.SelectedOption)
                .Where(av => av.CandidateId == cv.CandidateId)
                .ToListAsync();

            var model = new CvViewModel
            {
                Id = cv.Id,
                CandidateFullName = cv.Candidate?.FullName ?? "Неизвестный кандидат",
                CandidateEmail = cv.Candidate?.Email ?? string.Empty,
                PositionTitle = cv.Position?.Title ?? "Без названия",
                PositionDescription = cv.Position?.Description,
                CreatedAt = cv.CreatedAt,
                FirstName = cv.Candidate?.FirstName,
                LastName = cv.Candidate?.LastName,
                Location = cv.Candidate?.Location,
                PhotoUrl = cv.Candidate?.PhotoUrl
            };

            // Безопасно обходим атрибуты
            var positionAttributes = cv.Position?.PositionAttributes?
                .OrderBy(pa => pa.DisplayOrder) ?? Enumerable.Empty<PositionAttribute>();

            foreach (var pa in positionAttributes)
            {
                var attrDef = pa.AttributeDefinition;
                if (attrDef == null) continue; // Если определения нет, пропускаем

                var value = attributeValues
                    .FirstOrDefault(av => av.AttributeDefinitionId == attrDef.Id);

                var displayValue = GetDisplayValue(value, attrDef.Type);

                model.Attributes.Add(new CvAttributeViewModel
                {
                    Name = attrDef.Name,
                    Description = attrDef.Description,
                    Type = attrDef.Type,
                    IsRequired = pa.IsRequired,
                    DisplayValue = displayValue,
                    HasValue = displayValue != null
                });
            }

            var positionTags = cv.Position?.Tags?.Select(t => t.Tag).ToList() ?? new List<string>();

            var candidateProjects = await _context.Projects
                .Include(p => p.Tags)
                .Where(p => p.CandidateId == cv.CandidateId)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            var filteredProjects = new List<Project>();

            if (!positionTags.Any())
            {
                filteredProjects = candidateProjects;
            }
            else
            {
                foreach (var project in candidateProjects)
                {
                    var projectTags = project.Tags?.Select(t => t.Tag).ToList() ?? new List<string>();
                    var hasMatchingTag = projectTags.Any(pt => positionTags.Contains(pt));

                    if (hasMatchingTag)
                        filteredProjects.Add(project);
                }
            }

            foreach (var project in filteredProjects.Take(cv.Position?.MaxProjectsInCv ?? 3))
            {
                model.Projects.Add(new CvProjectViewModel
                {
                    Title = project.Title,
                    Description = project.Description,
                    StartDate = project.StartDate?.ToString("dd.MM.yyyy"),
                    EndDate = project.EndDate?.ToString("dd.MM.yyyy") ?? "н.в.",
                    Tags = project.Tags?.Select(t => t.Tag).ToList() ?? new List<string>()
                });
            }

            return View(model);
        }

        // POST: /Cv/Delete/id
        [HttpPost]
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdString = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdString)) return Challenge();

            var currentUserId = Guid.Parse(userIdString);
            var isAdmin = User.IsInRole("Administrator");
            var cv = await _context.Cvs
                .FirstOrDefaultAsync(c => c.Id == id && (c.CandidateId == currentUserId || isAdmin));

            if (cv == null) return NotFound();

            _context.Cvs.Remove(cv);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> DeleteMultiple(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return RedirectToAction(nameof(Index));

            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);
            var isAdmin = User.IsInRole("Administrator");

            var cvs = await _context.Cvs
                .Where(cv => ids.Contains(cv.Id) && (cv.CandidateId == currentUserId || isAdmin))
                .ToListAsync();

            _context.Cvs.RemoveRange(cvs);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Cv/CreateConfirm — страница подтверждения создания CV
        [HttpGet]
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> CreateConfirm(Guid positionId)
        {
            var position = await _context.Positions
                .Include(p => p.PositionAttributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == positionId && p.IsPublished);

            if (position == null)
                return NotFound();

            return View(new PositionForCandidateViewModel
            {
                Id = position.Id,
                Title = position.Title,
                Description = position.Description,
                Tags = position.Tags?.Select(t => t.Tag).ToList() ?? new List<string>(),
                AttributesCount = position.PositionAttributes?.Count ?? 0
            });
        }

        private string? GetDisplayValue(AttributeValue? value, AttributeType type)
        {
            if (value == null) return null;

            switch (type)
            {
                case AttributeType.String:
                case AttributeType.Text:
                    return string.IsNullOrEmpty(value.ValueString) ? null : value.ValueString;

                case AttributeType.Numeric:
                    return value.ValueNumber?.ToString("G29");

                case AttributeType.Date:
                    return value.ValueDate?.ToString("d", System.Globalization.CultureInfo.CurrentCulture);

                case AttributeType.Boolean:
                    // Используем _L для перевода "Да"/"Нет"
                    return value.ValueBoolean.HasValue
                        ? (value.ValueBoolean.Value ? _L["Yes"].Value : _L["No"].Value)
                        : null;

                case AttributeType.Image:
                    return string.IsNullOrEmpty(value.ValueImageUrl) ? null : value.ValueImageUrl;

                case AttributeType.Period:
                    if (value.PeriodStart.HasValue || value.PeriodEnd.HasValue)
                    {
                        var start = value.PeriodStart?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? "?";
                        // Используем _L для перевода "н.в."
                        var end = value.PeriodEnd?.ToString("d", System.Globalization.CultureInfo.CurrentCulture) ?? _L["PresentTime"].Value;
                        return $"{start} — {end}";
                    }
                    return null;

                case AttributeType.Dropdown:
                    return value.SelectedOption?.Value;

                default:
                    return null;
            }
        }
    }
}