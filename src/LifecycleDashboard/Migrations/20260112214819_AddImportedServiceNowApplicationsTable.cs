using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifecycleDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedServiceNowApplicationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportedServiceNowApplications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ServiceNowId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Capability = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProductManagerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BusinessOwnerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BusinessOwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FunctionalArchitectId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FunctionalArchitectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TechnicalArchitectId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TechnicalArchitectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TechnicalLeadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TechnicalLeadName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApplicationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ArchitectureType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserBase = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Importance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RepositoryUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Criticality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupportGroup = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawCsvValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedRepositoryId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LinkedRepositoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedServiceNowApplications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedServiceNowApplications_Capability",
                table: "ImportedServiceNowApplications",
                column: "Capability");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedServiceNowApplications_ImportedAt",
                table: "ImportedServiceNowApplications",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedServiceNowApplications_Name",
                table: "ImportedServiceNowApplications",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedServiceNowApplications_ServiceNowId",
                table: "ImportedServiceNowApplications",
                column: "ServiceNowId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedServiceNowApplications");
        }
    }
}
