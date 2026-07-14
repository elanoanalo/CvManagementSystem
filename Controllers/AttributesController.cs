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
        public async Task<IActionResult> Index()
        {
            var attributes = await _context.AttributeDefinitions
                .Include(a => a.CreatedByUser)
                .Include(a => a.Options)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AttributeListViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Type = a.Type,
                    OptionsCount = a.Options.Count,
                    CreatedAt = a.CreatedAt,
                    CreatedByName = a.CreatedByUser!.FullName
                })
                .ToListAsync();

            return View(attributes);
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
                Options = attribute.Options.Select(o => o.Value).ToList()
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

            // Обновляем основные поля
            attribute.Name = model.Name;
            attribute.Description = model.Description;

            // Если Dropdown — пересоздаём варианты
            if (model.Type == AttributeType.Dropdown)
            {
                // Удаляем все старые варианты
                _context.AttributeOptions.RemoveRange(attribute.Options);

                // Добавляем новые через foreach — так же как в Create
                int order = 0;
                foreach (var option in model.Options)
                {
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
    }
}