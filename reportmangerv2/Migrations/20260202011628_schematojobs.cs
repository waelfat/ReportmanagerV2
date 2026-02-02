using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reportmangerv2.Migrations
{
    /// <inheritdoc />
    public partial class schematojobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JOBSTATUS",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SCHEMAID",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULEDJOBS_SCHEMAID",
                table: "SCHEDULEDJOBS",
                column: "SCHEMAID");

            migrationBuilder.AddForeignKey(
                name: "FK_SCHEDULEDJOBS_SCHEMAS_SCHE~",
                table: "SCHEDULEDJOBS",
                column: "SCHEMAID",
                principalTable: "SCHEMAS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SCHEDULEDJOBS_SCHEMAS_SCHE~",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropIndex(
                name: "IX_SCHEDULEDJOBS_SCHEMAID",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropColumn(
                name: "JOBSTATUS",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropColumn(
                name: "SCHEMAID",
                table: "SCHEDULEDJOBS");
        }
    }
}
