using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class Technician : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FridgeUnits_AspNetUsers_TechnicianId",
                table: "FridgeUnits");

            migrationBuilder.DropIndex(
                name: "IX_FridgeUnits_TechnicianId",
                table: "FridgeUnits");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "FridgeUnits");

            migrationBuilder.AlterColumn<string>(
                name: "TechnicianUserId",
                table: "FridgeUnits",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FridgeUnits_TechnicianUserId",
                table: "FridgeUnits",
                column: "TechnicianUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FridgeUnits_AspNetUsers_TechnicianUserId",
                table: "FridgeUnits",
                column: "TechnicianUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FridgeUnits_AspNetUsers_TechnicianUserId",
                table: "FridgeUnits");

            migrationBuilder.DropIndex(
                name: "IX_FridgeUnits_TechnicianUserId",
                table: "FridgeUnits");

            migrationBuilder.AlterColumn<string>(
                name: "TechnicianUserId",
                table: "FridgeUnits",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianId",
                table: "FridgeUnits",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FridgeUnits_TechnicianId",
                table: "FridgeUnits",
                column: "TechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_FridgeUnits_AspNetUsers_TechnicianId",
                table: "FridgeUnits",
                column: "TechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
