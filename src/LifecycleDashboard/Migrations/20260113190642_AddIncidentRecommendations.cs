using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IncidentRecommendations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApplicationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApplicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RootCauseAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedImpact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedEffort = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedCloseCodesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedIncidentNumbersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncidentCount = table.Column<int>(type: "int", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentRecommendations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRecommendations_ApplicationId",
                table: "IncidentRecommendations",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRecommendations_GeneratedAt",
                table: "IncidentRecommendations",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRecommendations_Priority",
                table: "IncidentRecommendations",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRecommendations_Status",
                table: "IncidentRecommendations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRecommendations_Type",
                table: "IncidentRecommendations",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentRecommendations");
        }
    }
}
