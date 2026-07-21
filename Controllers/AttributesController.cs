using CvManagementSystem.Data;
using CvManagementSystem.Entities;
using CvManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvManagementSystem.Controllers
{
    [Authorize(Roles = "Recruiter,Administrator")]
    public class AttributesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public AttributesController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Attributes
        public async Task<IActionResult> Index(AttributeCategory? category)
        {
            var query = _context.AttributeDefinitions
                .Include(a => a.CreatedByUser)
                .Include(a => a.Options)
                .AsQueryable();

            // Фильтр по категории если выбрана
            if (category.HasValue)
            {
                query = query.Where(a => a.Category == category.Value);
            }

            var attributes = await query
                .OrderBy(a => a.Name)
                .ToListAsync();

            var result = new List<AttributeListViewModel>();
            foreach (var a in attributes)
            {
                result.Add(new AttributeListViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Type = a.Type,
                    Category = a.Category,  // ← добавь
                    OptionsCount = a.Options.Count,
                    CreatedByName = a.CreatedByUser!.FullName,
                    CreatedAt = a.CreatedAt
                });
            }

            // Передаём текущий фильтр во View чтобы подсветить активную кнопку
            ViewBag.SelectedCategory = category;

            return View(result);
        }

        // GET: /Attributes/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new AttributeFormViewModel());
        }

        // POST: /Attributes/Create
        [HttpPost]
        public async Task<IActionResult> Create(AttributeFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var attribute = new AttributeDefinition
            {
                Name = model.Name,
                Description = model.Description,
                Type = model.Type,
                Category = model.Category,  // ← добавь
                CreatedByUserId = currentUserId
            };

            _context.AttributeDefinitions.Add(attribute);

            // Если тип Dropdown — добавляем варианты
            if (model.Type == AttributeType.Dropdown)
            {
                int order = 0;
                foreach (var option in model.Options)
                {
                    // Пропускаем пустые строки
                    if (string.IsNullOrWhiteSpace(option))
                        continue;

                    _context.AttributeOptions.Add(new AttributeOption
                    {
                        AttributeDefinitionId = attribute.Id,
                        Value = option.Trim(),
                        DisplayOrder = order
                    });

                    order++;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Attributes/Edit/id
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var attribute = await _context.AttributeDefinitions
                .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attribute == null)
                return NotFound();

            var model = new AttributeFormViewModel
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Description = attribute.Description,
                Type = attribute.Type,
                Options = attribute.Options.Select(o => o.Value).ToList(),
                // Конвертируем byte[] в Base64 для передачи через форму
                RowVersion = attribute.RowVersion != null
                    ? Convert.ToBase64String(attribute.RowVersion)
                    : null
            };

            return View(model);
        }

        // POST: /Attributes/Edit/id
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, AttributeFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var attribute = await _context.AttributeDefinitions
                .Include(a => a.Options)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attribute == null)
                return NotFound();

            if (!string.IsNullOrEmpty(model.RowVersion))
            {
                attribute.RowVersion = Convert.FromBase64String(model.RowVersion);
            }

            attribute.Name = model.Name;
            attribute.Description = model.Description;
            attribute.Category = model.Category;  // ← добавь

            if (model.Type == AttributeType.Dropdown)
            {
                var newOptions = model.Options
                    .Where(o => !string.IsNullOrWhiteSpace(o))
                    .Select(o => o.Trim())
                    .ToList();

                var existingOptions = attribute.Options.OrderBy(o => o.DisplayOrder).ToList();

                for (int i = 0; i < newOptions.Count; i++)
                {
                    if (i < existingOptions.Count)
                    {
                        existingOptions[i].Value = newOptions[i];
                        existingOptions[i].DisplayOrder = i;
                    }
                    else
                    {
                        _context.AttributeOptions.Add(new AttributeOption
                        {
                            AttributeDefinitionId = attribute.Id,
                            Value = newOptions[i],
                            DisplayOrder = i
                        });
                    }
                }

                if (existingOptions.Count > newOptions.Count)
                {
                    for (int i = newOptions.Count; i < existingOptions.Count; i++)
                    {
                        var optionToRemove = existingOptions[i];

                        var isUsed = await _context.AttributeValues
                            .AnyAsync(av => av.SelectedOptionId == optionToRemove.Id);

                        if (!isUsed)
                        {
                            _context.AttributeOptions.Remove(optionToRemove);
                        }
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty,
                    "Этот атрибут был изменён другим пользователем пока вы редактировали. " +
                    "Пожалуйста, перезагрузите страницу и попробуйте снова.");
                return View(model);
            }
        }

        // GET: /Attributes/Details/id
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var attribute = await _context.AttributeDefinitions
                .Include(a => a.CreatedByUser)
                .Include(a => a.Options)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attribute == null)
                return NotFound();

            var model = new AttributeListViewModel
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Description = attribute.Description,
                Type = attribute.Type,
                Category = attribute.Category,
                OptionsCount = attribute.Options.Count,
                CreatedAt = attribute.CreatedAt,
                CreatedByName = attribute.CreatedByUser!.FullName
            };

            return View(model);
        }

        // POST: /Attributes/Delete/id
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var attribute = await _context.AttributeDefinitions
                .Include(a => a.Options)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attribute == null)
                return NotFound();

            _context.AttributeOptions.RemoveRange(attribute.Options);
            _context.AttributeDefinitions.Remove(attribute);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return RedirectToAction(nameof(Index));

            var attributes = await _context.AttributeDefinitions
                .Include(a => a.Options)
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();

            foreach (var attribute in attributes)
                _context.AttributeOptions.RemoveRange(attribute.Options);

            _context.AttributeDefinitions.RemoveRange(attributes);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}