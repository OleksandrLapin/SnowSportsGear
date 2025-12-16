using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductSizeVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "322dc9a3-1a74-4cbf-8a47-3bdebe3f4c75");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f10ba208-f5c2-4595-8051-5f6bea4b3e44");

            migrationBuilder.CreateTable(
                name: "ProductVariant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Size = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    QuantityInStock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ProductId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariant_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO ProductVariant (Size, QuantityInStock, ProductId)
                SELECT 'UNI', QuantityInStock, Id FROM Products
            """);

            migrationBuilder.DropColumn(
                name: "QuantityInStock",
                table: "Products");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "721a3f0b-a372-4275-960e-5b7c1f4db5e1", null, "Customer", "CUSTOMER" },
                    { "d0ad46c1-f0c0-4f13-af5c-c28aa2ff7c16", null, "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_ProductId_Size",
                table: "ProductVariant",
                columns: new[] { "ProductId", "Size" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariant");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "721a3f0b-a372-4275-960e-5b7c1f4db5e1");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d0ad46c1-f0c0-4f13-af5c-c28aa2ff7c16");

            migrationBuilder.AddColumn<int>(
                name: "QuantityInStock",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "322dc9a3-1a74-4cbf-8a47-3bdebe3f4c75", null, "Customer", "CUSTOMER" },
                    { "f10ba208-f5c2-4595-8051-5f6bea4b3e44", null, "Admin", "ADMIN" }
                });
        }
    }
}
