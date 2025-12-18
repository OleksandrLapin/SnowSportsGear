using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProductReviewIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_UserId",
                table: "ProductReviews");

            migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = 'ADMIN')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [ConcurrencyStamp], [Name], [NormalizedName])
    VALUES ('3071ff28-a856-4bff-8d16-65a729dbf3c0', NULL, 'Admin', 'ADMIN');
END
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = 'CUSTOMER')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [ConcurrencyStamp], [Name], [NormalizedName])
    VALUES ('1eeb7c27-c590-44ec-b013-2b97b1e95802', NULL, 'Customer', 'CUSTOMER');
END
""");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_UserId_ProductId",
                table: "ProductReviews",
                columns: new[] { "UserId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_UserId_ProductId",
                table: "ProductReviews");

            // Roles are left unchanged on downgrade to avoid conflicts with existing data.

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_UserId",
                table: "ProductReviews",
                column: "UserId");
        }
    }
}
