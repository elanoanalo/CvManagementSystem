using CvManagementSystem.Entities;
using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels
{
    public class AttributeFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Введите название атрибута")]
        [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Максимум 1000 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Выберите тип атрибута")]
        public AttributeType Type { get; set; } = AttributeType.String;

        // Варианты для Dropdown — заполняется только если Type == Dropdown
        public List<string> Options { get; set; } = new();

        // Это поле для новых вариантов которые вводит пользователь
        public string? NewOption { get; set; }

        // Для Optimistic Locking
        public uint RowVersion { get; set; }

        public AttributeCategory Category { get; set; } = AttributeCategory.General;
    }

    public class AttributeListViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AttributeType Type { get; set; }
        public int OptionsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        public AttributeCategory Category { get; set; }
    }
}