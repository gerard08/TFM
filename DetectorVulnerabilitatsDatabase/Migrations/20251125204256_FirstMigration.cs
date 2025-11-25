using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DetectorVulnerabilitatsDatabase.Migrations
{
    /// <inheritdoc />
    public partial class FirstMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ip = table.Column<string>(type: "text", nullable: false),
                    First_scanned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Last_scanned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Solutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Remediation_steps = table.Column<string>(type: "text", nullable: false),
                    References = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scan_type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Requested_by = table.Column<string>(type: "text", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanTasks_Assets_Asset_id",
                        column: x => x.Asset_id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScanResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scan_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanResults_ScanTasks_Scan_task_id",
                        column: x => x.Scan_task_id,
                        principalTable: "ScanTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scan_result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Solution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Cve_id = table.Column<string>(type: "text", nullable: false),
                    Affected_service = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Findings_ScanResults_Scan_result_id",
                        column: x => x.Scan_result_id,
                        principalTable: "ScanResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Findings_Solutions_Solution_id",
                        column: x => x.Solution_id,
                        principalTable: "Solutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Scan_result_id",
                table: "Findings",
                column: "Scan_result_id");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Solution_id",
                table: "Findings",
                column: "Solution_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScanResults_Scan_task_id",
                table: "ScanResults",
                column: "Scan_task_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScanTasks_Asset_id",
                table: "ScanTasks",
                column: "Asset_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "ScanResults");

            migrationBuilder.DropTable(
                name: "Solutions");

            migrationBuilder.DropTable(
                name: "ScanTasks");

            migrationBuilder.DropTable(
                name: "Assets");
        }
    }
}
