using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DetectorVulnerabilitatsDatabase.Migrations
{
    /// <inheritdoc />
    public partial class removeResultsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Findings_Solutions_Solution_id",
                table: "Findings");

            migrationBuilder.DropTable(
                name: "Solutions");

            migrationBuilder.DropIndex(
                name: "IX_Findings_Solution_id",
                table: "Findings");

            migrationBuilder.AddColumn<string>(
                name: "Solution",
                table: "Findings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Solution",
                table: "Findings");

            migrationBuilder.CreateTable(
                name: "Solutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    References = table.Column<string>(type: "text", nullable: false),
                    Remediation_steps = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Solution_id",
                table: "Findings",
                column: "Solution_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Findings_Solutions_Solution_id",
                table: "Findings",
                column: "Solution_id",
                principalTable: "Solutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
