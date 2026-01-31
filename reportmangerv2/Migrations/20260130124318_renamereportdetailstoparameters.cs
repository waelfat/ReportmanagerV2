using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reportmangerv2.Migrations
{
    /// <inheritdoc />
    public partial class renamereportdetailstoparameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EXECUTIONS_SCHEDULEDJOB_SC~",
                table: "EXECUTIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_REPORTDETAILS_REPORTS_REPO~",
                table: "REPORTDETAILS");

            migrationBuilder.DropForeignKey(
                name: "FK_SCHEDULEDJOB_ASPNETUSERS_C~",
                table: "SCHEDULEDJOB");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SCHEDULEDJOB",
                table: "SCHEDULEDJOB");

            migrationBuilder.DropPrimaryKey(
                name: "PK_REPORTDETAILS",
                table: "REPORTDETAILS");

            migrationBuilder.RenameTable(
                name: "SCHEDULEDJOB",
                newName: "SCHEDULEDJOBS");

            migrationBuilder.RenameTable(
                name: "REPORTDETAILS",
                newName: "REPORTPARAMETERS");

            migrationBuilder.RenameIndex(
                name: "IX_SCHEDULEDJOB_CREATEDBYID",
                table: "SCHEDULEDJOBS",
                newName: "IX_SCHEDULEDJOBS_CREATEDBYID");

            migrationBuilder.AddColumn<int>(
                name: "POSITION",
                table: "REPORTPARAMETERS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SCHEDULEDJOBS",
                table: "SCHEDULEDJOBS",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_REPORTPARAMETERS",
                table: "REPORTPARAMETERS",
                columns: new[] { "REPORTID", "NAME" });

            migrationBuilder.AddForeignKey(
                name: "FK_EXECUTIONS_SCHEDULEDJOBS_S~",
                table: "EXECUTIONS",
                column: "SCHEDULEDJOBID",
                principalTable: "SCHEDULEDJOBS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTPARAMETERS_REPORTS_R~",
                table: "REPORTPARAMETERS",
                column: "REPORTID",
                principalTable: "REPORTS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SCHEDULEDJOBS_ASPNETUSERS_~",
                table: "SCHEDULEDJOBS",
                column: "CREATEDBYID",
                principalTable: "ASPNETUSERS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EXECUTIONS_SCHEDULEDJOBS_S~",
                table: "EXECUTIONS");

            migrationBuilder.DropForeignKey(
                name: "FK_REPORTPARAMETERS_REPORTS_R~",
                table: "REPORTPARAMETERS");

            migrationBuilder.DropForeignKey(
                name: "FK_SCHEDULEDJOBS_ASPNETUSERS_~",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SCHEDULEDJOBS",
                table: "SCHEDULEDJOBS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_REPORTPARAMETERS",
                table: "REPORTPARAMETERS");

            migrationBuilder.DropColumn(
                name: "POSITION",
                table: "REPORTPARAMETERS");

            migrationBuilder.RenameTable(
                name: "SCHEDULEDJOBS",
                newName: "SCHEDULEDJOB");

            migrationBuilder.RenameTable(
                name: "REPORTPARAMETERS",
                newName: "REPORTDETAILS");

            migrationBuilder.RenameIndex(
                name: "IX_SCHEDULEDJOBS_CREATEDBYID",
                table: "SCHEDULEDJOB",
                newName: "IX_SCHEDULEDJOB_CREATEDBYID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SCHEDULEDJOB",
                table: "SCHEDULEDJOB",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_REPORTDETAILS",
                table: "REPORTDETAILS",
                columns: new[] { "REPORTID", "NAME" });

            migrationBuilder.AddForeignKey(
                name: "FK_EXECUTIONS_SCHEDULEDJOB_SC~",
                table: "EXECUTIONS",
                column: "SCHEDULEDJOBID",
                principalTable: "SCHEDULEDJOB",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTDETAILS_REPORTS_REPO~",
                table: "REPORTDETAILS",
                column: "REPORTID",
                principalTable: "REPORTS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SCHEDULEDJOB_ASPNETUSERS_C~",
                table: "SCHEDULEDJOB",
                column: "CREATEDBYID",
                principalTable: "ASPNETUSERS",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
