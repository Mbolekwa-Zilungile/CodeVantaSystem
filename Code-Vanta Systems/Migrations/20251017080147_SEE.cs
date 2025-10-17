using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class SEE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuotationItem_PurchaseRequestItems_PurchaseRequestItemId",
                table: "QuotationItem");

            migrationBuilder.DropForeignKey(
                name: "FK_QuotationItem_Quotations_QuotationId",
                table: "QuotationItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuotationItem",
                table: "QuotationItem");

            migrationBuilder.RenameTable(
                name: "QuotationItem",
                newName: "QuotationItems");

            migrationBuilder.RenameIndex(
                name: "IX_QuotationItem_QuotationId",
                table: "QuotationItems",
                newName: "IX_QuotationItems_QuotationId");

            migrationBuilder.RenameIndex(
                name: "IX_QuotationItem_PurchaseRequestItemId",
                table: "QuotationItems",
                newName: "IX_QuotationItems_PurchaseRequestItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuotationItems",
                table: "QuotationItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationItems_PurchaseRequestItems_PurchaseRequestItemId",
                table: "QuotationItems",
                column: "PurchaseRequestItemId",
                principalTable: "PurchaseRequestItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationItems_Quotations_QuotationId",
                table: "QuotationItems",
                column: "QuotationId",
                principalTable: "Quotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuotationItems_PurchaseRequestItems_PurchaseRequestItemId",
                table: "QuotationItems");

            migrationBuilder.DropForeignKey(
                name: "FK_QuotationItems_Quotations_QuotationId",
                table: "QuotationItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuotationItems",
                table: "QuotationItems");

            migrationBuilder.RenameTable(
                name: "QuotationItems",
                newName: "QuotationItem");

            migrationBuilder.RenameIndex(
                name: "IX_QuotationItems_QuotationId",
                table: "QuotationItem",
                newName: "IX_QuotationItem_QuotationId");

            migrationBuilder.RenameIndex(
                name: "IX_QuotationItems_PurchaseRequestItemId",
                table: "QuotationItem",
                newName: "IX_QuotationItem_PurchaseRequestItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuotationItem",
                table: "QuotationItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationItem_PurchaseRequestItems_PurchaseRequestItemId",
                table: "QuotationItem",
                column: "PurchaseRequestItemId",
                principalTable: "PurchaseRequestItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationItem_Quotations_QuotationId",
                table: "QuotationItem",
                column: "QuotationId",
                principalTable: "Quotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
