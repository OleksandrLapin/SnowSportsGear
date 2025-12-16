using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DefaultSizeQuantities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DECLARE @sizes TABLE(Size nvarchar(10), Qty int);
                INSERT INTO @sizes(Size, Qty) VALUES ('S',5),('M',7),('L',10),('XL',12);

                INSERT INTO ProductVariant (Size, QuantityInStock, ProductId)
                SELECT s.Size, s.Qty, p.Id
                FROM Products p
                CROSS JOIN @sizes s
                WHERE NOT EXISTS (
                    SELECT 1 FROM ProductVariant v 
                    WHERE v.ProductId = p.Id AND UPPER(v.Size) = UPPER(s.Size)
                );

                UPDATE v
                SET v.QuantityInStock = s.Qty
                FROM ProductVariant v
                JOIN Products p ON v.ProductId = p.Id
                JOIN @sizes s ON UPPER(s.Size) = UPPER(v.Size)
                WHERE v.QuantityInStock <= 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
