using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public class AttributeOption
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AttributeDefinitionId { get; set; }

        public AttributeDefinition? AttributeDefinition { get; set; }

        [Required]
        [MaxLength(200)]
        public string Value { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}