using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddFridgeUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FridgeUnitId",
                table: "MaintenanceSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_FridgeUnitId",
                table: "MaintenanceSchedules",
                column: "FridgeUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSchedules_FridgeUnits_FridgeUnitId",
                table: "MaintenanceSchedules",
                column: "FridgeUnitId",
                principalTable: "FridgeUnits",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSchedules_FridgeUnits_FridgeUnitId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceSchedules_FridgeUnitId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropColumn(
                name: "FridgeUnitId",
                table: "MaintenanceSchedules");
        }
    }
}
