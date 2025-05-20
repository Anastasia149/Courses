using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Courses.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeworkFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Homeworks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HomeworkFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HomeworkId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeworkFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeworkFiles_Homeworks_HomeworkId",
                        column: x => x.HomeworkId,
                        principalTable: "Homeworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Homeworks_UserId",
                table: "Homeworks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeworkFiles_HomeworkId",
                table: "HomeworkFiles",
                column: "HomeworkId");

            migrationBuilder.AddForeignKey(
                name: "FK_Homeworks_AspNetUsers_UserId",
                table: "Homeworks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Homeworks_AspNetUsers_UserId",
                table: "Homeworks");

            migrationBuilder.DropTable(
                name: "HomeworkFiles");

            migrationBuilder.DropIndex(
                name: "IX_Homeworks_UserId",
                table: "Homeworks");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Homeworks");
        }
    }
}
