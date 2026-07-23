using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public enum UserRole
    {
        Candidate,
        Recruiter,
        Administrator
    }

    public class User : IdentityUser<Guid>
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Location { get; set; }
        public string? PhotoUrl { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Candidate;

        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "en";

        [MaxLength(10)]
        public string PreferredTheme { get; set; } = "light";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public List<AttributeDefinition> CreatedAttributeDefinitions { get; set; } = new();
        public List<AttributeValue> AttributeValues { get; set; } = new();
        public List<Position> CreatedPositions { get; set; } = new();
        public List<Cv> Cvs { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
    }
}