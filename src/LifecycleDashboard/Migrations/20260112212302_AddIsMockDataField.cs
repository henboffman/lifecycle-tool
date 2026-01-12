using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddIsMockDataField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMockData",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Mark all existing applications as mock data (they were seeded)
            migrationBuilder.Sql("UPDATE Applications SET IsMockData = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMockData",
                table: "Applications");
        }
    }
}
