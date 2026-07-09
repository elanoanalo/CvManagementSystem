using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public class PositionTag
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PositionId { get; set; }
        public Position? Position { get; set; }

        [Required]
        [MaxLength(100)]
        public string Tag { get; set; } = string.Empty;
    }
}