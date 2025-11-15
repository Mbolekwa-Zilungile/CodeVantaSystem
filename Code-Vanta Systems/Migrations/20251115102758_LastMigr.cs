using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class LastMigr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSchedules_AspNetUsers_TechnicianId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSchedules_FridgeUnits_FridgeUnitId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRecords_Fridges_FridgeId",
                table: "ServiceRecords");

            migrationBuilder.AlterColumn<int>(
                name: "FridgeId",
                table: "ServiceRecords",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "ServiceRecords",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MaintenanceTypeId",
                table: "ServiceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FridgeUnitId",
                table: "MaintenanceSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSchedules_AspNetUsers_TechnicianId",
                table: "MaintenanceSchedules",
                column: "TechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSchedules_FridgeUnits_FridgeUnitId",
                table: "MaintenanceSchedules",
                column: "FridgeUnitId",
                principalTable: "FridgeUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRecords_Fridges_FridgeId",
                table: "ServiceRecords",
                column: "FridgeId",
                principalTable: "Fridges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict); // ✅ Use Restrict
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSchedules_AspNetUsers_TechnicianId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSchedules_FridgeUnits_FridgeUnitId",
                table: "MaintenanceSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRecords_Fridges_FridgeId",
                table: "ServiceRecords");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "ServiceRecords");

            migrationBuilder.DropColumn(
                name: "MaintenanceTypeId",
                table: "ServiceRecords");

            migrationBuilder.AlterColumn<int>(
                name: "FridgeId",
                table: "ServiceRecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FridgeUnitId",
                table: "MaintenanceSchedules",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSchedules_AspNetUsers_TechnicianId",
                table: "MaintenanceSchedules",
                column: "TechnicianId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSchedules_FridgeUnits_FridgeUnitId",
                table: "MaintenanceSchedules",
                column: "FridgeUnitId",
                principalTable: "FridgeUnits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRecords_Fridges_FridgeId",
                table: "ServiceRecords",
                column: "FridgeId",
                principalTable: "Fridges",
                principalColumn: "Id");
        }
    }
}
