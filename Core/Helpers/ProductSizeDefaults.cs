namespace Core.Helpers;

public static class ProductSizeDefaults
{
    public static readonly IReadOnlyList<string> BoardSizes =
    [
        "150",
        "154",
        "156",
        "158",
        "162",
        "154W",
        "158W",
        "162W",
        "166W",
        "170W"
    ];

    public static readonly IReadOnlyList<string> BootSizes =
    [
        "36",
        "37",
        "38",
        "39",
        "40",
        "41",
        "42",
        "43",
        "44",
        "45",
        "46"
    ];

    public static readonly IReadOnlyList<string> ApparelSizes = ["S", "M", "L", "XL"];

    public static IReadOnlyList<string> GetSizesForType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return ApparelSizes;
        }

        var normalized = type.Trim().ToLowerInvariant();

        if (normalized.Contains("board"))
        {
            return BoardSizes;
        }

        if (normalized.Contains("boot"))
        {
            return BootSizes;
        }

        if (normalized.Contains("hat") || normalized.Contains("glove"))
        {
            return ApparelSizes;
        }

        return ApparelSizes;
    }

    public static bool IsDisallowedSize(string? size)
    {
        return string.Equals(size?.Trim(), "UNI", StringComparison.OrdinalIgnoreCase);
    }
}
