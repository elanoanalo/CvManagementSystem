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


        [MaxLength(2000)]
        public string? ValueString { get; set; }       

        public decimal? ValueNumber { get; set; }      

        public DateTime? ValueDate { get; set; }        

        public bool? ValueBoolean { get; set; }      

        [MaxLength(500)]
        public string? ValueImageUrl { get; set; }        

        public DateTime? PeriodStart { get; set; }      
        public DateTime? PeriodEnd { get; set; }        

        public Guid? SelectedOptionId { get; set; }       
        public AttributeOption? SelectedOption { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}