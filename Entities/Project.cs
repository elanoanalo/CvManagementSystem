using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CandidateId { get; set; }
        public User? Candidate { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Теги проекта (Python, Backend, Data Engineering...)
        public List<ProjectTag> Tags { get; set; } = new();
    }

    public class ProjectTag
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }
        public Project? Project { get; set; }

        [Required]
        [MaxLength(50)]
        public string Tag { get; set; } = string.Empty;
    }
}