using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFhirImportLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FhirImportLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FhirResourceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InternalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FhirImportLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FhirImportLogs_ImportedAt",
                table: "FhirImportLogs",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FhirImportLogs_ResourceType_Status",
                table: "FhirImportLogs",
                columns: new[] { "ResourceType", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FhirImportLogs");
        }
    }
}
