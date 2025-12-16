using System.Globalization;

namespace Core.Specifications;

public class AdminReviewSpecParams : PagingParams
{
    public int? ProductId { get; set; }
    public string? Brand { get; set; }
    public string? Type { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public string? Status { get; set; } // published | hidden | all
    public string? Search { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string Sort { get; set; } = "newest";
}
