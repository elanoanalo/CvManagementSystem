using CvManagementSystem.Entities;

namespace CvManagementSystem.ViewModels
{
    public class CvListViewModel
    {
        public Guid Id { get; set; }
        public string PositionTitle { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int AttributesCount { get; set; }
        public int ProjectsCount { get; set; }
    }

    public class CvViewModel
    {
        public Guid Id { get; set; }
        public string CandidateFullName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string PositionTitle { get; set; } = string.Empty;
        public string? PositionDescription { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<CvAttributeViewModel> Attributes { get; set; } = new();

        public List<CvProjectViewModel> Projects { get; set; } = new();

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Location { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class CvAttributeViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AttributeType Type { get; set; }
        public bool IsRequired { get; set; }
        public string? DisplayValue { get; set; }
        public bool HasValue { get; set; }
    }

    public class CvProjectViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class PositionForCandidateViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Tags { get; set; } = new();
        public int AttributesCount { get; set; }
        public bool AlreadyHasCv { get; set; }
        public Guid? ExistingCvId { get; set; }
    }
}