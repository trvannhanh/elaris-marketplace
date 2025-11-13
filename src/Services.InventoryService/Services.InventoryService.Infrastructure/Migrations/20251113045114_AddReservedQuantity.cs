using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.InventoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReservedQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "InventoryItems");
        }
    }
}
