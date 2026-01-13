using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameworkVersionIsSystemData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemData",
                table: "FrameworkVersions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemData",
                table: "FrameworkVersions");
        }
    }
}
