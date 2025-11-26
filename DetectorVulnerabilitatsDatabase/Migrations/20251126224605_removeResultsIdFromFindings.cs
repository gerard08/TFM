using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DetectorVulnerabilitatsDatabase.Migrations
{
    /// <inheritdoc />
    public partial class removeResultsIdFromFindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Solution_id",
                table: "Findings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Solution_id",
                table: "Findings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
