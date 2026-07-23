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
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProfileController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null)
                return NotFound();

            var allAttributes = await _context.AttributeDefinitions
                .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
                .OrderBy(a => a.Name)
                .ToListAsync();

            var existingValues = await _context.AttributeValues
                .Where(av => av.CandidateId == currentUserId)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                Id = currentUserId,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Location = user.Location,
                PhotoUrl = user.PhotoUrl
            };

            var addedAttributeIds = existingValues
                .Select(av => av.AttributeDefinitionId)
                .ToHashSet();

            foreach (var attribute in allAttributes)
            {
                var existingValue = existingValues
                    .FirstOrDefault(av => av.AttributeDefinitionId == attribute.Id);

                if (existingValue == null)
                {
                    model.AvailableToAdd.Add(new AvailableAttributeViewModel
                    {
                        Id = attribute.Id,
                        Name = attribute.Name,
                        Type = attribute.Type
                    });
                    continue;
                }

                var attributeViewModel = new AttributeValueViewModel
                {
                    AttributeDefinitionId = attribute.Id,
                    AttributeName = attribute.Name,
                    AttributeDescription = attribute.Description,
                    AttributeType = attribute.Type,
                    ExistingValueId = existingValue.Id,
                    ValueString = existingValue.ValueString,
                    ValueNumber = existingValue.ValueNumber,
                    ValueDate = existingValue.ValueDate,
                    ValueBoolean = existingValue.ValueBoolean,
                    ValueImageUrl = existingValue.ValueImageUrl,
                    PeriodStart = existingValue.PeriodStart,
                    PeriodEnd = existingValue.PeriodEnd,
                    SelectedOptionId = existingValue.SelectedOptionId
                };

                if (attribute.Type == AttributeType.Dropdown)
                {
                    foreach (var option in attribute.Options)
                    {
                        attributeViewModel.Options.Add(new AttributeOptionViewModel
                        {
                            Id = option.Id,
                            Value = option.Value
                        });
                    }
                }

                model.AttributeValues.Add(attributeViewModel);
            }

            return View(model);
        }

        // POST: /Profile/UpdateName
        [HttpPost]
        public async Task<IActionResult> UpdateName(string fullName)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null)
                return Json(new { success = false, error = "Пользователь не найден" });

            if (string.IsNullOrWhiteSpace(fullName))
                return Json(new { success = false, error = "Имя не может быть пустым" });

            user.FullName = fullName.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Profile/UpdateMe
        [HttpPost]
        public async Task<IActionResult> UpdateMe(string? firstName, string? lastName,
            string? location, string? photoUrl)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (user == null)
                return Json(new { success = false, error = "Пользователь не найден" });

            user.FirstName = firstName?.Trim();
            user.LastName = lastName?.Trim();
            user.Location = location?.Trim();
            user.PhotoUrl = photoUrl?.Trim();

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Profile/AddAttribute
        [HttpPost]
        public async Task<IActionResult> AddAttribute(Guid attributeDefinitionId)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var attribute = await _context.AttributeDefinitions
                .FirstOrDefaultAsync(a => a.Id == attributeDefinitionId);

            if (attribute == null)
                return Json(new { success = false, error = "Атрибут не найден" });

            var existing = await _context.AttributeValues
                .FirstOrDefaultAsync(av =>
                    av.CandidateId == currentUserId &&
                    av.AttributeDefinitionId == attributeDefinitionId);

            if (existing != null)
                return Json(new { success = false, error = "Атрибут уже добавлен" });

            var newValue = new AttributeValue
            {
                CandidateId = currentUserId,
                AttributeDefinitionId = attributeDefinitionId
            };

            _context.AttributeValues.Add(newValue);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Profile/RemoveAttribute
        [HttpPost]
        public async Task<IActionResult> RemoveAttribute(Guid attributeDefinitionId)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var existingValue = await _context.AttributeValues
                .FirstOrDefaultAsync(av =>
                    av.CandidateId == currentUserId &&
                    av.AttributeDefinitionId == attributeDefinitionId);

            if (existingValue == null)
                return Json(new { success = false, error = "Атрибут не найден" });

            _context.AttributeValues.Remove(existingValue);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: /Profile/SaveAttribute
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SaveAttribute([FromBody] AttributeValueViewModel model)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User)!);

            var existingValue = await _context.AttributeValues
                .FirstOrDefaultAsync(av =>
                    av.CandidateId == currentUserId &&
                    av.AttributeDefinitionId == model.AttributeDefinitionId);

            if (existingValue == null)
            {
                existingValue = new AttributeValue
                {
                    CandidateId = currentUserId,
                    AttributeDefinitionId = model.AttributeDefinitionId
                };
                _context.AttributeValues.Add(existingValue);
            }

            existingValue.ValueString = null;
            existingValue.ValueNumber = null;
            existingValue.ValueDate = null;
            existingValue.ValueBoolean = null;
            existingValue.ValueImageUrl = null;
            existingValue.PeriodStart = null;
            existingValue.PeriodEnd = null;
            existingValue.SelectedOptionId = null;

            switch (model.AttributeType)
            {
                case AttributeType.String:
                case AttributeType.Text:
                    existingValue.ValueString = model.ValueString;
                    break;

                case AttributeType.Numeric:
                    existingValue.ValueNumber = model.ValueNumber;
                    break;

                case AttributeType.Date:
                    if (!string.IsNullOrEmpty(model.ValueString))
                    {
                        if (DateTime.TryParse(model.ValueString, out var parsedDate))
                            existingValue.ValueDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                    }
                    break;

                case AttributeType.Boolean:
                    existingValue.ValueBoolean = model.ValueBoolean ?? false;
                    break;

                case AttributeType.Image:
                    existingValue.ValueImageUrl = model.ValueImageUrl;
                    break;

                case AttributeType.Period:
                    if (!string.IsNullOrEmpty(model.ValueString))
                    {
                        var parts = model.ValueString.Split('|');
                        if (parts.Length == 2)
                        {
                            DateTime? start = null;
                            DateTime? end = null;

                            if (DateTime.TryParse(parts[0], out var parsedStart))
                                start = DateTime.SpecifyKind(parsedStart, DateTimeKind.Utc);

                            if (DateTime.TryParse(parts[1], out var parsedEnd))
                                end = DateTime.SpecifyKind(parsedEnd, DateTimeKind.Utc);

                            if (start.HasValue && end.HasValue && start > end)
                                return Json(new
                                {
                                    success = false,
                                    error = "Дата начала не может быть позже даты окончания"
                                });

                            existingValue.PeriodStart = start;
                            existingValue.PeriodEnd = end;
                        }
                    }
                    break;

                case AttributeType.Dropdown:
                    existingValue.SelectedOptionId = model.SelectedOptionId;
                    break;
            }

            existingValue.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}