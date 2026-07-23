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
    public class PositionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<LocalizationMarker> _localizer;

        public PositionsController(AppDbContext context, UserManager<User> userManager, IStringLocalizer<LocalizationMarker> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: /Positions
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var isRecruiterOrAdmin = User.IsInRole("Recruiter") ||
                                     User.IsInRole("Administrator");

            var query = _context.Positions
                .Include(p => p.CreatedByUser)
                .Include(p => p.PositionAttributes)
                .Include(p => p.Tags)
                .Include(p => p.Cvs)
                .AsQueryable();

            if (!isRecruiterOrAdmin)
            {
                query = query.Where(p => p.IsPublished);
            }

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

        // GET: /Positions/Details/id
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

            if (User.IsInRole("Candidate"))
            {
                var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);
                var existingCv = await _context.Cvs
                    .FirstOrDefaultAsync(cv => cv.CandidateId == currentUserId && cv.PositionId == id);

                ViewBag.ExistingCvId = existingCv?.Id;
            }

            if (User.IsInRole("Candidate"))
            {
                var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);
                var existingCv = await _context.Cvs
                    .FirstOrDefaultAsync(cv => cv.CandidateId == currentUserId && cv.PositionId == id);

                ViewBag.ExistingCvId = existingCv?.Id;
            }

            return View(model);
        }

        // GET: /Positions/Create
        [Authorize(Roles = "Recruiter,Administrator")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PositionFormViewModel();
            await ReloadAvailableAttributes(model);
            return View(model);
        }

        // POST: /Positions/Create
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
                MaxProjectsInCv = model.MaxProjectsInCv,
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

        // GET: /Positions/Edit/id
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
                MaxProjectsInCv = position.MaxProjectsInCv,
                Tags = position.Tags.Select(t => t.Tag).ToList(),
                RowVersion = (uint)_context.Entry(position).Property("xmin").CurrentValue!
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

        // POST: /Positions/Edit/id
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

            _context.Entry(position).Property("xmin").OriginalValue = model.RowVersion;

            position.Title = model.Title;
            position.Description = model.Description;
            position.IsPublished = model.IsPublished;
            position.MaxProjectsInCv = model.MaxProjectsInCv;

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
                ModelState.AddModelError(string.Empty,
                    _localizer["ConcurrencyConflictPosition"]);

                await ReloadAvailableAttributes(model);
                return View(model);
            }
        }

        // GET: /Positions/Duplicate/id
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

            var model = new PositionFormViewModel
            {
                Title = position.Title + " (копия)",
                Description = position.Description,
                IsPublished = false,
                MaxProjectsInCv = position.MaxProjectsInCv,
                Tags = position.Tags.Select(t => t.Tag).ToList()
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

            return View("Create", model);
        }

        // POST: /Positions/Delete/id
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

        // GET: /Positions/Candidates/id
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