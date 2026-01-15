using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductSizesAndLowestPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM ProductVariant
                WHERE UPPER(Size) = 'UNI';
                """);

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
                    ('40-55 kg'),
                    ('55-65 kg'),
                    ('65-75 kg'),
                    ('75-85 kg'),
                    ('85-95 kg'),
                    ('95-110 kg')
                ) AS s(Size)
                WHERE p.Type = 'Boards';
                """);

            migrationBuilder.Sql("""
                DELETE pv
                FROM ProductVariant pv
                INNER JOIN Products p ON p.Id = pv.ProductId
                WHERE p.Type = 'Boots';
                """);

            migrationBuilder.Sql("""
                INSERT INTO ProductVariant (ProductId, Size, QuantityInStock)
                SELECT p.Id, s.Size, 5
                FROM Products p
                CROSS JOIN (VALUES
                    ('36'),
                    ('37'),
                    ('38'),
                    ('39'),
                    ('40'),
                    ('41'),
                    ('42'),
                    ('43'),
                    ('44'),
                    ('45'),
                    ('46')
                ) AS s(Size)
                WHERE p.Type = 'Boots';
                """);

            migrationBuilder.Sql("""
                DELETE pv
                FROM ProductVariant pv
                INNER JOIN Products p ON p.Id = pv.ProductId
                WHERE p.Type IN ('Hats', 'Gloves')
                  AND UPPER(pv.Size) NOT IN ('S', 'M', 'L', 'XL');
                """);

            migrationBuilder.Sql("""
                INSERT INTO ProductVariant (ProductId, Size, QuantityInStock)
                SELECT p.Id, s.Size, 5
                FROM Products p
                CROSS JOIN (VALUES
                    ('S'),
                    ('M'),
                    ('L'),
                    ('XL')
                ) AS s(Size)
                WHERE p.Type IN ('Hats', 'Gloves')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ProductVariant pv
                      WHERE pv.ProductId = p.Id
                        AND UPPER(pv.Size) = s.Size
                  );
                """);

            migrationBuilder.Sql("""
                UPDATE Products
                SET LowestPrice = CASE
                    WHEN SalePrice IS NOT NULL AND SalePrice > 0 AND SalePrice < Price THEN SalePrice
                    ELSE Price
                END
                WHERE LowestPrice IS NULL
                   OR LowestPrice <= 0
                   OR LowestPrice > CASE
                        WHEN SalePrice IS NOT NULL AND SalePrice > 0 AND SalePrice < Price THEN SalePrice
                        ELSE Price
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
