namespace Core.Models;

public class ProductSizeGuide
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
    public string? HowToMeasure { get; set; }
    public string? FitNotes { get; set; }
    public string? Disclaimer { get; set; }
    public List<string> ExtraNotes { get; set; } = [];
}
