using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reportmangerv2.Migrations
{
    /// <inheritdoc />
    public partial class cascadingparameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DEPENDENCYQUERY",
                table: "REPORTPARAMETERS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DEPENDSON",
                table: "REPORTPARAMETERS",
                type: "NVARCHAR2(2000)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DEPENDENCYQUERY",
                table: "REPORTPARAMETERS");

            migrationBuilder.DropColumn(
                name: "DEPENDSON",
                table: "REPORTPARAMETERS");
        }
    }
}
