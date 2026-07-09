using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public enum UserRole
    {
        Candidate,
        Recruiter,
        Administrator
    }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "en";

        [MaxLength(10)]
        public string PreferredTheme { get; set; } = "light";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства — обратная сторона связей из других сущностей
        public List<AttributeDefinition> CreatedAttributeDefinitions { get; set; } = new();
        public List<AttributeValue> AttributeValues { get; set; } = new();
        public List<Position> CreatedPositions { get; set; } = new();
        public List<Cv> Cvs { get; set; } = new();
        public List<CvComment> Comments { get; set; } = new();
    }
}