using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddScrappingFieldsToFridgeUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScrapped",
                table: "FridgeUnits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ScrappedByUserId",
                table: "FridgeUnits",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScrappedDate",
                table: "FridgeUnits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScrappingReason",
                table: "FridgeUnits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FridgeUnits_ScrappedByUserId",
                table: "FridgeUnits",
                column: "ScrappedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FridgeUnits_AspNetUsers_ScrappedByUserId",
                table: "FridgeUnits",
                column: "ScrappedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FridgeUnits_AspNetUsers_ScrappedByUserId",
                table: "FridgeUnits");

            migrationBuilder.DropIndex(
                name: "IX_FridgeUnits_ScrappedByUserId",
                table: "FridgeUnits");

            migrationBuilder.DropColumn(
                name: "IsScrapped",
                table: "FridgeUnits");

            migrationBuilder.DropColumn(
                name: "ScrappedByUserId",
                table: "FridgeUnits");

            migrationBuilder.DropColumn(
                name: "ScrappedDate",
                table: "FridgeUnits");

            migrationBuilder.DropColumn(
                name: "ScrappingReason",
                table: "FridgeUnits");
        }
    }
}
