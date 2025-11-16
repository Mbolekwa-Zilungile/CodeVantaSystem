using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerConfirmationToMaintenanceSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerConfirmation",
                table: "MaintenanceSchedules",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerConfirmation",
                table: "MaintenanceSchedules");
        }
    }
}
