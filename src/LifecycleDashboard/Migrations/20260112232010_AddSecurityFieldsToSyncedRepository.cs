using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityFieldsToSyncedRepository : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AdvancedSecurityEnabled",
                table: "SyncedRepositories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ClosedCriticalVulnerabilities",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClosedHighVulnerabilities",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClosedLowVulnerabilities",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClosedMediumVulnerabilities",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DependencyAlertCount",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExposedSecretsCount",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSecurityScanDate",
                table: "SyncedRepositories",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OpenCriticalVulnerabilities",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenHighVulnerabilities",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenLowVulnerabilities",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenMediumVulnerabilities",
                table: "SyncedRepositories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvancedSecurityEnabled",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "ClosedCriticalVulnerabilities",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "ClosedHighVulnerabilities",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "ClosedLowVulnerabilities",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "ClosedMediumVulnerabilities",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "DependencyAlertCount",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "ExposedSecretsCount",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "LastSecurityScanDate",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "OpenCriticalVulnerabilities",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "OpenHighVulnerabilities",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "OpenLowVulnerabilities",
                table: "SyncedRepositories");

            migrationBuilder.DropColumn(
                name: "OpenMediumVulnerabilities",
                table: "SyncedRepositories");
        }
    }
}
