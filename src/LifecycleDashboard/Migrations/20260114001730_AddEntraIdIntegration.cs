using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddEntraIdIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartedUserAlerts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    UnmatchedValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RoleType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResolvedByUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ResolvedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReplacementUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ReplacementUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LinkedTaskId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartedUserAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntraUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    UserPrincipalName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GivenName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Mail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OfficeLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagerId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    AccountEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PhotoData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PhotoContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhotoLastUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EntraLastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntraUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportTracking",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    NewRecords = table.Column<int>(type: "int", nullable: false),
                    UpdatedRecords = table.Column<int>(type: "int", nullable: false),
                    SkippedRecords = table.Column<int>(type: "int", nullable: false),
                    ErrorRecords = table.Column<int>(type: "int", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ImportedByUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ImportedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportTracking", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAliases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    EntraUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OriginalValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DiscoveredFrom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAliases_EntraUsers_EntraUserId",
                        column: x => x.EntraUserId,
                        principalTable: "EntraUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartedUserAlerts_ApplicationId",
                table: "DepartedUserAlerts",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartedUserAlerts_DetectedAt",
                table: "DepartedUserAlerts",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DepartedUserAlerts_Status",
                table: "DepartedUserAlerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DepartedUserAlerts_UnmatchedValue_ValueType",
                table: "DepartedUserAlerts",
                columns: new[] { "UnmatchedValue", "ValueType" });

            migrationBuilder.CreateIndex(
                name: "IX_EntraUsers_DisplayName",
                table: "EntraUsers",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_EntraUsers_EmployeeId",
                table: "EntraUsers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EntraUsers_Mail",
                table: "EntraUsers",
                column: "Mail");

            migrationBuilder.CreateIndex(
                name: "IX_EntraUsers_UserPrincipalName",
                table: "EntraUsers",
                column: "UserPrincipalName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportTracking_DataSource",
                table: "ImportTracking",
                column: "DataSource");

            migrationBuilder.CreateIndex(
                name: "IX_ImportTracking_DataSource_FileHash",
                table: "ImportTracking",
                columns: new[] { "DataSource", "FileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportTracking_FileHash",
                table: "ImportTracking",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_ImportTracking_ImportedAt",
                table: "ImportTracking",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserAliases_EntraUserId",
                table: "UserAliases",
                column: "EntraUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAliases_Type_Value",
                table: "UserAliases",
                columns: new[] { "Type", "Value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartedUserAlerts");

            migrationBuilder.DropTable(
                name: "ImportTracking");

            migrationBuilder.DropTable(
                name: "UserAliases");

            migrationBuilder.DropTable(
                name: "EntraUsers");
        }
    }
}
