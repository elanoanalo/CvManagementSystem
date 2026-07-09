using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.Entities
{
    public class AttributeValue
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CandidateId { get; set; }
        public User? Candidate { get; set; }

        [Required]
        public Guid AttributeDefinitionId { get; set; }
        public AttributeDefinition? AttributeDefinition { get; set; }

        // --- Типизированные колонки под разные AttributeType ---

        [MaxLength(2000)]
        public string? ValueString { get; set; }       // для String и Text

        public decimal? ValueNumber { get; set; }       // для Numeric

        public DateTime? ValueDate { get; set; }         // для Date

        public bool? ValueBoolean { get; set; }          // для Boolean

        [MaxLength(500)]
        public string? ValueImageUrl { get; set; }        // для Image (ссылка на облако!)

        public DateTime? PeriodStart { get; set; }         // для Period
        public DateTime? PeriodEnd { get; set; }            // для Period

        public Guid? SelectedOptionId { get; set; }          // для Dropdown
        public AttributeOption? SelectedOption { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}