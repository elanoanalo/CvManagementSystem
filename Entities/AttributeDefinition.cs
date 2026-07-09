using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public enum AttributeType
    {
        String,
        Text,
        Image,
        Numeric,
        Date,
        Period,
        Boolean,
        Dropdown
    }

    public class AttributeDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public AttributeType Type { get; set; }

        [Required]
        public Guid CreatedByUserId { get; set; }

        // Навигационное свойство — связь с User, который создал атрибут
        public User? CreatedByUser { get; set; }

        // Навигационное свойство — список вариантов выбора (только для Dropdown)
        public List<AttributeOption> Options { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optimistic locking — разберём отдельно чуть позже подробно
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}