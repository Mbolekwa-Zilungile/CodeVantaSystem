using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddedSupplierRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Suppliers");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Suppliers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ApplicationUserId",
                table: "Suppliers",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_AspNetUsers_ApplicationUserId",
                table: "Suppliers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_AspNetUsers_ApplicationUserId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_ApplicationUserId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Suppliers");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
