using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels
{
    public class ProjectFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Введите название проекта")]
        [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000, ErrorMessage = "Максимум 4000 символов")]
        public string? Description { get; set; }

        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        public List<string> Tags { get; set; } = new();
    }

    public class ProjectListViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}