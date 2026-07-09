using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public class PositionAttribute
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PositionId { get; set; }
        public Position? Position { get; set; }

        [Required]
        public Guid AttributeDefinitionId { get; set; }
        public AttributeDefinition? AttributeDefinition { get; set; }

        public bool IsRequired { get; set; } = false;

        public int DisplayOrder { get; set; }

        [MaxLength(500)]
        public string? AccessRule { get; set; }
    }
}