using CvManagementSystem.Entities;

namespace CvManagementSystem.ViewModels
{
    public class AdminUserListViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }

        public int ProjectsCount { get; set; }
        public int CvsCount { get; set; }
        public int PositionsCount { get; set; }
        public int FilledAttributesCount { get; set; }
    }

    public class AdminUserDetailsViewModel
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Location { get; set; }
        public string? PhotoUrl { get; set; }

        public List<AdminAttributeItem> Attributes { get; set; } = new();
        public List<AdminProjectItem> Projects { get; set; } = new();
        public List<AdminCvItem> Cvs { get; set; } = new();
        public List<AdminPositionItem> CreatedPositions { get; set; } = new();
    }

    public class AdminAttributeItem
    {
        public string Name { get; set; } = string.Empty;
        public AttributeType Type { get; set; }
        public string? DisplayValue { get; set; }
    }

    public class AdminProjectItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class AdminCvItem
    {
        public Guid Id { get; set; }
        public string PositionTitle { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminPositionItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
    }
}