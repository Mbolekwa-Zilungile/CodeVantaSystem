using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class newAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "FridgeFaults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "FridgeFaults");
        }
    }
}
