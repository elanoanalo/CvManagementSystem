namespace CvManagementSystem.ViewModels
{
    public class SearchResultsViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<SearchPositionItem> Positions { get; set; } = new();
        public List<SearchAttributeItem> Attributes { get; set; } = new();
        public bool HasResults => Positions.Any() || Attributes.Any();
    }

    public class SearchPositionItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class SearchAttributeItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}