using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonLastNameAndUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_Name",
                table: "People");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "People",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                collation: "Latin1_General_CI_AI");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "People",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                collation: "Latin1_General_CI_AI");

            migrationBuilder.CreateIndex(
                name: "IX_People_LastName_Name",
                table: "People",
                columns: new[] { "LastName", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_UserName",
                table: "People",
                column: "UserName",
                unique: true,
                filter: "[UserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_LastName_Name",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_UserName",
                table: "People");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "People");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "People");

            migrationBuilder.CreateIndex(
                name: "IX_People_Name",
                table: "People",
                column: "Name",
                unique: true);
        }
    }
}
