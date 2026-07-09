using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public class CvComment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CvId { get; set; }
        public Cv? Cv { get; set; }

        [Required]
        public Guid AuthorUserId { get; set; }
        public User? AuthorUser { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}