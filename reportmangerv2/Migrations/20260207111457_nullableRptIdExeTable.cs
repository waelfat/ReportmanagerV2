using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reportmangerv2.Migrations
{
    /// <inheritdoc />
    public partial class nullableRptIdExeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "REPORTID",
                table: "EXECUTIONS",
                type: "NVARCHAR2(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "REPORTID",
                table: "EXECUTIONS",
                type: "NVARCHAR2(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)",
                oldNullable: true);
        }
    }
}
