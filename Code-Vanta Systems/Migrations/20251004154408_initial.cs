using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelRequested",
                table: "PurchaseRequests");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "PurchaseRequests",
                newName: "Priority");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "PurchaseRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDate",
                table: "PurchaseRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PurchaseRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseRequestId = table.Column<int>(type: "int", nullable: false),
                    ItemCatalogId = table.Column<int>(type: "int", nullable: false),
                    ItemCatalogId1 = table.Column<int>(type: "int", nullable: false),
                    ModelRequested = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestItems_ItemCatalog_ItemCatalogId",
                        column: x => x.ItemCatalogId,
                        principalTable: "ItemCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestItems_ItemCatalog_ItemCatalogId1",
                        column: x => x.ItemCatalogId1,
                        principalTable: "ItemCatalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestItems_PurchaseRequests_PurchaseRequestId",
                        column: x => x.PurchaseRequestId,
                        principalTable: "PurchaseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_ApplicationUserId",
                table: "PurchaseRequests",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestItems_ItemCatalogId",
                table: "PurchaseRequestItems",
                column: "ItemCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestItems_ItemCatalogId1",
                table: "PurchaseRequestItems",
                column: "ItemCatalogId1");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestItems_PurchaseRequestId",
                table: "PurchaseRequestItems",
                column: "PurchaseRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRequests_AspNetUsers_ApplicationUserId",
                table: "PurchaseRequests",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRequests_AspNetUsers_ApplicationUserId",
                table: "PurchaseRequests");

            migrationBuilder.DropTable(
                name: "PurchaseRequestItems");

            migrationBuilder.DropTable(
                name: "ItemCatalog");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseRequests_ApplicationUserId",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "ExpectedDate",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PurchaseRequests");

            migrationBuilder.RenameColumn(
                name: "Priority",
                table: "PurchaseRequests",
                newName: "Quantity");

            migrationBuilder.AddColumn<string>(
                name: "ModelRequested",
                table: "PurchaseRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
