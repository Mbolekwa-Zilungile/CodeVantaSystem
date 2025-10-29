using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class expectedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDate",
                table: "RequestQuotations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedDate",
                table: "RequestQuotations");
        }
    }
}
