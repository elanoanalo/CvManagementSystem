using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CvManagementSystem.Entities
{
    public class Cv
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CandidateId { get; set; }
        public User? Candidate { get; set; }

        [Required]
        public Guid PositionId { get; set; }
        public Position? Position { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}