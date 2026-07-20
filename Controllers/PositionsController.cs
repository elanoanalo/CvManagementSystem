using CvManagementSystem.Data;
using CvManagementSystem.Entities;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers
{
    public class PositionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public PositionsController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Positions — доступно всем
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // Проверяем роль ПЕРЕД запросом к БД
            var isRecruiterOrAdmin = User.IsInRole("Recruiter") ||
                                     User.IsInRole("Administrator");

            // AsQueryable() — создаём "заготовку" запроса, но НЕ выполняем его ещё
            var query = _context.Positions
                .Include(p => p.CreatedByUser)
                .Include(p => p.PositionAttributes)
                .Include(p => p.Tags)
                .Include(p => p.Cvs)
                .AsQueryable();

            // Незалогиненные и кандидаты видят только опубликованные
            // Рекрутеры и админы видят все (включая черновики)
            if (!isRecruiterOrAdmin)
            {
                query = query.Where(p => p.IsPublished);
            }

            // ТОЛЬКО ЗДЕСЬ запрос реально уходит в базу
            var positions = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = new List<PositionListViewModel>();

            foreach (var position in positions)
            {
                result.Add(new PositionListViewModel
                {
                    Id = position.Id,
                    Title = position.Title,
                    Description = position.Description,
                    IsPublished = position.IsPublished,
                    AttributesCount = position.PositionAttributes.Count,
                    CvsCount = position.Cvs.Count,
                    CreatedAt = position.CreatedAt,
                    CreatedByName = position.CreatedByUser!.FullName,
                    Tags = position.Tags.Select(t => t.Tag).ToList()
                });
            }

            return View(result);
        }

        // GET: /Positions/Details/id — доступно всем
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var position = await _context.Positions
                .Include(p => p.CreatedByUser)
                .Include(p => p.Tags)
                .Include(p => p.PositionAttributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
                .Include(p => p.Cvs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (position == null)
                return NotFound();

            var model = new PositionListViewModel
            {
                Id = position.Id,
                Title = position.Title,
                Description = position.Description,
                IsPublished = position.IsPublished,
                AttributesCount = position.PositionAttributes.Count,
                CvsCount = position.Cvs.Count,
                CreatedAt = position.CreatedAt,
                CreatedByName = position.CreatedByUser!.FullName,
                Tags = position.Tags.Select(t => t.Tag).ToList()
            };

            ViewBag.Attributes = position.PositionAttributes
                .OrderBy(pa => pa.DisplayOrder)
                .Select(pa => new
                {
                    Name = pa.AttributeDefinition!.Name,
                    Type = pa.AttributeDefinition.Type,
                    IsRequired = pa.IsRequired
                })
                .ToList();

            return View(model);
        }

        // GET: /Positions/Create — только рекрутерам
        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PositionFormViewModel();
            await ReloadAvailableAttributes(model);
            return View(model);
        }

        // POST: /Positions/Create — только рекрутерам
        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpPost]
        public async Task<IActionResult> Create(PositionFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await ReloadAvailableAttributes(model);
                return View(model);
            }

            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var position = new Position
            {
                Title = model.Title,
                Description = model.Description,
                IsPublished = model.IsPublished,
                CreatedByUserId = currentUserId
            };

            _context.Positions.Add(position);

            foreach (var tag in model.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;

                _context.PositionTags.Add(new PositionTag
                {
                    PositionId = position.Id,
                    Tag = tag.Trim()
                });
            }

            int order = 0;
            foreach (var attr in model.Attributes)
            {
                _context.PositionAttributes.Add(new PositionAttribute
                {
                    PositionId = position.Id,
                    AttributeDefinitionId = attr.AttributeDefinitionId,
                    IsRequired = attr.IsRequired,
                    DisplayOrder = order
                });
                order++;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Positions/Edit/id — только рекрутерам
        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var position = await _context.Positions
                .Include(p => p.Tags)
                .Include(p => p.PositionAttributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (position == null)
                return NotFound();

            var model = new PositionFormViewModel
            {
                Id = position.Id,
                Title = position.Title,
                Description = position.Description,
                IsPublished = position.IsPublished,
                Tags = position.Tags.Select(t => t.Tag).ToList(),
                // Конвертируем byte[] в Base64 строку для передачи через форму
                RowVersion = position.RowVersion != null
                    ? Convert.ToBase64String(position.RowVersion)
                    : null
            };

            foreach (var pa in position.PositionAttributes.OrderBy(pa => pa.DisplayOrder))
            {
                model.Attributes.Add(new PositionAttributeViewModel
                {
                    AttributeDefinitionId = pa.AttributeDefinitionId,
                    AttributeName = pa.AttributeDefinition!.Name,
                    AttributeType = pa.AttributeDefinition.Type,
                    IsRequired = pa.IsRequired,
                    DisplayOrder = pa.DisplayOrder
                });
            }

            await ReloadAvailableAttributes(model);
            return View(model);
        }

        // POST: /Positions/Edit/id — только рекрутерам
        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, PositionFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await ReloadAvailableAttributes(model);
                return View(model);
            }

            var position = await _context.Positions
                .Include(p => p.Tags)
                .Include(p => p.PositionAttributes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (position == null)
                return NotFound();

            // Optimistic Locking — проверяем версию записи
            if (!string.IsNullOrEmpty(model.RowVersion))
            {
                position.RowVersion = Convert.FromBase64String(model.RowVersion);
            }

            position.Title = model.Title;
            position.Description = model.Description;
            position.IsPublished = model.IsPublished;

            _context.PositionTags.RemoveRange(position.Tags);

            foreach (var tag in model.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                _context.PositionTags.Add(new PositionTag
                {
                    PositionId = position.Id,
                    Tag = tag.Trim()
                });
            }

            _context.PositionAttributes.RemoveRange(position.PositionAttributes);

            int order = 0;
            foreach (var attr in model.Attributes)
            {
                _context.PositionAttributes.Add(new PositionAttribute
                {
                    PositionId = position.Id,
                    AttributeDefinitionId = attr.AttributeDefinitionId,
                    IsRequired = attr.IsRequired,
                    DisplayOrder = order
                });
                order++;
            }

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Кто-то другой изменил эту запись пока мы редактировали
                ModelState.AddModelError(string.Empty,
                    "Эта позиция была изменена другим пользователем пока вы редактировали. " +
                    "Пожалуйста, перезагрузите страницу и попробуйте снова.");

                await ReloadAvailableAttributes(model);
                return View(model);
            }
        }

        // GET: /Positions/Duplicate/id — открывает форму создания с данными оригинала
        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpGet]
        public async Task<IActionResult> Duplicate(Guid id)
        {
            var position = await _context.Positions
                .Include(p => p.Tags)
                .Include(p => p.PositionAttributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (position == null)
                return NotFound();

            // Готовим модель для формы Create — с данными оригинала, но БЕЗ сохранения в базу
            var model = new PositionFormViewModel
            {
                // Id НЕ задаём — это новая позиция, не редактирование
                Title = position.Title + " (копия)",
                Description = position.Description,
                IsPublished = false, // копия всегда черновик
                Tags = position.Tags.Select(t => t.Tag).ToList()
            };

            // Копируем привязанные атрибуты
            foreach (var pa in position.PositionAttributes.OrderBy(pa => pa.DisplayOrder))
            {
                model.Attributes.Add(new PositionAttributeViewModel
                {
                    AttributeDefinitionId = pa.AttributeDefinitionId,
                    AttributeName = pa.AttributeDefinition!.Name,
                    AttributeType = pa.AttributeDefinition.Type,
                    IsRequired = pa.IsRequired,
                    DisplayOrder = pa.DisplayOrder
                });
            }

            // Загружаем список доступных атрибутов для формы
            await ReloadAvailableAttributes(model);

            // Возвращаем ПРЕДСТАВЛЕНИЕ Create (не Duplicate!) с заполненной моделью
            // Пользователь увидит форму создания с данными. Ничего пока не в базе.
            return View("Create", model);
        }

        // POST: /Positions/Delete/id — только рекрутерам
        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var position = await _context.Positions
                .Include(p => p.Tags)
                .Include(p => p.PositionAttributes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (position == null)
                return NotFound();

            _context.PositionTags.RemoveRange(position.Tags);
            _context.PositionAttributes.RemoveRange(position.PositionAttributes);
            _context.Positions.Remove(position);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpPost]
        public async Task<IActionResult> DeleteMultiple(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return RedirectToAction(nameof(Index));

            var positions = await _context.Positions
                .Include(p => p.Tags)
                .Include(p => p.PositionAttributes)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            foreach (var position in positions)
            {
                _context.PositionTags.RemoveRange(position.Tags);
                _context.PositionAttributes.RemoveRange(position.PositionAttributes);
            }

            _context.Positions.RemoveRange(positions);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Positions/Candidates/id — только рекрутерам
        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpGet]
        public async Task<IActionResult> Candidates(Guid id)
        {
            var position = await _context.Positions
                .FirstOrDefaultAsync(p => p.Id == id);

            if (position == null)
                return NotFound();

            var cvs = await _context.Cvs
                .Include(cv => cv.Candidate)
                .Where(cv => cv.PositionId == id)
                .OrderByDescending(cv => cv.CreatedAt)
                .ToListAsync();

            ViewBag.PositionTitle = position.Title;
            ViewBag.PositionId = id;

            var result = new List<CvListViewModel>();

            foreach (var cv in cvs)
            {
                result.Add(new CvListViewModel
                {
                    Id = cv.Id,
                    PositionTitle = cv.Candidate!.FullName,
                    CreatedAt = cv.CreatedAt,
                    AttributesCount = 0,
                    ProjectsCount = 0
                });
            }

            return View(result);
        }

        // Приватный метод — загружает список атрибутов для формы
        private async Task ReloadAvailableAttributes(PositionFormViewModel model)
        {
            model.AvailableAttributes.Clear();

            var availableAttributes = await _context.AttributeDefinitions
                .OrderBy(a => a.Name)
                .ToListAsync();

            foreach (var attribute in availableAttributes)
            {
                model.AvailableAttributes.Add(new AttributeSelectionViewModel
                {
                    Id = attribute.Id,
                    Name = attribute.Name,
                    Type = attribute.Type
                });
            }
        }
    }
}