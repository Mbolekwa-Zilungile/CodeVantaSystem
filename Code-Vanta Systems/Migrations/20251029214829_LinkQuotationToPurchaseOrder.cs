using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class LinkQuotationToPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuotationId",
                table: "PurchaseOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_QuotationId",
                table: "PurchaseOrders",
                column: "QuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Quotations_QuotationId",
                table: "PurchaseOrders",
                column: "QuotationId",
                principalTable: "Quotations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Quotations_QuotationId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_QuotationId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "PurchaseOrders");
        }
    }
}
