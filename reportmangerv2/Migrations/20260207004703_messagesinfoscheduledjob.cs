using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reportmangerv2.Migrations
{
    /// <inheritdoc />
    public partial class messagesinfoscheduledjob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DESCRIPTION",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AddColumn<string>(
                name: "CCMAILS",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MESSAGEBODY",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MESSAGESUBJECT",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SENDTOEMAILS",
                table: "SCHEDULEDJOBS",
                type: "NVARCHAR2(2000)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CCMAILS",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropColumn(
                name: "MESSAGEBODY",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropColumn(
                name: "MESSAGESUBJECT",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropColumn(
                name: "SENDTOEMAILS",
                table: "SCHEDULEDJOBS");

            migrationBuilder.AlterColumn<string>(
                name: "DESCRIPTION",
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
