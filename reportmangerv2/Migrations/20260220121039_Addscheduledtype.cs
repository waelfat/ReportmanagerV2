using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reportmangerv2.Migrations
{
    /// <inheritdoc />
    public partial class Addscheduledtype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SENDTOEMAILS",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AddColumn<string>(
                name: "SCHEDULEDTYPE",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SCHEDULEDTYPE",
                table: "SCHEDULEDJOBS");

            migrationBuilder.AlterColumn<string>(
                name: "SENDTOEMAILS",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);
        }
    }
}
