using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace reportmangerv2.Migrations
{
    /// <inheritdoc />
    public partial class initialmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ASPNETROLES",
                columns: table => new
                {
                    ID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NORMALIZEDNAME = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    CONCURRENCYSTAMP = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETROLES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    FULLNAME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ISACTIVE = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    USERNAME = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NORMALIZEDUSERNAME = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NORMALIZEDEMAIL = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    EMAILCONFIRMED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    PASSWORDHASH = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SECURITYSTAMP = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CONCURRENCYSTAMP = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PHONENUMBER = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PHONENUMBERCONFIRMED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    TWOFACTORENABLED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    LOCKOUTEND = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    LOCKOUTENABLED = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ACCESSFAILEDCOUNT = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SCHEMAS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CREATEDAT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    HOST = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    PORT = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SERVICENAME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    USERID = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    PASSWORD = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEMAS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETROLECLAIMS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ROLEID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CLAIMTYPE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CLAIMVALUE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETROLECLAIMS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ASPNETROLECLAIMS_ASPNETROL~",
                        column: x => x.ROLEID,
                        principalTable: "ASPNETROLES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERCLAIMS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    USERID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CLAIMTYPE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CLAIMVALUE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERCLAIMS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ASPNETUSERCLAIMS_ASPNETUSE~",
                        column: x => x.USERID,
                        principalTable: "ASPNETUSERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERLOGINS",
                columns: table => new
                {
                    LOGINPROVIDER = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    PROVIDERKEY = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    PROVIDERDISPLAYNAME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    USERID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERLOGINS", x => new { x.LOGINPROVIDER, x.PROVIDERKEY });
                    table.ForeignKey(
                        name: "FK_ASPNETUSERLOGINS_ASPNETUSE~",
                        column: x => x.USERID,
                        principalTable: "ASPNETUSERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERROLES",
                columns: table => new
                {
                    USERID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ROLEID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERROLES", x => new { x.USERID, x.ROLEID });
                    table.ForeignKey(
                        name: "FK_ASPNETUSERROLES_ASPNETROLE~",
                        column: x => x.ROLEID,
                        principalTable: "ASPNETROLES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ASPNETUSERROLES_ASPNETUSER~",
                        column: x => x.USERID,
                        principalTable: "ASPNETUSERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ASPNETUSERTOKENS",
                columns: table => new
                {
                    USERID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    LOGINPROVIDER = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    VALUE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASPNETUSERTOKENS", x => new { x.USERID, x.LOGINPROVIDER, x.NAME });
                    table.ForeignKey(
                        name: "FK_ASPNETUSERTOKENS_ASPNETUSE~",
                        column: x => x.USERID,
                        principalTable: "ASPNETUSERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CATEGORIES",
                columns: table => new
                {
                    ID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CREATEDAT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    CREATEDBYID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    PARENTCATEGORYID = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORIES", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CATEGORIES_ASPNETUSERS_CRE~",
                        column: x => x.CREATEDBYID,
                        principalTable: "ASPNETUSERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CATEGORIES_CATEGORIES_PARE~",
                        column: x => x.PARENTCATEGORYID,
                        principalTable: "CATEGORIES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULEDJOB",
                columns: table => new
                {
                    ID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    PROCEDURENAME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CRONEXPRESSION = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ISACTIVE = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CREATEDAT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LASTRUNAT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    NEXTRUNAT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    CREATEDBYID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Parameters = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULEDJOB", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SCHEDULEDJOB_ASPNETUSERS_C~",
                        column: x => x.CREATEDBYID,
                        principalTable: "ASPNETUSERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "REPORTS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    REPORTQUERY = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CREATEDDATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    MODIFIEDDATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    CREATEDBYID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    SCHEMAID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ISACTIVE = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CATEGORYID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORTS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_REPORTS_ASPNETUSERS_CREATE~",
                        column: x => x.CREATEDBYID,
                        principalTable: "ASPNETUSERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_REPORTS_CATEGORIES_CATEGOR~",
                        column: x => x.CATEGORYID,
                        principalTable: "CATEGORIES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_REPORTS_SCHEMAS_SCHEMAID",
                        column: x => x.SCHEMAID,
                        principalTable: "SCHEMAS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EXECUTIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    REPORTID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    EXECUTIONDATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    EXECUTIONTYPE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EXECUTIONSTATUS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    USERID = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    SCHEDULEDJOBID = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    RESULTFILEPATH = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ERRORMESSAGE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DURATION = table.Column<TimeSpan>(type: "INTERVAL DAY(8) TO SECOND(7)", nullable: false),
                    ExecutionParameters = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EXECUTIONS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EXECUTIONS_ASPNETUSERS_USE~",
                        column: x => x.USERID,
                        principalTable: "ASPNETUSERS",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_EXECUTIONS_REPORTS_REPORTID",
                        column: x => x.REPORTID,
                        principalTable: "REPORTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EXECUTIONS_SCHEDULEDJOB_SC~",
                        column: x => x.SCHEDULEDJOBID,
                        principalTable: "SCHEDULEDJOB",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "REPORTDETAILS",
                columns: table => new
                {
                    NAME = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    REPORTID = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ID = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    DESCRIPTION = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TYPE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DEFAULTVALUE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ISREQUIRED = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORTDETAILS", x => new { x.REPORTID, x.NAME });
                    table.ForeignKey(
                        name: "FK_REPORTDETAILS_REPORTS_REPO~",
                        column: x => x.REPORTID,
                        principalTable: "REPORTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ASPNETROLECLAIMS_ROLEID",
                table: "ASPNETROLECLAIMS",
                column: "ROLEID");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "ASPNETROLES",
                column: "NORMALIZEDNAME",
                unique: true,
                filter: "\"NORMALIZEDNAME\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ASPNETUSERCLAIMS_USERID",
                table: "ASPNETUSERCLAIMS",
                column: "USERID");

            migrationBuilder.CreateIndex(
                name: "IX_ASPNETUSERLOGINS_USERID",
                table: "ASPNETUSERLOGINS",
                column: "USERID");

            migrationBuilder.CreateIndex(
                name: "IX_ASPNETUSERROLES_ROLEID",
                table: "ASPNETUSERROLES",
                column: "ROLEID");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "ASPNETUSERS",
                column: "NORMALIZEDEMAIL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "ASPNETUSERS",
                column: "NORMALIZEDUSERNAME",
                unique: true,
                filter: "\"NORMALIZEDUSERNAME\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORIES_CREATEDBYID",
                table: "CATEGORIES",
                column: "CREATEDBYID");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORIES_NAME",
                table: "CATEGORIES",
                column: "NAME");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORIES_NAME_PARENTCATE~",
                table: "CATEGORIES",
                columns: new[] { "NAME", "PARENTCATEGORYID" },
                unique: true,
                filter: "\"PARENTCATEGORYID\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORIES_PARENTCATEGORYID",
                table: "CATEGORIES",
                column: "PARENTCATEGORYID");

            migrationBuilder.CreateIndex(
                name: "IX_EXECUTIONS_REPORTID",
                table: "EXECUTIONS",
                column: "REPORTID");

            migrationBuilder.CreateIndex(
                name: "IX_EXECUTIONS_SCHEDULEDJOBID",
                table: "EXECUTIONS",
                column: "SCHEDULEDJOBID");

            migrationBuilder.CreateIndex(
                name: "IX_EXECUTIONS_USERID",
                table: "EXECUTIONS",
                column: "USERID");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTS_CATEGORYID",
                table: "REPORTS",
                column: "CATEGORYID");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTS_CREATEDBYID",
                table: "REPORTS",
                column: "CREATEDBYID");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTS_SCHEMAID",
                table: "REPORTS",
                column: "SCHEMAID");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULEDJOB_CREATEDBYID",
                table: "SCHEDULEDJOB",
                column: "CREATEDBYID");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEMAS_NAME",
                table: "SCHEMAS",
                column: "NAME",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ASPNETROLECLAIMS");

            migrationBuilder.DropTable(
                name: "ASPNETUSERCLAIMS");

            migrationBuilder.DropTable(
                name: "ASPNETUSERLOGINS");

            migrationBuilder.DropTable(
                name: "ASPNETUSERROLES");

            migrationBuilder.DropTable(
                name: "ASPNETUSERTOKENS");

            migrationBuilder.DropTable(
                name: "EXECUTIONS");

            migrationBuilder.DropTable(
                name: "REPORTDETAILS");

            migrationBuilder.DropTable(
                name: "ASPNETROLES");

            migrationBuilder.DropTable(
                name: "SCHEDULEDJOB");

            migrationBuilder.DropTable(
                name: "REPORTS");

            migrationBuilder.DropTable(
                name: "CATEGORIES");

            migrationBuilder.DropTable(
                name: "SCHEMAS");

            migrationBuilder.DropTable(
                name: "ASPNETUSERS");
        }
    }
}
