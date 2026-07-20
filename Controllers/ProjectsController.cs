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
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectsController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Projects
        public async Task<IActionResult> Index()
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var projects = await _context.Projects
                .Include(p => p.Tags)
                .Where(p => p.CandidateId == currentUserId)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            var result = new List<ProjectListViewModel>();

            foreach (var project in projects)
            {
                result.Add(new ProjectListViewModel
                {
                    Id = project.Id,
                    Title = project.Title,
                    Description = project.Description,
                    StartDate = project.StartDate.HasValue
                        ? project.StartDate.Value.ToString("dd.MM.yyyy")
                        : null,
                    EndDate = project.EndDate.HasValue
                        ? project.EndDate.Value.ToString("dd.MM.yyyy")
                        : null,
                    Tags = project.Tags.Select(t => t.Tag).ToList(),
                    CreatedAt = project.CreatedAt
                });
            }

            return View(result);
        }

        // GET: /Projects/Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new ProjectFormViewModel();
            return View(model);
        }

        // POST: /Projects/Create
        [HttpPost]
        public async Task<IActionResult> Create(ProjectFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var project = new Project
            {
                CandidateId = currentUserId,
                Title = model.Title,
                Description = model.Description
            };

            // Конвертируем даты из строк
            if (!string.IsNullOrEmpty(model.StartDate) &&
                DateTime.TryParse(model.StartDate, out var startDate))
            {
                project.StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            }

            if (!string.IsNullOrEmpty(model.EndDate) &&
                DateTime.TryParse(model.EndDate, out var endDate))
            {
                project.EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
            }

            // Валидация дат
            if (project.StartDate.HasValue && project.EndDate.HasValue &&
                project.StartDate > project.EndDate)
            {
                ModelState.AddModelError("EndDate",
                    "Дата окончания не может быть раньше даты начала");
                return View(model);
            }

            _context.Projects.Add(project);

            // Добавляем теги
            foreach (var tag in model.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                _context.ProjectTags.Add(new ProjectTag
                {
                    ProjectId = project.Id,
                    Tag = tag.Trim()
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Projects/Edit/id
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var project = await _context.Projects
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id && p.CandidateId == currentUserId);

            if (project == null)
                return NotFound();

            var model = new ProjectFormViewModel
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                StartDate = project.StartDate.HasValue
                    ? project.StartDate.Value.ToString("yyyy-MM-dd")
                    : null,
                EndDate = project.EndDate.HasValue
                    ? project.EndDate.Value.ToString("yyyy-MM-dd")
                    : null,
                Tags = project.Tags.Select(t => t.Tag).ToList()
            };

            return View(model);
        }

        // POST: /Projects/Edit/id
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, ProjectFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var project = await _context.Projects
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id && p.CandidateId == currentUserId);

            if (project == null)
                return NotFound();

            project.Title = model.Title;
            project.Description = model.Description;
            project.StartDate = null;
            project.EndDate = null;

            if (!string.IsNullOrEmpty(model.StartDate) &&
                DateTime.TryParse(model.StartDate, out var startDate))
            {
                project.StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            }

            if (!string.IsNullOrEmpty(model.EndDate) &&
                DateTime.TryParse(model.EndDate, out var endDate))
            {
                project.EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
            }

            if (project.StartDate.HasValue && project.EndDate.HasValue &&
                project.StartDate > project.EndDate)
            {
                ModelState.AddModelError("EndDate",
                    "Дата окончания не может быть раньше даты начала");
                return View(model);
            }

            // Пересоздаём теги
            _context.ProjectTags.RemoveRange(project.Tags);

            foreach (var tag in model.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                _context.ProjectTags.Add(new ProjectTag
                {
                    ProjectId = project.Id,
                    Tag = tag.Trim()
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return RedirectToAction(nameof(Index));

            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            // Только свои проекты (security by ownership)
            var projects = await _context.Projects
                .Include(p => p.Tags)
                .Where(p => ids.Contains(p.Id) && p.CandidateId == currentUserId)
                .ToListAsync();

            foreach (var project in projects)
                _context.ProjectTags.RemoveRange(project.Tags);

            _context.Projects.RemoveRange(projects);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Projects/Delete/id
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var project = await _context.Projects
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id && p.CandidateId == currentUserId);

            if (project == null)
                return NotFound();

            _context.ProjectTags.RemoveRange(project.Tags);
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}