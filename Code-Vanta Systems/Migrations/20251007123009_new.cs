using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Fridges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Fridges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Fridges",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Fridges",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Fridges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RentalPricePerMonth",
                table: "Fridges",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StockQuantity",
                table: "Fridges",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Fridges");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Fridges");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Fridges");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Fridges");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Fridges");

            migrationBuilder.DropColumn(
                name: "RentalPricePerMonth",
                table: "Fridges");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Fridges");
        }
    }
}
