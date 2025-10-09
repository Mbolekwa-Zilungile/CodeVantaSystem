using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicianIdToFridgeConfigured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TechnicianUserId",
                table: "Fridges",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Fridges_TechnicianUserId",
                table: "Fridges",
                column: "TechnicianUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fridges_AspNetUsers_TechnicianUserId",
                table: "Fridges",
                column: "TechnicianUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fridges_AspNetUsers_TechnicianUserId",
                table: "Fridges");

            migrationBuilder.DropIndex(
                name: "IX_Fridges_TechnicianUserId",
                table: "Fridges");

            migrationBuilder.DropColumn(
                name: "TechnicianUserId",
                table: "Fridges");
        }
    }
}
