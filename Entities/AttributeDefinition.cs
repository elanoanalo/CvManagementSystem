using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public enum AttributeType
    {
        [Display(Name = "Строка")]
        String,

        [Display(Name = "Текст (Markdown)")]
        Text,

        [Display(Name = "Изображение")]
        Image,

        [Display(Name = "Число")]
        Numeric,

        [Display(Name = "Дата")]
        Date,

        [Display(Name = "Период")]
        Period,

        [Display(Name = "Да/Нет")]
        Boolean,

        [Display(Name = "Список вариантов")]
        Dropdown
    }

    public enum AttributeCategory
    {
        [Display(Name = "General")]
        General,

        [Display(Name = "Certification")]
        Certification,

        [Display(Name = "Domain Knowledge")]
        DomainKnowledge,

        [Display(Name = "Skill")]
        Skill,

        [Display(Name = "Language")]
        Language,

        [Display(Name = "Education")]
        Education,

        [Display(Name = "Experience")]
        Experience
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

        public User? CreatedByUser { get; set; }

        public List<AttributeOption> Options { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AttributeCategory Category { get; set; } = AttributeCategory.General;
    }
}