using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class one : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestQuotations_PurchaseRequests_PurchaseRequestId",
                table: "RequestQuotations");

            migrationBuilder.DropIndex(
                name: "IX_RequestQuotations_PurchaseRequestId",
                table: "RequestQuotations");

            migrationBuilder.DropColumn(
                name: "PurchaseRequestId",
                table: "RequestQuotations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseRequestId",
                table: "RequestQuotations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RequestQuotations_PurchaseRequestId",
                table: "RequestQuotations",
                column: "PurchaseRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestQuotations_PurchaseRequests_PurchaseRequestId",
                table: "RequestQuotations",
                column: "PurchaseRequestId",
                principalTable: "PurchaseRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
