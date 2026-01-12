using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncedRepositoriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncedRepositories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CloneUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultBranch = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SyncedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryStack = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetFramework = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DetectedPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalCommits = table.Column<int>(type: "int", nullable: true),
                    LastCommitDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NuGetPackageCount = table.Column<int>(type: "int", nullable: false),
                    NpmPackageCount = table.Column<int>(type: "int", nullable: false),
                    LastBuildStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastBuildResult = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastBuildDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HasReadme = table.Column<bool>(type: "bit", nullable: false),
                    ReadmeQualityScore = table.Column<int>(type: "int", nullable: true),
                    LinkedApplicationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LinkedApplicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FrameworksJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LanguagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContributorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PackagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncedRepositories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncedRepositories_LinkedApplicationId",
                table: "SyncedRepositories",
                column: "LinkedApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedRepositories_Name",
                table: "SyncedRepositories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedRepositories_PrimaryStack",
                table: "SyncedRepositories",
                column: "PrimaryStack");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedRepositories_SyncedAt",
                table: "SyncedRepositories",
                column: "SyncedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncedRepositories");
        }
    }
}
