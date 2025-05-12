using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Courses.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursesModels2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
