using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reportmangerv2.Migrations
{
    /// <inheritdoc />
    public partial class AddJobType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JOBTYPE",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: false,
                defaultValue: "StoredProcedure");

            migrationBuilder.AddColumn<string>(
                name: "SQLSTATEMENT",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JOBTYPE",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropColumn(
                name: "SQLSTATEMENT",
                table: "SCHEDULEDJOBS");
        }
    }
}
