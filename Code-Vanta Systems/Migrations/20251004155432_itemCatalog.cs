using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class itemCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequestItems_ItemCatalog_ItemCatalogId",
                table: "PurchaseRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequestItems_ItemCatalog_ItemCatalogId1",
                table: "PurchaseRequestItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemCatalog",
                table: "ItemCatalog");

            migrationBuilder.RenameTable(
                name: "ItemCatalog",
                newName: "ItemCatalogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemCatalogs",
                table: "ItemCatalogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequestItems_ItemCatalogs_ItemCatalogId",
                table: "PurchaseRequestItems",
                column: "ItemCatalogId",
                principalTable: "ItemCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequestItems_ItemCatalogs_ItemCatalogId1",
                table: "PurchaseRequestItems",
                column: "ItemCatalogId1",
                principalTable: "ItemCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequestItems_ItemCatalogs_ItemCatalogId",
                table: "PurchaseRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequestItems_ItemCatalogs_ItemCatalogId1",
                table: "PurchaseRequestItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemCatalogs",
                table: "ItemCatalogs");

            migrationBuilder.RenameTable(
                name: "ItemCatalogs",
                newName: "ItemCatalog");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemCatalog",
                table: "ItemCatalog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequestItems_ItemCatalog_ItemCatalogId",
                table: "PurchaseRequestItems",
                column: "ItemCatalogId",
                principalTable: "ItemCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequestItems_ItemCatalog_ItemCatalogId1",
                table: "PurchaseRequestItems",
                column: "ItemCatalogId1",
                principalTable: "ItemCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
