using CvManagementSystem.Entities;

namespace CvManagementSystem.ViewModels
{
    // Список CV кандидата
    public class CvListViewModel
    {
        public Guid Id { get; set; }
        public string PositionTitle { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int AttributesCount { get; set; }
        public int ProjectsCount { get; set; }
    }

    // Полное CV — то что видит рекрутер или кандидат
    public class CvViewModel
    {
        public Guid Id { get; set; }
        public string CandidateFullName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string PositionTitle { get; set; } = string.Empty;
        public string? PositionDescription { get; set; }
        public DateTime CreatedAt { get; set; }

        // Атрибуты — подтягиваются живьём из профиля
        public List<CvAttributeViewModel> Attributes { get; set; } = new();

        // Проекты — фильтруются по тегам позиции
        public List<CvProjectViewModel> Projects { get; set; } = new();
    }

    // Один атрибут в CV
    public class CvAttributeViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AttributeType Type { get; set; }
        public bool IsRequired { get; set; }

        // Отображаемое значение — одна строка для любого типа
        public string? DisplayValue { get; set; }
        public bool HasValue { get; set; }
    }

    // Один проект в CV
    public class CvProjectViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    // Список позиций для кандидата
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