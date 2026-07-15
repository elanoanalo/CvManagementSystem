using CvManagementSystem.Data;
using CvManagementSystem.Entities;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers
{
    [Authorize]
    public class CvController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public CvController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Cv — список CV кандидата
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> Index()
        {
            var result = new List<CvListViewModel>();
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            // Загружаем всё что нужно ДО цикла — один раз!
            var cvs = await _context.Cvs
                .Include(cv => cv.Position)
                    .ThenInclude(p => p.PositionAttributes)
                .Include(cv => cv.Position)
                    .ThenInclude(p => p.Tags)
                .Where(cv => cv.CandidateId == currentUserId)
                .OrderByDescending(cv => cv.CreatedAt)
                .ToListAsync();

            // Загружаем ВСЕ проекты кандидата с тегами — один запрос
            var allProjects = await _context.Projects
                .Include(p => p.Tags)
                .Where(p => p.CandidateId == currentUserId)
                .ToListAsync();

            // Теперь цикл — никаких запросов к БД внутри!
            foreach (var cv in cvs)
            {
                var positionTags = cv.Position.Tags
                    .Select(t => t.Tag)
                    .ToList();

                int projectsCount;

                if (!positionTags.Any())
                {
                    projectsCount = Math.Min(allProjects.Count, 5);
                }
                else
                {
                    projectsCount = 0;
                    foreach (var project in allProjects)
                    {
                        var projectTags = project.Tags.Select(t => t.Tag).ToList();
                        var hasMatch = projectTags.Any(pt => positionTags.Contains(pt));
                        if (hasMatch) projectsCount++;
                    }
                    projectsCount = Math.Min(projectsCount, 5);
                }

                result.Add(new CvListViewModel
                {
                    Id = cv.Id,
                    PositionTitle = cv.Position.Title,
                    CreatedAt = cv.CreatedAt,
                    AttributesCount = cv.Position.PositionAttributes.Count,
                    ProjectsCount = projectsCount
                });
            }
            return View(result);
        }

        // GET: /Cv/Positions — список позиций для кандидата
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> Positions()
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            // Загружаем опубликованные позиции
            var positions = await _context.Positions
                .Include(p => p.Tags)
                .Include(p => p.PositionAttributes)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Загружаем CV кандидата чтобы знать для каких позиций уже есть CV
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
                    Tags = position.Tags.Select(t => t.Tag).ToList(),
                    AttributesCount = position.PositionAttributes.Count,
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
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            // Проверяем что позиция существует и опубликована
            var position = await _context.Positions
                .FirstOrDefaultAsync(p => p.Id == positionId && p.IsPublished);

            if (position == null)
                return NotFound();

            // Проверяем что CV ещё не создано
            var existingCv = await _context.Cvs
                .FirstOrDefaultAsync(cv =>
                    cv.CandidateId == currentUserId &&
                    cv.PositionId == positionId);

            if (existingCv != null)
                return RedirectToAction(nameof(View), new { id = existingCv.Id });

            // Создаём CV — только связь кандидат + позиция
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
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);
            var isRecruiter = User.IsInRole("Recruiter") || User.IsInRole("Administrator");

            // Загружаем CV
            var cv = await _context.Cvs
                .Include(c => c.Candidate)
                .Include(c => c.Position)
                    .ThenInclude(p => p.PositionAttributes)
                        .ThenInclude(pa => pa.AttributeDefinition)
                            .ThenInclude(ad => ad.Options)
                .Include(c => c.Position)
                    .ThenInclude(p => p.Tags)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cv == null)
                return NotFound();

            // Кандидат может видеть только своё CV
            if (!isRecruiter && cv.CandidateId != currentUserId)
                return Forbid();

            // Загружаем значения атрибутов кандидата
            var attributeValues = await _context.AttributeValues
                .Include(av => av.SelectedOption)
                .Where(av => av.CandidateId == cv.CandidateId)
                .ToListAsync();

            // Строим ViewModel
            var model = new CvViewModel
            {
                Id = cv.Id,
                CandidateFullName = cv.Candidate!.FullName,
                CandidateEmail = cv.Candidate.Email ?? string.Empty,
                PositionTitle = cv.Position.Title,
                PositionDescription = cv.Position.Description,
                CreatedAt = cv.CreatedAt
            };

            // Атрибуты — подтягиваем живьём из профиля
            foreach (var pa in cv.Position.PositionAttributes.OrderBy(pa => pa.DisplayOrder))
            {
                var attrDef = pa.AttributeDefinition!;

                // Ищем значение у кандидата
                var value = attributeValues
                    .FirstOrDefault(av => av.AttributeDefinitionId == attrDef.Id);

                // Конвертируем значение в строку для отображения
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

            // Проекты — фильтруем по тегам позиции
            var positionTags = cv.Position.Tags.Select(t => t.Tag).ToList();

            var candidateProjects = await _context.Projects
                .Include(p => p.Tags)
                .Where(p => p.CandidateId == cv.CandidateId)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            // Фильтруем проекты по тегам позиции
            // Если у позиции нет тегов — показываем все проекты
            var filteredProjects = new List<Project>();

            if (!positionTags.Any())
            {
                filteredProjects = candidateProjects;
            }
            else
            {
                foreach (var project in candidateProjects)
                {
                    var projectTags = project.Tags.Select(t => t.Tag).ToList();

                    // Проект подходит если хотя бы один тег совпадает с тегами позиции
                    var hasMatchingTag = projectTags
                        .Any(pt => positionTags.Contains(pt));

                    if (hasMatchingTag)
                        filteredProjects.Add(project);
                }
            }

            // Берём максимум 5 проектов (самые свежие)
            foreach (var project in filteredProjects.Take(5))
            {
                model.Projects.Add(new CvProjectViewModel
                {
                    Title = project.Title,
                    Description = project.Description,
                    StartDate = project.StartDate.HasValue
                        ? project.StartDate.Value.ToString("dd.MM.yyyy")
                        : null,
                    EndDate = project.EndDate.HasValue
                        ? project.EndDate.Value.ToString("dd.MM.yyyy")
                        : "н.в.",
                    Tags = project.Tags.Select(t => t.Tag).ToList()
                });
            }

            return View(model);
        }

        // POST: /Cv/Delete/id
        [HttpPost]
        [Authorize(Roles = "Candidate,Administrator")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var cv = await _context.Cvs
                .FirstOrDefaultAsync(c => c.Id == id && c.CandidateId == currentUserId);

            if (cv == null)
                return NotFound();

            _context.Cvs.Remove(cv);
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
                Tags = position.Tags.Select(t => t.Tag).ToList(),
                AttributesCount = position.PositionAttributes.Count
            });
        }

        // Приватный метод — конвертирует значение атрибута в строку для отображения
        // Вся логика здесь, в контроллере — View просто показывает строку
        private string? GetDisplayValue(AttributeValue? value, AttributeType type)
        {
            if (value == null) return null;

            switch (type)
            {
                case AttributeType.String:
                case AttributeType.Text:
                    return string.IsNullOrEmpty(value.ValueString) ? null : value.ValueString;

                case AttributeType.Numeric:
                    return value.ValueNumber.HasValue
                        ? value.ValueNumber.Value.ToString("G29")
                        : null;

                case AttributeType.Date:
                    return value.ValueDate.HasValue
                        ? value.ValueDate.Value.ToString("dd.MM.yyyy")
                        : null;

                case AttributeType.Boolean:
                    return value.ValueBoolean.HasValue
                        ? (value.ValueBoolean.Value ? "Да" : "Нет")
                        : null;

                case AttributeType.Image:
                    return string.IsNullOrEmpty(value.ValueImageUrl)
                        ? null
                        : value.ValueImageUrl;

                case AttributeType.Period:
                    if (value.PeriodStart.HasValue || value.PeriodEnd.HasValue)
                    {
                        var start = value.PeriodStart.HasValue
                            ? value.PeriodStart.Value.ToString("dd.MM.yyyy")
                            : "?";
                        var end = value.PeriodEnd.HasValue
                            ? value.PeriodEnd.Value.ToString("dd.MM.yyyy")
                            : "н.в.";
                        return start + " — " + end;
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