using CvManagementSystem.Entities;
using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels
{
    public class ProfileViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Введите имя")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // Атрибуты которые кандидат УЖЕ добавил в профиль
        public List<AttributeValueViewModel> AttributeValues { get; set; } = new();

        // Атрибуты из библиотеки которые ещё НЕ добавлены
        // (для выпадающего списка "Добавить атрибут")
        public List<AvailableAttributeViewModel> AvailableToAdd { get; set; } = new();
    }

    public class AvailableAttributeViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AttributeType Type { get; set; }
    }

    public class AttributeValueViewModel
    {
        public Guid AttributeDefinitionId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string? AttributeDescription { get; set; }
        public AttributeType AttributeType { get; set; }
        public Guid? ExistingValueId { get; set; }

        public string? ValueString { get; set; }
        public decimal? ValueNumber { get; set; }
        public DateTime? ValueDate { get; set; }
        public bool? ValueBoolean { get; set; }
        public string? ValueImageUrl { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public Guid? SelectedOptionId { get; set; }

        public List<AttributeOptionViewModel> Options { get; set; } = new();
    }

    public class AttributeOptionViewModel
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}