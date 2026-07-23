using CvManagementSystem.Entities;
using System.ComponentModel.DataAnnotations;

namespace CvManagementSystem.ViewModels
{
    public class PositionFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Введите название позиции")]
        [MaxLength(200, ErrorMessage = "Максимум 200 символов")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000, ErrorMessage = "Максимум 4000 символов")]
        public string? Description { get; set; }

        public bool IsPublished { get; set; } = false;

        public List<string> Tags { get; set; } = new();

        public List<PositionAttributeViewModel> Attributes { get; set; } = new();

        public List<AttributeSelectionViewModel> AvailableAttributes { get; set; } = new();

        public uint RowVersion { get; set; }

        public int MaxProjectsInCv { get; set; } = 3;
    }

    public class PositionAttributeViewModel
    {
        public Guid AttributeDefinitionId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public AttributeType AttributeType { get; set; }
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class AttributeSelectionViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AttributeType Type { get; set; }
    }

    public class PositionListViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublished { get; set; }
        public int AttributesCount { get; set; }
        public int CvsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}