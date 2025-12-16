using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillProductRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ;WITH RatingStats AS (
                    SELECT
                        pr.ProductId,
                        AVG(CAST(pr.Rating AS float)) AS AvgRating,
                        COUNT(*) AS Cnt
                    FROM ProductReviews pr
                    WHERE pr.IsHidden = 0
                    GROUP BY pr.ProductId
                )
                UPDATE p
                SET
                    p.RatingAverage = ISNULL(rs.AvgRating, 0),
                    p.RatingCount = ISNULL(rs.Cnt, 0)
                FROM Products p
                LEFT JOIN RatingStats rs ON rs.ProductId = p.Id;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Products
                SET RatingAverage = 0, RatingCount = 0;
            """);
        }
    }
}
