using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameworkVersionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FrameworkVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Framework = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReleaseDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndOfLifeDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndOfActiveSupportDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsLts = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LatestPatchVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecommendedUpgradePath = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AutoDetected = table.Column<bool>(type: "bit", nullable: false),
                    TargetFrameworkMoniker = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkVersions_Framework",
                table: "FrameworkVersions",
                column: "Framework");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkVersions_Framework_Version",
                table: "FrameworkVersions",
                columns: new[] { "Framework", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkVersions_TargetFrameworkMoniker",
                table: "FrameworkVersions",
                column: "TargetFrameworkMoniker");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkVersions_Version",
                table: "FrameworkVersions",
                column: "Version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FrameworkVersions");
        }
    }
}
