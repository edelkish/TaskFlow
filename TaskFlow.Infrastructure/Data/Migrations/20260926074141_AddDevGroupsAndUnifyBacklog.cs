using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDevGroupsAndUnifyBacklog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_PlanningTasks_TaskGroupId_Number",
                table: "PlanningTasks");

            migrationBuilder.DropIndex(
                name: "IX_PlanningTasks_TaskGroupId_Number_SubNumber",
                table: "PlanningTasks");

            migrationBuilder.AddColumn<Guid>(
                name: "DevGroupId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TaskGroupId",
                table: "PlanningTasks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<int>(
                name: "Number",
                table: "PlanningTasks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "PlanningTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DevGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_CI_AI"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DevGroupMembers",
                columns: table => new
                {
                    DevGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevGroupMembers", x => new { x.DevGroupId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_DevGroupMembers_DevGroups_DevGroupId",
                        column: x => x.DevGroupId,
                        principalTable: "DevGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevGroupMembers_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DevGroupId",
                table: "Projects",
                column: "DevGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningTasks_ProjectId_TaskGroupId",
                table: "PlanningTasks",
                columns: new[] { "ProjectId", "TaskGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningTasks_TaskGroupId_Number",
                table: "PlanningTasks",
                columns: new[] { "TaskGroupId", "Number" },
                unique: true,
                filter: "[TaskGroupId] IS NOT NULL AND [SubNumber] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningTasks_TaskGroupId_Number_SubNumber",
                table: "PlanningTasks",
                columns: new[] { "TaskGroupId", "Number", "SubNumber" },
                unique: true,
                filter: "[TaskGroupId] IS NOT NULL AND [SubNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DevGroupMembers_PersonId",
                table: "DevGroupMembers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_DevGroups_Name",
                table: "DevGroups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanningTasks_Projects_ProjectId",
                table: "PlanningTasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_DevGroups_DevGroupId",
                table: "Projects",
                column: "DevGroupId",
                principalTable: "DevGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanningTasks_Projects_ProjectId",
                table: "PlanningTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_DevGroups_DevGroupId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "DevGroupMembers");

            migrationBuilder.DropTable(
                name: "DevGroups");

            migrationBuilder.DropIndex(
                name: "IX_Projects_DevGroupId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_PlanningTasks_ProjectId_TaskGroupId",
                table: "PlanningTasks");

            migrationBuilder.DropIndex(
                name: "IX_PlanningTasks_TaskGroupId_Number",
                table: "PlanningTasks");

            migrationBuilder.DropIndex(
                name: "IX_PlanningTasks_TaskGroupId_Number_SubNumber",
                table: "PlanningTasks");

            migrationBuilder.DropColumn(
                name: "DevGroupId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "PlanningTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "TaskGroupId",
                table: "PlanningTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Number",
                table: "PlanningTasks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningTasks_TaskGroupId_Number",
                table: "PlanningTasks",
                columns: new[] { "TaskGroupId", "Number" },
                unique: true,
                filter: "[SubNumber] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningTasks_TaskGroupId_Number_SubNumber",
                table: "PlanningTasks",
                columns: new[] { "TaskGroupId", "Number", "SubNumber" },
                unique: true,
                filter: "[SubNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");
        }
    }
}
