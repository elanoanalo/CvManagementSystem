using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public class Position
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        [Required]
        public Guid CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int MaxProjectsInCv { get; set; } = 3;

        public List<PositionAttribute> PositionAttributes { get; set; } = new();
        public List<PositionTag> Tags { get; set; } = new();
        public List<Cv> Cvs { get; set; } = new();
    }
}