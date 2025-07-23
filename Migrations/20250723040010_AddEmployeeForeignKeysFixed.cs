using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ideku.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeForeignKeysFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nama_category = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "divisi",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(10)", nullable: false),
                    nama_divisi = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_divisi", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nama_event = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "varchar(20)", nullable: false),
                    Description = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departement",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(10)", nullable: false),
                    nama_departement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    divisi_id = table.Column<string>(type: "varchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departement", x => x.id);
                    table.ForeignKey(
                        name: "FK_departement_divisi_divisi_id",
                        column: x => x.divisi_id,
                        principalTable: "divisi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ideas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    cInitiator = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    cDivision = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    cDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    category_id = table.Column<int>(type: "int", nullable: true),
                    event_id = table.Column<int>(type: "int", nullable: true),
                    cIdea_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    cIdea_issue_background = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cIdea_solution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nSaving_cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    cAttachment_file = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dSubmitted_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dUpdated_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    nCurrent_stage = table.Column<int>(type: "int", nullable: true),
                    cCurrent_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cImsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    flag_status = table.Column<bool>(type: "bit", nullable: true),
                    cSavingCostOption = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    rejectReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    catReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    nSavingCostValidated = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    cSavingCostOptionValidated = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    attachmentMonitoring = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cFlagFlow = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cIdeaType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    flagFinance = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ideas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ideas_category_category_id",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ideas_event_event_id",
                        column: x => x.event_id,
                        principalTable: "event",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(10)", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", nullable: false),
                    departement_id = table.Column<string>(type: "varchar(10)", nullable: true),
                    divisi_id = table.Column<string>(type: "varchar(10)", nullable: true),
                    PositionTitle = table.Column<string>(type: "varchar(100)", nullable: false),
                    Position_Lvl = table.Column<string>(type: "varchar(10)", nullable: true),
                    Emp_Status = table.Column<string>(type: "varchar(10)", nullable: true),
                    LdapUser = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_departement_departement_id",
                        column: x => x.departement_id,
                        principalTable: "departement",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_employees_divisi_divisi_id",
                        column: x => x.divisi_id,
                        principalTable: "divisi",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_id = table.Column<string>(type: "varchar(10)", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(50)", nullable: false),
                    Name = table.Column<string>(type: "varchar(150)", nullable: false),
                    FlagActing = table.Column<string>(type: "varchar(50)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_departement_divisi_id",
                table: "departement",
                column: "divisi_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_departement_id",
                table: "employees",
                column: "departement_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_divisi_id",
                table: "employees",
                column: "divisi_id");

            migrationBuilder.CreateIndex(
                name: "IX_ideas_category_id",
                table: "ideas",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_ideas_event_id",
                table: "ideas",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_employee_id",
                table: "users",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ideas");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "event");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "departement");

            migrationBuilder.DropTable(
                name: "divisi");
        }
    }
}
