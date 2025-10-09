using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Code_Vanta_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRecordTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FridgeId = table.Column<int>(type: "int", nullable: false),
                    DateOfService = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TechnicianUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ServiceDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FaultId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_AspNetUsers_TechnicianUserId",
                        column: x => x.TechnicianUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_FridgeFaults_FaultId",
                        column: x => x.FaultId,
                        principalTable: "FridgeFaults",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Fridges_FridgeId",
                        column: x => x.FridgeId,
                        principalTable: "Fridges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_FaultId",
                table: "ServiceRecords",
                column: "FaultId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_FridgeId",
                table: "ServiceRecords",
                column: "FridgeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_TechnicianUserId",
                table: "ServiceRecords",
                column: "TechnicianUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceRecords");
        }
    }
}
