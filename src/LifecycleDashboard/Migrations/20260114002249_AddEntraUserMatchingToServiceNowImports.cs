using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddEntraUserMatchingToServiceNowImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessOwnerEntraId",
                table: "ImportedServiceNowApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionalArchitectEntraId",
                table: "ImportedServiceNowApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerEntraId",
                table: "ImportedServiceNowApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductManagerEntraId",
                table: "ImportedServiceNowApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalArchitectEntraId",
                table: "ImportedServiceNowApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalLeadEntraId",
                table: "ImportedServiceNowApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserMatchingMatchedCount",
                table: "ImportedServiceNowApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UserMatchingPerformedAt",
                table: "ImportedServiceNowApplications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserMatchingUnmatchedCount",
                table: "ImportedServiceNowApplications",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessOwnerEntraId",
                table: "ImportedServiceNowApplications");

            migrationBuilder.DropColumn(
                name: "FunctionalArchitectEntraId",
                table: "ImportedServiceNowApplications");

            migrationBuilder.DropColumn(
                name: "OwnerEntraId",
                table: "ImportedServiceNowApplications");

            migrationBuilder.DropColumn(
                name: "ProductManagerEntraId",
                table: "ImportedServiceNowApplications");

            migrationBuilder.DropColumn(
                name: "TechnicalArchitectEntraId",
                table: "ImportedServiceNowApplications");

            migrationBuilder.DropColumn(
                name: "TechnicalLeadEntraId",
                table: "ImportedServiceNowApplications");

            migrationBuilder.DropColumn(
                name: "UserMatchingMatchedCount",
                table: "ImportedServiceNowApplications");

            migrationBuilder.DropColumn(
                name: "UserMatchingPerformedAt",
                table: "ImportedServiceNowApplications");

            migrationBuilder.DropColumn(
                name: "UserMatchingUnmatchedCount",
                table: "ImportedServiceNowApplications");
        }
    }
}
