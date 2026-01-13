using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceNowIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceNowIncidents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IncidentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConfigurationItem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CloseCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CloseNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommentsAndWorkNotesRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntriesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedApplicationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LinkedApplicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LinkStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LinkStatusNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManuallyReviewed = table.Column<bool>(type: "bit", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawCsvValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceNowIncidents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNowIncidents_CloseCode",
                table: "ServiceNowIncidents",
                column: "CloseCode");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNowIncidents_ConfigurationItem",
                table: "ServiceNowIncidents",
                column: "ConfigurationItem");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNowIncidents_IncidentNumber",
                table: "ServiceNowIncidents",
                column: "IncidentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNowIncidents_LinkedApplicationId",
                table: "ServiceNowIncidents",
                column: "LinkedApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceNowIncidents_State",
                table: "ServiceNowIncidents",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceNowIncidents");
        }
    }
}
