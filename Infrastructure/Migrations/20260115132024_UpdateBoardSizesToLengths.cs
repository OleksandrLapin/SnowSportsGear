using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBoardSizesToLengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE pv
                FROM ProductVariant pv
                INNER JOIN Products p ON p.Id = pv.ProductId
                WHERE p.Type = 'Boards';
                """);

            migrationBuilder.Sql("""
                INSERT INTO ProductVariant (ProductId, Size, QuantityInStock)
                SELECT p.Id, s.Size, 5
                FROM Products p
                CROSS JOIN (VALUES
                    ('150'),
                    ('154'),
                    ('156'),
                    ('158'),
                    ('162'),
                    ('154W'),
                    ('158W'),
                    ('162W'),
                    ('166W'),
                    ('170W')
                ) AS s(Size)
                WHERE p.Type = 'Boards';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
